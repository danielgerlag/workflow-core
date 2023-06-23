using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace WorkflowCore.Models
{
    public class Token
    {
        public string SubscriptionId { get; set; }
        public string ActivityName { get; set; }
        public string Nonce { get; set; }

        public string Encode()
        {
            var json = JsonConvert.SerializeObject(this);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static Token Create(string subscriptionId, string activityName)
        {
            return new Token
            {
                SubscriptionId = subscriptionId,
                ActivityName = activityName,
                Nonce = Guid.NewGuid().ToString()
            };
        }

        public static Token Decode(string encodedToken)
        {
            var raw = Convert.FromBase64String(encodedToken);
            var json = Encoding.UTF8.GetString(raw);
            return JsonConvert.DeserializeObject<Token>(json);
        }
    }
}