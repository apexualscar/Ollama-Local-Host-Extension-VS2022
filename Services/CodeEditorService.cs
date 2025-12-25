using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for interacting with Visual Studio code editors
    /// </summary>
    public class CodeEditorService
    {
        /// <summary>
        /// Gets the text of the currently active document
        /// </summary>
        public async Task<string> GetActiveDocumentTextAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDoc)
                {
                    EditPoint editPoint = textDoc.StartPoint.CreateEditPoint();
                    return editPoint.GetText(textDoc.EndPoint);
                }
            }
            catch (Exception)
            {
                // Silently fail if we can't get the document text
            }
            return null;
        }

        /// <summary>
        /// Gets the currently selected text in the active editor
        /// </summary>
        public async Task<string> GetSelectedTextAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDoc)
                {
                    var selection = textDoc.Selection;
                    if (selection != null && !selection.IsEmpty)
                    {
                        return selection.Text;
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail
            }
            return null;
        }

        /// <summary>
        /// Gets the file path of the active document
        /// </summary>
        public async Task<string> GetActiveDocumentPathAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                return dte?.ActiveDocument?.FullName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Replaces the selected text with new text
        /// </summary>
        public async Task<bool> ReplaceSelectedTextAsync(string newText)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDoc)
                {
                    var selection = textDoc.Selection;
                    if (selection != null)
                    {
                        selection.Delete();
                        selection.Insert(newText);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Failed to replace
            }
            return false;
        }

        /// <summary>
        /// Inserts text at the current cursor position
        /// </summary>
        public async Task<bool> InsertTextAtCursorAsync(string text)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDoc)
                {
                    var selection = textDoc.Selection;
                    selection?.Insert(text);
                    return true;
                }
            }
            catch (Exception)
            {
                // Failed to insert
            }
            return false;
        }

        /// <summary>
        /// Replaces text in the entire document
        /// </summary>
        public async Task<bool> ReplaceDocumentTextAsync(string newText)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDoc)
                {
                    EditPoint startPoint = textDoc.StartPoint.CreateEditPoint();
                    startPoint.Delete(textDoc.EndPoint);
                    startPoint.Insert(newText);
                    return true;
                }
            }
            catch (Exception)
            {
                // Failed to replace
            }
            return false;
        }

        /// <summary>
        /// Gets the programming language of the active document
        /// </summary>
        public async Task<string> GetActiveDocumentLanguageAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                var activeDoc = dte?.ActiveDocument;
                if (activeDoc != null)
                {
                    string extension = System.IO.Path.GetExtension(activeDoc.FullName)?.ToLower();
                    return MapExtensionToLanguage(extension);
                }
            }
            catch (Exception)
            {
                // Failed
            }
            return "text";
        }

        private string MapExtensionToLanguage(string extension)
        {
            return extension switch
            {
                ".cs" => "csharp",
                ".vb" => "vb",
                ".fs" => "fsharp",
                ".cpp" or ".cc" or ".cxx" or ".h" or ".hpp" => "cpp",
                ".c" => "c",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".java" => "java",
                ".html" or ".htm" => "html",
                ".css" => "css",
                ".xml" => "xml",
                ".json" => "json",
                ".sql" => "sql",
                ".php" => "php",
                ".rb" => "ruby",
                ".go" => "go",
                ".rs" => "rust",
                ".swift" => "swift",
                ".kt" => "kotlin",
                _ => "text"
            };
        }

        /// <summary>
        /// Gets information about the current selection
        /// </summary>
        public async Task<(int startLine, int endLine, string text)> GetSelectionInfoAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDoc)
                {
                    var selection = textDoc.Selection;
                    if (selection != null && !selection.IsEmpty)
                    {
                        return (selection.TopLine, selection.BottomLine, selection.Text);
                    }
                }
            }
            catch (Exception)
            {
                // Failed
            }
            return (0, 0, null);
        }
    }
}
