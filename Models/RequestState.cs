using System;
using System.Collections.Generic;
using OpenAI.Chat;

namespace WebApiServer.Models
{
    public class RequestState
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string ClientPrompt { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public string? Result { get; set; }
        public DateTime SubmissionTime { get; set; } = DateTime.UtcNow;
        // public List<OpenAI.Chat.ChatMessage> ConversationHistory { get; set; } = new List<OpenAI.Chat.ChatMessage>();
    }

    // Model for incoming request from the client app
    public class ClientRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string? PreviousRequestId { get; set; }
    }

    public class ChatMessage
    {
        public string Sender { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}