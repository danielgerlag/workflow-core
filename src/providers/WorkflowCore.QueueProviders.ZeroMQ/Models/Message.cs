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
                
        public static Message FromWorkflowId(string id)
        {
            Message result = new Message();
            result.MessageType = MessageType.Workflow;
            result.Content = id;
            return result;
        }

        public static Message FromEventId(string id)
        {
            Message result = new Message();
            result.MessageType = MessageType.Event;
            result.Content = id;
            return result;
        }
    }

    enum MessageType { Workflow, Event }
}
