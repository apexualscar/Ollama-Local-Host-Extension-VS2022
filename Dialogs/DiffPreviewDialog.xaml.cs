using System;
using System.Linq;
using System.Windows;
using OllamaLocalHostIntergration.Models;
using OllamaLocalHostIntergration.Services;

namespace OllamaLocalHostIntergration.Dialogs
{
    public partial class DiffPreviewDialog : Window
    {
        private readonly CodeEdit _codeEdit;
        public bool WasApplied { get; private set; }

        public DiffPreviewDialog(CodeEdit codeEdit)
        {
            InitializeComponent();
            _codeEdit = codeEdit ?? throw new ArgumentNullException(nameof(codeEdit));
            WasApplied = false;
            
            LoadCodeEdit();
        }

        private void LoadCodeEdit()
        {
            // Set description
            txtDescription.Text = string.IsNullOrEmpty(_codeEdit.Description) 
                ? "No description provided." 
                : _codeEdit.Description;

            // Set original and modified code
            txtOriginalCode.Text = _codeEdit.OriginalCode ?? string.Empty;
            txtModifiedCode.Text = _codeEdit.ModifiedCode ?? string.Empty;

            // Generate unified diff
            txtUnifiedDiff.Text = GenerateUnifiedDiff();

            // Set file info
            txtFileInfo.Text = string.IsNullOrEmpty(_codeEdit.FilePath) 
                ? "File: Active document" 
                : $"File: {_codeEdit.FilePath}";

            // Calculate statistics
            var originalLines = (_codeEdit.OriginalCode ?? string.Empty).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var modifiedLines = (_codeEdit.ModifiedCode ?? string.Empty).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            int added = 0, removed = 0, changed = 0;
            CalculateDiffStats(originalLines, modifiedLines, out added, out removed, out changed);
            
            txtStats.Text = $"Changes: +{added} lines added, -{removed} lines removed, ~{changed} lines modified";
        }

        private string GenerateUnifiedDiff()
        {
            var service = new CodeModificationService(new CodeEditorService());
            return service.GeneratePreviewDiff(_codeEdit);
        }

        private void CalculateDiffStats(string[] original, string[] modified, out int added, out int removed, out int changed)
        {
            added = 0;
            removed = 0;
            changed = 0;

            int maxLength = Math.Max(original.Length, modified.Length);
            int minLength = Math.Min(original.Length, modified.Length);

            // Count changed lines
            for (int i = 0; i < minLength; i++)
            {
                if (original[i] != modified[i])
                {
                    changed++;
                }
            }

            // Count added or removed lines
            if (modified.Length > original.Length)
            {
                added = modified.Length - original.Length;
            }
            else if (original.Length > modified.Length)
            {
                removed = original.Length - modified.Length;
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            WasApplied = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            WasApplied = false;
            DialogResult = false;
            Close();
        }
    }
}
