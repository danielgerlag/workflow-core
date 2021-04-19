using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace WorkflowCore.Persistence.MongoDB.Services
{
    public class AssemblyQualifiedDiscriminatorConvention : IDiscriminatorConvention
    {
        public string ElementName
        {
            get
            {
                return "_t";
            }
        }

        public Type GetActualType(IBsonReader bsonReader, Type nominalType)
        {
            var bookmark = bsonReader.GetBookmark();
            bsonReader.ReadStartDocument();
            string typeValue = string.Empty;
            if (bsonReader.FindElement(ElementName))
                typeValue = bsonReader.ReadString();
            else
                throw new NotSupportedException();

            bsonReader.ReturnToBookmark(bookmark);
            var result = Type.GetType(typeValue);
            return result;
        }

        public BsonValue GetDiscriminator(Type nominalType, Type actualType)
        {
            return actualType.AssemblyQualifiedName;
        }
    }
}
