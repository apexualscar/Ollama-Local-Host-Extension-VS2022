using System;
using System.Collections.Generic;

namespace OllamaLocalHostIntergration.Models
{
    /// <summary>
    /// Represents a saved conversation with Ollama
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// Unique identifier for this conversation
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display title for the conversation
        /// </summary>
        public string Title { get; set; } = "New Conversation";

        /// <summary>
        /// When this conversation was created
        /// </summary>
        public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>
        /// When this conversation was last modified
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// The Ollama model used in this conversation
        /// </summary>
        public string ModelUsed { get; set; }

        /// <summary>
        /// The interaction mode (Ask or Agent)
        /// </summary>
        public InteractionMode Mode { get; set; }

        /// <summary>
        /// All messages in this conversation
        /// </summary>
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Estimated total tokens used
        /// </summary>
        public int TokensUsed { get; set; }

        /// <summary>
        /// User-defined tags for categorization
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
