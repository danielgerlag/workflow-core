using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WorkflowCore.TestAssets
{
    public static class Utils
    {
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, DateFormatHandling = DateFormatHandling.IsoDateFormat, DateTimeZoneHandling = DateTimeZoneHandling.Utc };

        public static bool CompareObjects(object objectA, object objectB)
        {
            string strA = JsonConvert.SerializeObject(objectA, SerializerSettings);
            string strB = JsonConvert.SerializeObject(objectB, SerializerSettings);

            Console.WriteLine("A = " + strA);
            Console.WriteLine("B = " + strB);

            return (strA == strB);
        }

        public static T DeepCopy<T>(T obj)
        {
            string str = JsonConvert.SerializeObject(obj, SerializerSettings);
            T result = JsonConvert.DeserializeObject<T>(str);
            return result;
        }

    }
}

