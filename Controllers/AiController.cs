// Controllers/AiController.cs
using Microsoft.AspNetCore.Mvc;
using WebApiServer.Models;
using WebApiServer.Services;
using System.Linq;

namespace WebApiServer.Controllers
{
    [ApiController]
    [Route("api")]
    public class AiController : ControllerBase
    {
        private readonly RequestQueueService _queueService;

        public AiController(RequestQueueService queueService)
        {
            _queueService = queueService;
        }

        // POST /api/generate - Client submits a new request
        [HttpPost("generate")]
        public ActionResult<RequestState> SubmitRequest([FromBody] ClientRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { Error = "Prompt cannot be empty." });
            }
            
            // Pass the previous request ID if this is a follow-up
            string requestId = _queueService.AddRequest(request, request.PreviousRequestId);
            
            return Ok(new RequestState { RequestId = requestId, Status = "PENDING" });
        }

        // GET /api/status/{id} - Client polls for status and result
        [HttpGet("status/{id}")]
        public ActionResult<RequestState> GetStatus(string id)
        {
            // 1. Retrieve the state from the queue
            var state = _queueService.GetRequest(id);

            if (state == null)
            {
                return NotFound(new { Error = $"Request ID {id} not found." });
            }
            
            // 2. Return the full state (Status, Result, etc.)
            return Ok(state);
        }
    }
}