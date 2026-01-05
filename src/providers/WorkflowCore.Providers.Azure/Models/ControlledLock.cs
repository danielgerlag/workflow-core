using Azure.Storage.Blobs.Specialized;

namespace WorkflowCore.Providers.Azure.Models
{
    class ControlledLock
    {
        public string Id { get; set; }
        public string LeaseId { get; set; }
        public BlockBlobClient Blob { get; set; }

        public ControlledLock(string id, string leaseId, BlockBlobClient blob)
        {
            Id = id;
            LeaseId = leaseId;
            Blob = blob;
        }
    }
}
