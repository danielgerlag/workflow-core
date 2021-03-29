using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.WebAPI.Controllers
{
    [Route("[controller]")]
    public class EventsController : Controller
    {

        private readonly IWorkflowHost _workflowHost;
        private readonly ILogger _logger;

        public EventsController(IWorkflowHost workflowHost, ILoggerFactory loggerFactory)
        {
            _workflowHost = workflowHost;
            _logger = loggerFactory.CreateLogger<EventsController>();
        }

        [HttpPost("{eventName}/{eventKey}")]
        public async Task<IActionResult> Post(string eventName, string eventKey, [FromBody]object eventData)
        {
            await _workflowHost.PublishEvent(eventName, eventKey, eventData);
            return Ok();
        }
    }
}
