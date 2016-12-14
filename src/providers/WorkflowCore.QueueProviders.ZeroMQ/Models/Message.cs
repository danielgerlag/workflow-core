using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.QueueProviders.ZeroMQ.Models
{
    class Message
    {
        public MessageType MessageType { get; set; }
        public string Content { get; set; }

        public EventPublication ToEventPublication()
        {
            if (MessageType != MessageType.Publication)
                throw new NotSupportedException();

            return JsonConvert.DeserializeObject<EventPublication>(Content);
        }

        public static Message FromWorkflowId(string id)
        {
            Message result = new Message();
            result.MessageType = MessageType.Workflow;
            result.Content = id;
            return result;
        }

        public static Message FromPublication(EventPublication pub)
        {
            Message result = new Message();
            result.MessageType = MessageType.Publication;
            result.Content = JsonConvert.SerializeObject(pub);
            return result;
        }
    }

    enum MessageType { Workflow, Publication }
}
