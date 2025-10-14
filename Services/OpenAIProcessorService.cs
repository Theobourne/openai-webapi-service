// Services/OpenAIProcessorService.cs

// Clean up your using directives. You only need the ones used below.
using Azure; // Needed for AzureKeyCredential
using OpenAI; // Contains OpenAIClient and OpenAIClientOptions
using OpenAI.Chat; // Contains ChatClient, ChatCompletionOptions, ChatMessage, etc.


namespace WebApiServer.Services
{
    public class OpenAIProcessorService : BackgroundService
    {
        private readonly RequestQueueService _queueService;
        
        // Rate limiting: Wait at least 45 seconds between calls (as per lab example)
        private static readonly TimeSpan MinWaitTime = TimeSpan.FromSeconds(45);
        
        // Azure OpenAI Configuration (from environment variables for security)
        private readonly string azureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") 
            ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT environment variable is required");
        private readonly string azureApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") 
            ?? throw new InvalidOperationException("AZURE_OPENAI_API_KEY environment variable is required");
        private readonly string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") 
            ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT environment variable is required"); 

        public OpenAIProcessorService(RequestQueueService queueService)
        {
            _queueService = queueService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            // Initialize messages list for storing conversation history
            var messages = new List<ChatMessage>();
            var lastRequestId = string.Empty; // Track the last request ID to detect new conversations
            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Check Rate Limit
                var timeSinceLastCall = DateTime.UtcNow - _queueService.LastAzureCallTime;

                if (timeSinceLastCall < MinWaitTime)
                {
                    // Wait the remaining time before checking again
                    var timeToWait = MinWaitTime - timeSinceLastCall;
                    await Task.Delay(timeToWait, stoppingToken);
                    continue;
                }

                // 2. Get Next Pending Request
                var request = _queueService.GetNextPendingRequest();

                if (request != null)
                {
                    _queueService.UpdateStatus(request.RequestId, "PROCESSING");
                    try
                    {
                        var client = new OpenAIClient(new AzureKeyCredential(azureApiKey), new OpenAIClientOptions { Endpoint = new Uri(azureEndpoint) });
                        var chatClient = client.GetChatClient(deploymentName);

                        Console.WriteLine($"[DEBUG] Processing request ID: {request.RequestId}");
                        Console.WriteLine($"[DEBUG] Current prompt: {request.ClientPrompt}");
                        Console.WriteLine($"[DEBUG] Previous request ID: {lastRequestId}");
                        
                        // Update the last request ID
                        lastRequestId = request.RequestId;
                        
                        Console.WriteLine($"[DEBUG] Current message list count: {messages.Count}");

                        // First, always add the system message with enhanced memory instructions
                        messages.Add(new SystemChatMessage(
                            "You are an AI assistant that helps people find information. " +
                            "You must remember and reference all previous messages in the conversation. " +
                            "When asked about previous interactions, look at all prior messages to maintain accurate context. " +
                            "If someone asks about something mentioned before, refer back to the specific message. " +
                            "Always maintain conversation continuity."
                        ));

                        // Then add the conversation history if it exists
                        // if (request.ConversationHistory != null && request.ConversationHistory.Count > 0)
                        // {
                        //     Console.WriteLine("[DEBUG] Adding previous conversation history:");
                        //     // Skip the system message if it exists in history
                        //     var historyToAdd = request.ConversationHistory.Where(m => !(m is SystemChatMessage));
                        //     foreach (var msg in historyToAdd)
                        //     {
                        //         string type = msg switch
                        //         {
                        //             UserChatMessage => "User",
                        //             AssistantChatMessage => "Assistant",
                        //             _ => "Unknown"
                        //         };
                        //         Console.WriteLine($"[DEBUG] Adding historical message - Type: {type}, Content: {msg.Content}");
                        //         messages.Add(msg);
                        //     }
                        // }
                        // else
                        // {
                        //     Console.WriteLine("[DEBUG] Starting new conversation");
                        // }

                        // Add the new user message to the existing conversation
                        Console.WriteLine("[DEBUG] Adding new user message to conversation");
                        messages.Add(new UserChatMessage(request.ClientPrompt));

                        // Update the request's conversation history immediately
                        // request.ConversationHistory = new List<ChatMessage>(messages);

                        // Record the time of the call BEFORE the API call
                        _queueService.LastAzureCallTime = DateTime.UtcNow;

                        // Debug: Print all messages being sent to the API
                        Console.WriteLine("\n[DEBUG] Messages being sent to API:");
                        foreach (var msg in messages)
                        {
                            Console.WriteLine($"[DEBUG] Message Type: {msg.GetType().Name}");
                            Console.WriteLine($"[DEBUG] Content: {msg.Content}");
                            Console.WriteLine("-------------------");
                        }
                        Console.WriteLine($"[DEBUG] Total messages being sent: {messages.Count}\n");

                        // Execute the call with memory settings
                        var response = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                        {
                            Temperature = 0.7f,
                            FrequencyPenalty = 0.3f,  // Increase frequency penalty to encourage more diverse responses
                            PresencePenalty = 0.3f    // Increase presence penalty to encourage using context
                        });

                        // Extract the result - get the text from the content
                        string resultText = response.Value.Content.Last().Text;

                        Console.WriteLine("[DEBUG] Got response from API");
                        Console.WriteLine($"[DEBUG] Response text: {resultText}");
                        Console.WriteLine($"[DEBUG] Current message count: {messages.Count}");

                        // Add the assistant's response to conversation history
                        messages.Add(new AssistantChatMessage(resultText));
                        Console.WriteLine($"[DEBUG] Added assistant response, new count: {messages.Count}");

                        // Debug: Print all messages being sent to the API
                        Console.WriteLine("\n[DEBUG] Messages being sent to API:");
                        foreach (var msg in messages)
                        {
                            Console.WriteLine($"[DEBUG] Message Type: {msg.GetType().Name}");
                            Console.WriteLine($"[DEBUG] Content: {msg.Content}");
                            Console.WriteLine("-------------------");
                        }
                        Console.WriteLine($"[DEBUG] Total messages being sent: {messages.Count}\n");

                        // Update state with both result and conversation history
                        _queueService.UpdateStatus(request.RequestId, "COMPLETE", resultText, messages);
                        Console.WriteLine("[DEBUG] Updated request status with new conversation history");
                    }
                    //end of AI replacement

                    catch (Exception ex)
                    {
                        // 4. Update State to FAILED
                        _queueService.UpdateStatus(request.RequestId, "FAILED", $"Error: {ex.Message}. Check your endpoint, key, and deployment name.");
                    }
                }

                // Wait for a short interval before checking the queue again
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
