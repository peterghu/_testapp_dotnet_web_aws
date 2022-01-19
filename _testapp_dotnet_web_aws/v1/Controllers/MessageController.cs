using _testapp_dotnet_web_aws.Clients;
using _testapp_dotnet_web_aws.v1.Models;
using _testapp_dotnet_web_aws.v1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace _testapp_dotnet_web_aws.v1.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ISqsService _sqsService;
        private readonly ILogger<MessageController> _logger;
        private readonly IMessageService _messageService;

        public MessageController(ISqsService sqsService, ILogger<MessageController> logger, IMessageService messageService)
        {
            _sqsService = sqsService;
            _logger = logger;
            _messageService = messageService;
        }

        //
        [HttpGet("messages")]
        public async Task<ActionResult<List<RequestLogModel>>> Messages()
        {
            var res = await _messageService.GetRequestLogs();
            return Ok(res);
        }

        [HttpPost("messages")]
        public ActionResult<bool> AddMessage([FromBody] AddMessageModel body)
        {
            var res = _messageService.AddMessage(body.Message);
            return Ok(res);
        }

        [HttpPost]
        [Route("queue")]
        public async Task<IActionResult> PublishMessage([FromBody] AddMessageModel body)
        {
            await _sqsService.PostMessageAsync(body);
            _logger.LogInformation("New message published with {@Content}", body);
            return StatusCode((int)HttpStatusCode.Created);
        }

        [HttpGet("queueStatus")]
        public async Task<ActionResult<SqsStatus>> QueueStatus()
        {
            var res = await _sqsService.GetQueueStatusAsync();
            return Ok(res);
        }

        [HttpGet("queue")]
        public async Task<ActionResult<Amazon.SQS.Model.Message>> GetMessages()
        {
            var res = await _sqsService.GetMessagesAsync();

            return Ok(res);
        }
    }
}