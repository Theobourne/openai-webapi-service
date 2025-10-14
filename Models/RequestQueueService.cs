// Services/RequestQueueService.cs
using System.Collections.Concurrent;
using WebApiServer.Models;
using OpenAI.Chat;
using System.Linq;
using System;

namespace WebApiServer.Services
{
    public class RequestQueueService
    {
        // Thread-safe dictionary to store all requests and their state
        private readonly ConcurrentDictionary<string, RequestState> _requests = new ConcurrentDictionary<string, RequestState>();

        // Timestamp to enforce the rate limit
        public DateTime LastAzureCallTime { get; set; } = DateTime.MinValue;

        public string AddRequest(ClientRequest clientRequest, string? previousRequestId = null)
        {
            var newState = new RequestState 
            { 
                ClientPrompt = clientRequest.Prompt ?? throw new ArgumentNullException(nameof(clientRequest.Prompt)),
                Status = "PENDING",
                SubmissionTime = DateTime.UtcNow
            };

            _requests[newState.RequestId] = newState;
            return newState.RequestId;
        }

        public RequestState GetRequest(string requestId)
        {
            if (_requests.TryGetValue(requestId, out var state))
            {
            return state;
        }
            else
            {
                throw new KeyNotFoundException($"Request with ID '{requestId}' was not found.");
            }
        }

        public RequestState? GetNextPendingRequest()
        {
            // Simple queue: return the oldest PENDING request, or null if none are found.
            // The .FirstOrDefault() method handles the empty collection gracefully by returning null.
            return _requests.Values
                .Where(r => r.Status == "PENDING")
                .OrderBy(r => r.SubmissionTime)
                .FirstOrDefault();
        }


        public void UpdateStatus(string requestId, string status, string? result = null, List<OpenAI.Chat.ChatMessage>? messages = null)
        {
            if (_requests.TryGetValue(requestId, out var state))
            {
                Console.WriteLine($"[DEBUG] Updating status for request {requestId}");
                Console.WriteLine($"[DEBUG] New status: {status}");
                Console.WriteLine($"[DEBUG] Message count in update: {messages?.Count ?? 0}");
                
                state.Status = status;
                if (result != null)
                {
                    state.Result = result;
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] Failed to find request {requestId} for status update");
            }
        }
    }
}