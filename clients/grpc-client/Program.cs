using Grpc.Net.Client;
using GrpcClient.Grpc;

namespace GrpcClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Interactive gRPC Client for AI Text Generation ===");
            Console.WriteLine("Connecting to server at http://localhost:5167...");
            
            // Create gRPC channel for insecure HTTP/2 connection
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            using var channel = GrpcChannel.ForAddress("http://localhost:5167");
            var client = new AiTextService.AiTextServiceClient(channel);

            Console.WriteLine("✓ Connected to gRPC server");
            Console.WriteLine("\nInstructions:");
            Console.WriteLine("- Enter your prompt and press Enter to generate text");
            Console.WriteLine("- Type 'exit' or 'quit' to close the client");
            Console.WriteLine("- Type 'stream' followed by your prompt to use streaming mode");
            Console.WriteLine("- Example: 'stream Write a poem about coding'");
            Console.WriteLine("==========================================\n");

            while (true)
            {
                try
                {
                    // Get prompt from user
                    Console.Write("Enter your prompt: ");
                    string? input = Console.ReadLine();
                    
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Console.WriteLine("❌ Please enter a valid prompt.\n");
                        continue;
                    }

                    // Check for exit commands
                    if (input.ToLower() == "exit" || input.ToLower() == "quit")
                    {
                        Console.WriteLine("👋 Goodbye!");
                        break;
                    }

                    // Check for streaming mode
                    bool useStreaming = false;
                    string prompt = input;
                    if (input.ToLower().StartsWith("stream "))
                    {
                        useStreaming = true;
                        prompt = input.Substring(7); // Remove "stream " prefix
                        if (string.IsNullOrWhiteSpace(prompt))
                        {
                            Console.WriteLine("❌ Please provide a prompt after 'stream'.\n");
                            continue;
                        }
                    }

                    Console.WriteLine($"\n🚀 Processing: \"{prompt}\"");
                    Console.WriteLine($"Mode: {(useStreaming ? "Streaming (real-time)" : "Polling (every 3 seconds)")}");
                    Console.WriteLine("---");

                    if (useStreaming)
                    {
                        await ProcessWithStreaming(client, prompt);
                    }
                    else
                    {
                        await ProcessWithPolling(client, prompt);
                    }

                    Console.WriteLine("\n" + new string('=', 50) + "\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Error occurred: {ex.Message}");
                    Console.WriteLine("Make sure the server is running on http://localhost:5167\n");
                }
            }
        }

        static async Task ProcessWithPolling(AiTextService.AiTextServiceClient client, string prompt)
        {
            // Submit request
            var submitRequest = new TextGenerationRequest
            {
                Prompt = prompt,
                PreviousRequestId = ""
            };

            var submitResponse = await client.SubmitRequestAsync(submitRequest);
            Console.WriteLine($"✓ Request submitted! ID: {submitResponse.RequestId}");
            Console.WriteLine($"Initial Status: {submitResponse.Status}");

            // Poll for status until completion
            string requestId = submitResponse.RequestId;
            int pollCount = 0;
            
            while (true)
            {
                await Task.Delay(3000); // Wait 3 seconds between polls
                pollCount++;
                
                var statusRequest = new StatusRequest { RequestId = requestId };
                var statusResponse = await client.GetRequestStatusAsync(statusRequest);
                
                Console.WriteLine($"Poll #{pollCount} - Status: {statusResponse.Status}");
                
                if (statusResponse.Status == "COMPLETE")
                {
                    Console.WriteLine($"\n✅ RESULT:");
                    Console.WriteLine($"{statusResponse.Result}");
                    break;
                }
                else if (statusResponse.Status == "FAILED")
                {
                    Console.WriteLine($"\n❌ FAILED: {statusResponse.Result}");
                    break;
                }
            }
        }

        static async Task ProcessWithStreaming(AiTextService.AiTextServiceClient client, string prompt)
        {
            // Submit request
            var submitRequest = new TextGenerationRequest
            {
                Prompt = prompt,
                PreviousRequestId = ""
            };

            var submitResponse = await client.SubmitRequestAsync(submitRequest);
            Console.WriteLine($"✓ Request submitted! ID: {submitResponse.RequestId}");
            
            // Stream status updates in real-time
            var streamRequest = new StatusRequest { RequestId = submitResponse.RequestId };
            using var streamingCall = client.StreamRequestStatus(streamRequest);
            
            Console.WriteLine("📡 Receiving real-time updates...");
            
            while (await streamingCall.ResponseStream.MoveNext(CancellationToken.None))
            {
                var update = streamingCall.ResponseStream.Current;
                Console.WriteLine($"📡 Status: {update.Status}");
                
                if (update.Status == "COMPLETE")
                {
                    Console.WriteLine($"\n✅ RESULT:");
                    Console.WriteLine($"{update.Result}");
                    break;
                }
                else if (update.Status == "FAILED")
                {
                    Console.WriteLine($"\n❌ FAILED: {update.Result}");
                    break;
                }
            }
        }
    }
}