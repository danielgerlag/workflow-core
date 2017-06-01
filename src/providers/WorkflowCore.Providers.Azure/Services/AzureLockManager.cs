using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using WorkflowCore.Interface;
using WorkflowCore.Providers.Azure.Models;

namespace WorkflowCore.Providers.Azure.Services
{
    public class AzureLockManager: IDistributedLockProvider
    {
        private readonly CloudBlobClient _client;
        private readonly CloudBlobContainer _container;
        private readonly ILogger _logger;
        private readonly List<ControlledLock> _locks = new List<ControlledLock>();
        private Timer _renewTimer;
        private TimeSpan LockTimeout => TimeSpan.FromMinutes(5);
        private TimeSpan RenewInterval => TimeSpan.FromMinutes(3);

        public AzureLockManager(string connectionString, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<AzureLockManager>();
            var account = CloudStorageAccount.Parse(connectionString);
            _client = account.CreateCloudBlobClient();

            _container = _client.GetContainerReference("workflowcore-locks");
            _container.CreateIfNotExistsAsync().Wait();
        }

        public async Task<bool> AcquireLock(string Id)
        {
            var blob = _container.GetBlockBlobReference(Id);

            if (!await blob.ExistsAsync())
            {
                await blob.UploadTextAsync(string.Empty);
            }

            try
            {
                var leaseId = await blob.AcquireLeaseAsync(LockTimeout);
                lock (_locks)
                {
                    _locks.Add(new ControlledLock(Id, leaseId, blob));
                }
                return true;
            }
            catch (StorageException ex)
            {
                _logger.LogDebug($"Failed to acquire lock {Id} - {ex.Message}");
                return false;
            }
        }

        public async Task ReleaseLock(string Id)
        {
            ControlledLock entry = null;
            lock (_locks)
            {
                entry = _locks.FirstOrDefault(x => x.Id == Id);
            }

            if (entry != null)
            {
                await entry.Blob.ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(entry.Id));
                lock (_locks)
                {
                    _locks.Remove(entry);
                }
            }
        }

        public void Start()
        {
            _renewTimer = new Timer(RenewLeases, null, RenewInterval, RenewInterval);
        }

        public void Stop()
        {
            if (_renewTimer == null)
                return;

            _renewTimer.Dispose();
            _renewTimer = null;
        }

        private void RenewLeases(object state)
        {
            _logger.LogDebug("Renewing active leases");
            lock (_locks)
            {
                foreach (var entry in _locks)
                    RenewLock(entry);
            }
        }

        private async Task RenewLock(ControlledLock entry)
        {
            try
            {
                await entry.Blob.RenewLeaseAsync(AccessCondition.GenerateLeaseCondition(entry.LeaseId));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error renewing lease - {ex.Message}");
            }
        }
    }
    
}

