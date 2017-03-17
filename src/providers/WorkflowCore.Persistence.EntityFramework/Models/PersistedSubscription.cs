using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowCore.Persistence.EntityFramework.Models
{    
    public class PersistedSubscription
    {
        [Key]
        public long PersistenceId { get; set; }

        [MaxLength(200)]
        public Guid SubscriptionId { get; set; }

        [MaxLength(200)]
        public string WorkflowId { get; set; }

        public int StepId { get; set; }

        [MaxLength(200)]
        public string EventName { get; set; }

        [MaxLength(200)]
        public string EventKey { get; set; }

        public DateTime SubscribeAsOf { get; set; }
    }
}
