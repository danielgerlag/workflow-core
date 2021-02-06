using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
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
            TypeNameHandling = TypeNameHandling.Objects,
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
            var str = JsonConvert.SerializeObject(value, SerializerSettings);
            var doc = BsonDocument.Parse(str);
            ConvertMetaFormat(doc);
            
            BsonSerializer.Serialize(context.Writer, doc);
        }

        private static void ConvertMetaFormat(BsonDocument root)
        {
            var stack = new Stack<BsonDocument>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var doc = stack.Pop();

                if (doc.TryGetElement("$type", out var typeElem))
                {
                    doc.RemoveElement(typeElem);

                    if (doc.Elements.All(x => x.Name != "_t"))
                        doc.InsertAt(0, new BsonElement("_t", typeElem.Value));
                }

                foreach (var subDoc in doc.Elements)
                {
                    if (subDoc.Value.IsBsonDocument)
                        stack.Push(subDoc.Value.ToBsonDocument());

                    if (subDoc.Value.IsBsonArray)
                    {
                        foreach (var element in subDoc.Value.AsBsonArray)
                        {
                            if (element.IsBsonDocument)
                                stack.Push(element.ToBsonDocument());
                        }
                    }
                }
            }
        }
    }
}
