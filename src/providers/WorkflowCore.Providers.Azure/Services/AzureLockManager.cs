using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Providers.Azure.Models;

namespace WorkflowCore.Providers.Azure.Services
{
    public class AzureLockManager: IDistributedLockProvider
    {
        private readonly BlobServiceClient _client;
        private readonly ILogger _logger;
        private readonly List<ControlledLock> _locks = new List<ControlledLock>();
        private readonly AutoResetEvent _mutex = new AutoResetEvent(true);
        private BlobContainerClient _container;
        private Timer _renewTimer;
        private TimeSpan LockTimeout => TimeSpan.FromMinutes(1);
        private TimeSpan RenewInterval => TimeSpan.FromSeconds(45);

        public AzureLockManager(string connectionString, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<AzureLockManager>();
            _client = new BlobServiceClient(connectionString);
        }

        public AzureLockManager(Uri blobEndpoint, TokenCredential tokenCredential, ILoggerFactory logFactory)
        {
            _logger = logFactory.CreateLogger<AzureLockManager>();
            _client = new BlobServiceClient(blobEndpoint, tokenCredential);
        }

        public async Task<bool> AcquireLock(string Id, CancellationToken cancellationToken)
        {
            var blob = _container.GetBlockBlobClient(Id);

            if (!await blob.ExistsAsync())
                await blob.UploadAsync(new MemoryStream());

            if (_mutex.WaitOne())
            {
                try
                {
                    var lease = await blob.GetBlobLeaseClient().AcquireAsync(LockTimeout);
                    _locks.Add(new ControlledLock(Id, lease.Value.LeaseId, blob));
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Failed to acquire lock {Id} - {ex.Message}");
                    return false;
                }
                finally
                {
                    _mutex.Set();
                }
            }
            return false;
        }

        public async Task ReleaseLock(string Id)
        {
            if (_mutex.WaitOne())
            {
                try
                {
                    var entry = _locks.FirstOrDefault(x => x.Id == Id);

                    if (entry != null)
                    {
                        try
                        {
                            await entry.Blob.GetBlobLeaseClient(entry.LeaseId).ReleaseAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error releasing lock - {ex.Message}");
                        }                        
                        _locks.Remove(entry);
                    }
                }
                finally
                {
                    _mutex.Set();
                }
            }
        }

        public async Task Start()
        {
            _container = _client.GetBlobContainerClient("workflowcore-locks");
            await _container.CreateIfNotExistsAsync();
            _renewTimer = new Timer(RenewLeases, null, RenewInterval, RenewInterval);
        }

        public Task Stop()
        {
            if (_renewTimer == null)
                return Task.CompletedTask;

            _renewTimer.Dispose();
            _renewTimer = null;

            return Task.CompletedTask;
        }

        private async void RenewLeases(object state)
        {
            _logger.LogDebug("Renewing active leases");
            if (_mutex.WaitOne())
            {
                try
                {
                    foreach (var entry in _locks)
                        await RenewLock(entry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error renewing leases - {ex.Message}");
                }
                finally
                {
                    _mutex.Set();
                }
            }
        }

        private async Task RenewLock(ControlledLock entry)
        {
            try
            {
                await entry.Blob.GetBlobLeaseClient(entry.LeaseId).RenewAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error renewing lease - {ex.Message}");
            }
        }
    }
    
}

