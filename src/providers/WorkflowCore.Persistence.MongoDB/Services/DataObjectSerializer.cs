using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Models;
using Newtonsoft.Json;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class DataObjectSerializer : SerializerBase<object>
    {
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public override object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {                        
            var raw = BsonSerializer.Deserialize<string>(context.Reader);
            var result = JsonConvert.DeserializeObject(raw, SerializerSettings);
            return result;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            string str = JsonConvert.SerializeObject(value, SerializerSettings);
            BsonSerializer.Serialize(context.Writer, str);
        }
                
                
    }
}
