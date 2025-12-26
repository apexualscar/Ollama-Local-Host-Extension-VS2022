using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for parsing and enriching chat messages with code block information
    /// </summary>
    public class MessageParserService
    {
        /// <summary>
        /// Parses a chat message and extracts code blocks
        /// </summary>
        public ChatMessage ParseMessage(string content, bool isUser)
        {
            var message = new ChatMessage(content, isUser);
            
            if (!isUser)
            {
                // Extract code blocks from AI responses
                message.CodeBlocks = ExtractCodeBlocks(content);
                message.HasCodeBlocks = message.CodeBlocks.Count > 0;
            }
            
            return message;
        }
        
        /// <summary>
        /// Extracts code blocks from markdown-formatted text
        /// </summary>
        private List<CodeBlock> ExtractCodeBlocks(string text)
        {
            var codeBlocks = new List<CodeBlock>();
            
            if (string.IsNullOrEmpty(text))
                return codeBlocks;
            
            // Pattern to match markdown code blocks: ```language\ncode\n```
            var pattern = @"```(\w+)?\s*\n(.*?)\n```";
            var matches = Regex.Matches(text, pattern, RegexOptions.Singleline);
            
            foreach (Match match in matches)
            {
                string language = match.Groups[1].Success ? match.Groups[1].Value : "text";
                string code = match.Groups[2].Value;
                
                var codeBlock = new CodeBlock(
                    language,
                    code,
                    match.Index,
                    match.Index + match.Length
                );
                
                codeBlocks.Add(codeBlock);
            }
            
            return codeBlocks;
        }
        
        /// <summary>
        /// Prepares content for display by removing code blocks (they'll be rendered separately)
        /// </summary>
        public string PrepareDisplayContent(ChatMessage message)
        {
            if (!message.HasCodeBlocks)
                return message.Content;
            
            string content = message.Content;
            
            // Remove code blocks from the text content (they'll be displayed separately in code blocks panel)
            var pattern = @"```(\w+)?\s*\n(.*?)\n```";
            content = Regex.Replace(content, pattern, "", RegexOptions.Singleline);
            
            // Clean up any extra whitespace
            content = Regex.Replace(content, @"\n{3,}", "\n\n", RegexOptions.Multiline);
            content = content.Trim();
            
            return content;
        }
        
        /// <summary>
        /// Checks if a message contains code modifications that can be applied
        /// </summary>
        public bool IsApplicableMessage(ChatMessage message)
        {
            return !message.IsUser && message.HasCodeBlocks && message.CodeBlocks.Count > 0;
        }
        
        /// <summary>
        /// Gets formatted code for display with line numbers
        /// </summary>
        public string FormatCodeWithLineNumbers(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;
            
            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var formatted = new System.Text.StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                formatted.AppendLine($"{i + 1,4} | {lines[i]}");
            }
            
            return formatted.ToString();
        }
    }
}
