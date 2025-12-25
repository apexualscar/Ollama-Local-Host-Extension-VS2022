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

        public ChatMessage(string content, bool isUser)
        {
            Content = content;
            IsUser = isUser;
            Timestamp = DateTime.Now;
            HasCodeBlocks = false;
            CodeBlocks = new List<CodeBlock>();
            IsApplicable = false;
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