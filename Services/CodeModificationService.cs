using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for parsing AI responses and applying code modifications
    /// </summary>
    public class CodeModificationService
    {
        private readonly CodeEditorService _editorService;

        public CodeModificationService(CodeEditorService editorService)
        {
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        }

        /// <summary>
        /// Extracts code blocks from AI response
        /// </summary>
        public List<(string language, string code)> ExtractCodeBlocks(string aiResponse)
        {
            var codeBlocks = new List<(string language, string code)>();
            
            // Match markdown code blocks with optional language specifier
            var pattern = @"```(\w+)?\s*\n(.*?)\n```";
            var matches = Regex.Matches(aiResponse, pattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                string language = match.Groups[1].Success ? match.Groups[1].Value : "text";
                string code = match.Groups[2].Value.Trim();
                codeBlocks.Add((language, code));
            }

            return codeBlocks;
        }

        /// <summary>
        /// Creates a CodeEdit object from AI response
        /// </summary>
        public async Task<CodeEdit> CreateCodeEditFromResponseAsync(string aiResponse, string originalCode)
        {
            var codeBlocks = ExtractCodeBlocks(aiResponse);
            
            if (codeBlocks.Count == 0)
            {
                return null;
            }

            // Get the first code block as the modified code
            var (language, modifiedCode) = codeBlocks[0];
            
            // Get current file path
            string filePath = await _editorService.GetActiveDocumentPathAsync();

            // Extract description (text before the first code block)
            var firstCodeBlockIndex = aiResponse.IndexOf("```");
            string description = firstCodeBlockIndex > 0 
                ? aiResponse.Substring(0, firstCodeBlockIndex).Trim() 
                : "Code modification";

            var codeEdit = new CodeEdit
            {
                FilePath = filePath,
                OriginalCode = originalCode,
                ModifiedCode = modifiedCode,
                Description = description
            };

            return codeEdit;
        }

        /// <summary>
        /// Applies a code edit to the active document
        /// </summary>
        public async Task<bool> ApplyCodeEditAsync(CodeEdit codeEdit)
        {
            if (codeEdit == null)
                return false;

            try
            {
                // Check if there's selected text that matches the original code
                var selectedText = await _editorService.GetSelectedTextAsync();
                
                if (!string.IsNullOrEmpty(selectedText))
                {
                    // Replace selected text
                    bool success = await _editorService.ReplaceSelectedTextAsync(codeEdit.ModifiedCode);
                    if (success)
                    {
                        codeEdit.Applied = true;
                        return true;
                    }
                }
                else
                {
                    // No selection, replace entire document
                    bool success = await _editorService.ReplaceDocumentTextAsync(codeEdit.ModifiedCode);
                    if (success)
                    {
                        codeEdit.Applied = true;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Generates a preview diff between original and modified code
        /// </summary>
        public string GeneratePreviewDiff(CodeEdit codeEdit)
        {
            if (codeEdit == null)
                return string.Empty;

            var originalLines = codeEdit.OriginalCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var modifiedLines = codeEdit.ModifiedCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var diff = new System.Text.StringBuilder();
            diff.AppendLine("=== Code Modification Preview ===");
            diff.AppendLine($"Description: {codeEdit.Description}");
            diff.AppendLine();
            diff.AppendLine("--- Original");
            diff.AppendLine("+++ Modified");
            diff.AppendLine();

            int maxLines = Math.Max(originalLines.Length, modifiedLines.Length);
            
            for (int i = 0; i < maxLines; i++)
            {
                string originalLine = i < originalLines.Length ? originalLines[i] : "";
                string modifiedLine = i < modifiedLines.Length ? modifiedLines[i] : "";

                if (originalLine != modifiedLine)
                {
                    if (!string.IsNullOrEmpty(originalLine))
                        diff.AppendLine($"- {originalLine}");
                    if (!string.IsNullOrEmpty(modifiedLine))
                        diff.AppendLine($"+ {modifiedLine}");
                }
                else
                {
                    diff.AppendLine($"  {originalLine}");
                }
            }

            return diff.ToString();
        }

        /// <summary>
        /// Validates that the modified code is syntactically different from original
        /// </summary>
        public bool ValidateCodeEdit(CodeEdit codeEdit)
        {
            if (codeEdit == null)
                return false;

            // Basic validation
            if (string.IsNullOrWhiteSpace(codeEdit.ModifiedCode))
                return false;

            // Check if there's actually a change
            if (codeEdit.OriginalCode.Trim() == codeEdit.ModifiedCode.Trim())
                return false;

            return true;
        }

        /// <summary>
        /// Extracts explanation text from AI response (text outside code blocks)
        /// </summary>
        public string ExtractExplanation(string aiResponse)
        {
            // Remove all code blocks to get just the explanation
            var pattern = @"```(\w+)?\s*\n(.*?)\n```";
            var result = Regex.Replace(aiResponse, pattern, "[CODE BLOCK]", RegexOptions.Singleline);
            return result.Trim();
        }

        /// <summary>
        /// Checks if AI response contains code modifications
        /// </summary>
        public bool ContainsCodeModifications(string aiResponse)
        {
            return Regex.IsMatch(aiResponse, @"```(\w+)?\s*\n", RegexOptions.Multiline);
        }
    }
}
