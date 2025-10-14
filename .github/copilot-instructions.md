# Copilot Instructions for OpenAI Web API Service

## Project Overview
This is an **asynchronous OpenAI text generation web API service** demonstrating distributed systems concepts including Producer-Consumer patterns, rate limiting, and async processing. The system decouples client requests from AI processing through a queue-based architecture.

## Architecture Pattern: Producer-Consumer with Rate Limiting
```
Clients → AiController → RequestQueueService → OpenAIProcessorService → Azure OpenAI
         (Producer)      (Queue)              (Consumer)
```

**Key Principle**: Clients get immediate response with request ID, then poll for results. Background service processes requests with 45-second rate limiting.

## Core Components & Responsibilities

### `AiController.cs` - HTTP Gateway
- `POST /api/generate` - Accepts prompts, returns request ID immediately
- `GET /api/status/{id}` - Polling endpoint for request status/results
- **Pattern**: Always validate input and return appropriate HTTP status codes

### `RequestQueueService.cs` - State Manager (Singleton)
- Thread-safe state using `ConcurrentDictionary<string, RequestState>`
- FIFO queue via `OrderBy(r => r.SubmissionTime)`
- **Critical**: All state operations must be thread-safe for concurrent access

### `OpenAIProcessorService.cs` - Background Worker
- Rate limiting: minimum 45-second intervals between Azure API calls
- Continuous polling loop processing PENDING requests
- **Pattern**: Always update `LastAzureCallTime` BEFORE making API calls

## State Lifecycle Management
```
Request States: PENDING → PROCESSING → COMPLETE/FAILED
```

**Key Pattern**: Use `UpdateStatus()` for all state transitions:
```csharp
_queueService.UpdateStatus(requestId, "PROCESSING");
// ... process request ...
_queueService.UpdateStatus(requestId, "COMPLETE", result);
```

## Rate Limiting Implementation
The system enforces 45-second intervals between Azure OpenAI calls:
```csharp
var timeSinceLastCall = DateTime.UtcNow - _queueService.LastAzureCallTime;
if (timeSinceLastCall < MinWaitTime) {
    await Task.Delay(MinWaitTime - timeSinceLastCall, stoppingToken);
}
```

## Azure OpenAI Integration Pattern
```csharp
var client = new OpenAIClient(new AzureKeyCredential(azureApiKey), 
                              new OpenAIClientOptions { Endpoint = new Uri(azureEndpoint) });
var chatClient = client.GetChatClient(deploymentName);
```

**Configuration Location**: Hardcoded in `OpenAIProcessorService.cs` (development only)

## Service Registration Pattern (Program.cs)
```csharp
builder.Services.AddSingleton<RequestQueueService>(); // Singleton for shared state
builder.Services.AddHostedService<OpenAIProcessorService>(); // Background processing
```

## Development Workflow

### Build & Run
```bash
dotnet build
dotnet run  # Runs on http://localhost:5166
```

### Testing with REST Client
Use `webapi.http` file with VS Code REST Client extension:
```http
POST http://localhost:5166/api/generate
Content-Type: application/json

{"prompt": "Your prompt here"}
```

### Debugging
- Console logging extensively used (see `[DEBUG]` messages)
- Check background service processing via console output
- Monitor rate limiting behavior through timestamps

## Error Handling Patterns
- **API Level**: Return appropriate HTTP status codes (400, 404, 200)
- **Processing Level**: Catch exceptions, update status to "FAILED" with error message
- **Client Level**: Errors returned through normal polling mechanism

## Thread Safety Requirements
- All shared state operations use `ConcurrentDictionary`
- Background service is single-threaded by design
- Multiple API requests handled concurrently

## Key Files for Understanding
- `DISTRIBUTED_SYSTEMS_CONCEPTS.md` - Detailed explanation of implemented patterns
- `IMPLEMENTATION_REPORT.md` - Complete architectural documentation
- `Models/RequestState.cs` - Core data structures

## Common Tasks
- **Adding new endpoints**: Follow RESTful patterns in `AiController.cs`
- **Modifying processing logic**: Update `OpenAIProcessorService.ExecuteAsync()`
- **State management**: Always use thread-safe operations in `RequestQueueService`
- **Configuration changes**: Update hardcoded values in service classes (move to config for production)

## Production Considerations
- Move Azure credentials to environment variables/Key Vault
- Implement persistent storage (currently in-memory only)
- Add authentication and authorization
- Enable HTTPS redirection
- Add structured logging framework

## Testing Strategy
- Manual testing via REST client
- Monitor console output for processing flow
- Test rate limiting by submitting multiple requests
- Verify error scenarios with invalid inputs