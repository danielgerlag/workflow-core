using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Persistence.EntityFramework.Models
{
    public class PersistedPublication
    {
        [Key]
        public long ClusterKey { get; set; }

        public Guid PublicationId { get; set; }

        [MaxLength(200)]
        public string WorkflowId { get; set; }

        public int StepId { get; set; }

        [MaxLength(200)]
        public string EventName { get; set; }

        [MaxLength(200)]
        public string EventKey { get; set; }

        public string EventData { get; set; }
    }
}
