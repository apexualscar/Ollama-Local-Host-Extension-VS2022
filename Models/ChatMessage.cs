using System;

namespace OllamaLocalHostIntergration.Models
{
    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }

        public ChatMessage(string content, bool isUser)
        {
            Content = content;
            IsUser = isUser;
            Timestamp = DateTime.Now;
        }
    }
}