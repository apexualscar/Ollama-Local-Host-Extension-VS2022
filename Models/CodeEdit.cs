using System;

namespace OllamaLocalHostIntergration.Models
{
    /// <summary>
    /// Represents a code editing operation suggested by the AI
    /// </summary>
    public class CodeEdit
    {
        public string FilePath { get; set; }
        public string OriginalCode { get; set; }
        public string ModifiedCode { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Applied { get; set; }

        public CodeEdit()
        {
            CreatedAt = DateTime.Now;
            Applied = false;
        }

        public CodeEdit(string filePath, string originalCode, string modifiedCode, string description = "")
        {
            FilePath = filePath;
            OriginalCode = originalCode;
            ModifiedCode = modifiedCode;
            Description = description;
            CreatedAt = DateTime.Now;
            Applied = false;
        }
    }
}
