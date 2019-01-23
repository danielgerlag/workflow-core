using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WebApiSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : Controller
    {
        private readonly IWorkflowController _workflowService;

        public EventsController(IWorkflowController workflowService)
        {
            _workflowService = workflowService;
        }

        [HttpPost("{eventName}/{eventKey}")]
        public async Task<IActionResult> Post(string eventName, string eventKey, [FromBody]object eventData)
        {
            await _workflowService.PublishEvent(eventName, eventKey, eventData);
            return Ok();
        }

    }
}
