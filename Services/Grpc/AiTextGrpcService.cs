using Grpc.Core;
using WebApiServer.Grpc;
using WebApiServer.Models;

namespace WebApiServer.Services.Grpc
{
    /// <summary>
    /// gRPC service implementation for AI text generation
    /// Uses the same RequestQueueService as the REST API to maintain consistency
    /// </summary>
    public class AiTextGrpcService : AiTextService.AiTextServiceBase
    {
        private readonly RequestQueueService _queueService;
        private readonly ILogger<AiTextGrpcService> _logger;

        public AiTextGrpcService(RequestQueueService queueService, ILogger<AiTextGrpcService> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        /// <summary>
        /// Submits a new text generation request via gRPC
        /// Same functionality as POST /api/generate in REST API
        /// </summary>
        public override Task<SubmitResponse> SubmitRequest(TextGenerationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("[gRPC] Received text generation request with prompt length: {PromptLength}", 
                request.Prompt?.Length ?? 0);

            // Validate input
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Prompt cannot be empty"));
            }

            try
            {
                // Create client request object (reuse existing model)
                var clientRequest = new ClientRequest
                {
                    Prompt = request.Prompt,
                    PreviousRequestId = string.IsNullOrWhiteSpace(request.PreviousRequestId) ? null : request.PreviousRequestId
                };

                // Use the same queue service as REST API
                string requestId = _queueService.AddRequest(clientRequest, clientRequest.PreviousRequestId);
                
                _logger.LogInformation("[gRPC] Created request with ID: {RequestId}", requestId);

                // Return gRPC response
                return Task.FromResult(new SubmitResponse
                {
                    RequestId = requestId,
                    Status = "PENDING",
                    SubmissionTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[gRPC] Error submitting request");
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Gets the status of a previously submitted request via gRPC
        /// Same functionality as GET /api/status/{id} in REST API
        /// </summary>
        public override Task<RequestStatusResponse> GetRequestStatus(StatusRequest request, ServerCallContext context)
        {
            _logger.LogInformation("[gRPC] Status check for request ID: {RequestId}", request.RequestId);

            if (string.IsNullOrWhiteSpace(request.RequestId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request ID cannot be empty"));
            }

            try
            {
                // Use the same queue service as REST API
                var state = _queueService.GetRequest(request.RequestId);

                if (state == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, $"Request ID {request.RequestId} not found"));
                }

                // Convert internal state to gRPC response
                var response = new RequestStatusResponse
                {
                    RequestId = state.RequestId,
                    Status = state.Status,
                    Result = state.Result ?? string.Empty,
                    SubmissionTime = state.SubmissionTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ClientPrompt = state.ClientPrompt ?? string.Empty,
                    PreviousRequestId = state.PreviousRequestId ?? string.Empty
                };

                _logger.LogInformation("[gRPC] Returning status: {Status} for request: {RequestId}", 
                    state.Status, request.RequestId);

                return Task.FromResult(response);
            }
            catch (RpcException)
            {
                // Re-throw gRPC exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[gRPC] Error getting request status for ID: {RequestId}", request.RequestId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Streams status updates for a request (optional enhancement)
        /// Polls the queue service and streams updates until completion
        /// </summary>
        public override async Task StreamRequestStatus(StatusRequest request, IServerStreamWriter<RequestStatusResponse> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("[gRPC] Starting status stream for request ID: {RequestId}", request.RequestId);

            if (string.IsNullOrWhiteSpace(request.RequestId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request ID cannot be empty"));
            }

            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var state = _queueService.GetRequest(request.RequestId);

                    if (state == null)
                    {
                        throw new RpcException(new Status(StatusCode.NotFound, $"Request ID {request.RequestId} not found"));
                    }

                    // Send current status
                    var response = new RequestStatusResponse
                    {
                        RequestId = state.RequestId,
                        Status = state.Status,
                        Result = state.Result ?? string.Empty,
                        SubmissionTime = state.SubmissionTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        ClientPrompt = state.ClientPrompt ?? string.Empty,
                        PreviousRequestId = state.PreviousRequestId ?? string.Empty
                    };

                    await responseStream.WriteAsync(response);
                    _logger.LogDebug("[gRPC] Streamed status: {Status} for request: {RequestId}", 
                        state.Status, request.RequestId);

                    // Stop streaming if request is complete or failed
                    if (state.Status == "COMPLETE" || state.Status == "FAILED")
                    {
                        _logger.LogInformation("[gRPC] Request {RequestId} finished with status: {Status}", 
                            request.RequestId, state.Status);
                        break;
                    }

                    // Wait before next poll (5 seconds)
                    await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[gRPC] Status stream cancelled for request ID: {RequestId}", request.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[gRPC] Error in status stream for request ID: {RequestId}", request.RequestId);
                throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
            }
        }
    }
}