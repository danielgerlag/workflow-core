using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
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
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };
        
        public override object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context.Reader.CurrentBsonType == BsonType.String)
            {
                var raw = BsonSerializer.Deserialize<string>(context.Reader);
                return JsonConvert.DeserializeObject(raw, SerializerSettings);
            }

            return BsonSerializer.Deserialize(context.Reader, typeof(object));
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            string str = JsonConvert.SerializeObject(value, SerializerSettings);
            var doc = BsonDocument.Parse(str);
            var typeElem = doc.GetElement("$type");
            doc.RemoveElement(typeElem);

            if (doc.Elements.All(x => x.Name != "_t"))
                doc.InsertAt(0, new BsonElement("_t", typeElem.Value));
            
            BsonSerializer.Serialize(context.Writer, doc);
        }
    }
}
