# OpenAI Web API Service

An asynchronous web API service that integrates with Azure OpenAI for text generation, demonstrating distributed systems concepts including Producer-Consumer patterns, rate limiting, and async processing.

## 🏗️ Architecture

```
Clients → AiController → RequestQueueService → OpenAIProcessorService → Azure OpenAI
         (Producer)      (Queue)              (Consumer)
```

**Key Features:**
- **Asynchronous Processing**: Immediate response with request ID, background AI processing
- **Rate Limiting**: 45-second intervals between Azure OpenAI API calls
- **Thread-Safe State Management**: Concurrent request handling with FIFO processing
- **RESTful API Design**: Standard HTTP endpoints for request submission and status polling

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Azure OpenAI account with API key

### Build & Run
```bash
dotnet build
dotnet run
```

The API will be available at `http://localhost:5166`

### Usage Example

**Submit a request:**
```http
POST http://localhost:5166/api/generate
Content-Type: application/json

{
    "prompt": "Explain quantum computing in simple terms"
}
```

**Response:**
```json
{
    "requestId": "123e4567-e89b-12d3-a456-426614174000",
    "status": "PENDING"
}
```

**Poll for results:**
```http
GET http://localhost:5166/api/status/123e4567-e89b-12d3-a456-426614174000
```

## 📁 Project Structure

```
├── Controllers/
│   └── AiController.cs          # REST API endpoints
├── Models/
│   ├── RequestState.cs          # Data models
│   └── RequestQueueService.cs   # State management & queue
├── Services/
│   └── OpenAIProcessorService.cs # Background AI processing
├── .github/
│   └── copilot-instructions.md  # AI coding agent guidance
├── DISTRIBUTED_SYSTEMS_CONCEPTS.md # Architecture explanation
├── IMPLEMENTATION_REPORT.md     # Detailed technical documentation
└── webapi.http                 # REST client testing file
```

## 🔧 Core Components

### AiController
- `POST /api/generate` - Submit new text generation request
- `GET /api/status/{id}` - Poll request status and retrieve results

### RequestQueueService (Singleton)
- Thread-safe request state management using `ConcurrentDictionary`
- FIFO queue processing for pending requests
- Rate limiting timestamp tracking

### OpenAIProcessorService (Background Service)
- Continuous polling of request queue
- Rate-limited Azure OpenAI API integration
- Error handling and status updates

## 🛡️ State Management

Request lifecycle:
```
PENDING → PROCESSING → COMPLETE/FAILED
```

All state operations are thread-safe, supporting concurrent client requests while maintaining data integrity.

## ⚡ Rate Limiting

The system enforces a minimum 45-second interval between Azure OpenAI API calls to:
- Protect external service from overload
- Control API usage costs
- Ensure stable system performance

## 🧪 Testing

Use the included `webapi.http` file with VS Code REST Client extension for manual testing:

1. Submit a request using the POST endpoint
2. Copy the returned request ID
3. Poll the status endpoint until completion
4. Verify error handling with invalid requests

## 📖 Documentation

- **[Distributed Systems Concepts](DISTRIBUTED_SYSTEMS_CONCEPTS.md)** - Detailed explanation of implemented patterns
- **[Implementation Report](IMPLEMENTATION_REPORT.md)** - Complete architectural documentation
- **[Copilot Instructions](.github/copilot-instructions.md)** - AI coding agent guidance

## 🔮 Distributed Systems Concepts Demonstrated

1. **Asynchronous Processing** - Non-blocking request handling
2. **Producer-Consumer Pattern** - Queue-based request processing
3. **State Management** - Centralized, thread-safe state store
4. **Rate Limiting** - Flow control for external service protection
5. **RESTful Communication** - Standard HTTP-based interfaces
6. **Service-Oriented Architecture** - Modular, loosely coupled design
7. **Error Handling** - Graceful failure management
8. **Concurrency Control** - Thread-safe operations

## 🏫 Academic Context

This project was developed as part of the Distributed Systems course at Kaunas Technical University, demonstrating practical application of theoretical distributed systems concepts in a real-world scenario.

## ⚠️ Development Notes

- Azure OpenAI credentials are currently hardcoded (development only)
- In-memory state storage (suitable for demonstration purposes)
- HTTPS disabled in development configuration
- Extensive console logging for debugging and learning

## 🚀 Production Considerations

For production deployment, consider:
- Moving credentials to environment variables or Azure Key Vault
- Implementing persistent storage (database)
- Adding authentication and authorization
- Enabling HTTPS redirection
- Implementing structured logging
- Adding monitoring and health checks

## 📝 License

This project is for educational purposes as part of university coursework.