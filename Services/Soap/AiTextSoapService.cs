using WebApiServer.Models;

namespace WebApiServer.Services.Soap
{
    /// <summary>
    /// SOAP service implementation for AI text generation
    /// Uses the same RequestQueueService as REST API and gRPC to maintain consistency
    /// </summary>
    public class AiTextSoapService : IAiTextSoapService
    {
        private readonly RequestQueueService _queueService;
        private readonly ILogger<AiTextSoapService> _logger;

        public AiTextSoapService(RequestQueueService queueService, ILogger<AiTextSoapService> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        /// <summary>
        /// Submits a new text generation request via SOAP
        /// Same functionality as REST API and gRPC
        /// </summary>
        public SoapSubmitResponse SubmitTextRequest(SoapTextRequest request)
        {
            _logger.LogInformation("[SOAP] Received text generation request with prompt length: {PromptLength}", 
                request.Prompt?.Length ?? 0);

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    _logger.LogWarning("[SOAP] Empty prompt received");
                    return new SoapSubmitResponse
                    {
                        Success = false,
                        ErrorMessage = "Prompt cannot be empty",
                        Status = "ERROR",
                        SubmissionTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    };
                }

                // Create client request object (reuse existing model)
                var clientRequest = new ClientRequest
                {
                    Prompt = request.Prompt,
                    PreviousRequestId = string.IsNullOrWhiteSpace(request.PreviousRequestId) ? null : request.PreviousRequestId
                };

                // Use the same queue service as REST API and gRPC
                string requestId = _queueService.AddRequest(clientRequest, clientRequest.PreviousRequestId);
                
                _logger.LogInformation("[SOAP] Created request with ID: {RequestId}", requestId);

                // Return SOAP response
                return new SoapSubmitResponse
                {
                    RequestId = requestId,
                    Status = "PENDING",
                    SubmissionTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SOAP] Error submitting request");
                return new SoapSubmitResponse
                {
                    Success = false,
                    ErrorMessage = $"Internal error: {ex.Message}",
                    Status = "ERROR",
                    SubmissionTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };
            }
        }

        /// <summary>
        /// Gets the status of a previously submitted request via SOAP
        /// Same functionality as REST API and gRPC
        /// </summary>
        public SoapStatusResponse GetRequestStatus(SoapStatusRequest request)
        {
            _logger.LogInformation("[SOAP] Status check for request ID: {RequestId}", request.RequestId);

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.RequestId))
                {
                    _logger.LogWarning("[SOAP] Empty request ID received");
                    return new SoapStatusResponse
                    {
                        Success = false,
                        ErrorMessage = "Request ID cannot be empty",
                        Status = "ERROR"
                    };
                }

                // Use the same queue service as REST API and gRPC
                RequestState? state;
                try
                {
                    state = _queueService.GetRequest(request.RequestId);
                }
                catch (KeyNotFoundException)
                {
                    _logger.LogWarning("[SOAP] Request ID not found: {RequestId}", request.RequestId);
                    return new SoapStatusResponse
                    {
                        Success = false,
                        ErrorMessage = $"Request ID {request.RequestId} not found",
                        Status = "NOT_FOUND"
                    };
                }

                if (state == null)
                {
                    return new SoapStatusResponse
                    {
                        Success = false,
                        ErrorMessage = $"Request ID {request.RequestId} not found",
                        Status = "NOT_FOUND"
                    };
                }

                // Convert internal state to SOAP response
                var response = new SoapStatusResponse
                {
                    RequestId = state.RequestId,
                    Status = state.Status,
                    Result = state.Result,
                    SubmissionTime = state.SubmissionTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ClientPrompt = state.ClientPrompt,
                    PreviousRequestId = state.PreviousRequestId,
                    Success = true
                };

                _logger.LogInformation("[SOAP] Returning status: {Status} for request: {RequestId}", 
                    state.Status, request.RequestId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SOAP] Error getting request status for ID: {RequestId}", request.RequestId);
                return new SoapStatusResponse
                {
                    Success = false,
                    ErrorMessage = $"Internal error: {ex.Message}",
                    Status = "ERROR"
                };
            }
        }
    }
}