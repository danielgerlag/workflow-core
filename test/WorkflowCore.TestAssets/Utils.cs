using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace WorkflowCore.TestAssets
{
    public static class Utils
    {
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, DateFormatHandling = DateFormatHandling.IsoDateFormat, DateTimeZoneHandling = DateTimeZoneHandling.Utc };

        public static T DeepCopy<T>(T obj)
        {
            string str = JsonConvert.SerializeObject(obj, SerializerSettings);
            T result = JsonConvert.DeserializeObject<T>(str);
            return result;
        }

        public static string GetTestDefinitionJson()
        {
            return File.ReadAllText("stored-definition.json");
        }

        public static string GetTestDefinitionYaml()
        {
            return File.ReadAllText("stored-definition.yaml");
        }

        public static string GetTestDefinitionDynamicJson()
        {
            return File.ReadAllText("stored-dynamic-definition.json");
        }

        public static string GetTestDefinitionJsonMissingInputProperty()
        {
            return File.ReadAllText("stored-def-missing-input-property.json");
        }
    }
}

