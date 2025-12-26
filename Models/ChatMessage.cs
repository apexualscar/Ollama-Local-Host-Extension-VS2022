using System;
using System.Collections.Generic;

namespace OllamaLocalHostIntergration.Models
{
    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Model name for AI responses (e.g., "qwen2.5-coder:3b")
        /// </summary>
        public string ModelName { get; set; }
        
        /// <summary>
        /// Indicates if this message contains code blocks
        /// </summary>
        public bool HasCodeBlocks { get; set; }
        
        /// <summary>
        /// Extracted code blocks from the message content
        /// </summary>
        public List<CodeBlock> CodeBlocks { get; set; }
        
        /// <summary>
        /// Indicates if this is an Agent mode response that can be applied
        /// </summary>
        public bool IsApplicable { get; set; }
        
        /// <summary>
        /// Associated CodeEdit object for Agent mode responses
        /// </summary>
        public CodeEdit AssociatedCodeEdit { get; set; }

        public ChatMessage(string content, bool isUser, string modelName = null)
        {
            Content = content;
            IsUser = isUser;
            ModelName = modelName;
            Timestamp = DateTime.Now;
            HasCodeBlocks = false;
            CodeBlocks = new List<CodeBlock>();
            IsApplicable = false;
        }
        
        /// <summary>
        /// Gets a shortened version of the model name for display
        /// Examples: "qwen2.5-coder:3b" ? "Qwen2.5-coder"
        ///           "codellama:latest" ? "Codellama"
        /// </summary>
        public string ShortModelName
        {
            get
            {
                if (string.IsNullOrEmpty(ModelName))
                    return "Ollama";
                    
                // Remove version/size suffix (e.g., ":3b", ":latest", ":7b")
                string name = ModelName.Split(':')[0];
                
                // Capitalize first letter
                if (!string.IsNullOrEmpty(name))
                {
                    name = char.ToUpper(name[0]) + name.Substring(1);
                }
                
                return name;
            }
        }
    }
    
    /// <summary>
    /// Represents a code block extracted from a chat message
    /// </summary>
    public class CodeBlock
    {
        public string Language { get; set; }
        public string Code { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        
        public CodeBlock(string language, string code, int startIndex = 0, int endIndex = 0)
        {
            Language = language;
            Code = code;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}