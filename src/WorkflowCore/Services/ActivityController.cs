using System;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WorkflowCore.Exceptions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class ActivityController : IActivityController
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IDistributedLockProvider _lockProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IWorkflowController _workflowController;

        public ActivityController(ISubscriptionRepository subscriptionRepository, IWorkflowController workflowController, IDateTimeProvider dateTimeProvider, IDistributedLockProvider lockProvider)
        {
            _subscriptionRepository = subscriptionRepository;
            _dateTimeProvider = dateTimeProvider;
            _lockProvider = lockProvider;
            _workflowController = workflowController;
        }
        
        public async Task<PendingActivity> GetPendingActivity(string activityName, string workerId, TimeSpan? timeout = null)
        {
            var endTime = _dateTimeProvider.UtcNow.Add(timeout ?? TimeSpan.Zero);
            var firstPass = true;
            EventSubscription subscription = null;
            while ((subscription == null && _dateTimeProvider.UtcNow < endTime) || firstPass)
            {
                if (!firstPass)
                    await Task.Delay(100);
                subscription = await _subscriptionRepository.GetFirstOpenSubscription(Event.EventTypeActivity, activityName, _dateTimeProvider.Now);
                if (subscription != null)
                    if (!await _lockProvider.AcquireLock($"sub:{subscription.Id}", CancellationToken.None))
                        subscription = null;
                firstPass = false;
            }
            if (subscription == null)
                return null;
            
            try
            {
                var token = Token.Create(subscription.Id, subscription.EventKey);
                var result = new PendingActivity()
                {
                    Token = token.Encode(),
                    ActivityName = subscription.EventKey,
                    Parameters = subscription.SubscriptionData,
                    TokenExpiry = DateTime.MaxValue
                };

                if (!await _subscriptionRepository.SetSubscriptionToken(subscription.Id, result.Token, workerId, result.TokenExpiry))
                    return null;

                return result;
            }
            finally
            {
                await _lockProvider.ReleaseLock($"sub:{subscription.Id}");
            }

        }

        public async Task ReleaseActivityToken(string token)
        {
            var tokenObj = Token.Decode(token);
            await _subscriptionRepository.ClearSubscriptionToken(tokenObj.SubscriptionId, token);
        }

        public async Task SubmitActivitySuccess(string token, object result)
        {
            await SubmitActivityResult(token, new ActivityResult()
            {
                Data = result,
                Status = ActivityResult.StatusType.Success
            });
        }

        public async Task SubmitActivityFailure(string token, object result)
        {
            await SubmitActivityResult(token, new ActivityResult()
            {
                Data = result,
                Status = ActivityResult.StatusType.Fail
            });
        }

        private async Task SubmitActivityResult(string token, ActivityResult result)
        {
            var tokenObj = Token.Decode(token);
            var sub = await _subscriptionRepository.GetSubscription(tokenObj.SubscriptionId);
            if (sub == null)
                throw new NotFoundException();

            if (sub.ExternalToken != token)
                throw new NotFoundException("Token mismatch");
            
            result.SubscriptionId = sub.Id;

            await _workflowController.PublishEvent(sub.EventName, sub.EventKey, result);
        }

        class Token
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
                return new Token()
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
}