using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WorkflowCore.Providers.Azure.Models
{
    class ControlledLock
    {
        public string Id { get; set; }
        public string LeaseId { get; set; }
        public CloudBlockBlob Blob { get; set; }

        public ControlledLock(string id, string leaseId, CloudBlockBlob blob)
        {
            Id = id;
            LeaseId = leaseId;
            Blob = blob;
        }
    }
}
