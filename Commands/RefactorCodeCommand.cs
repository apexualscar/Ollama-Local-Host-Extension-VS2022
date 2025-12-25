using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using OllamaLocalHostIntergration.Services;

namespace OllamaLocalHostIntergration.Commands
{
    /// <summary>
    /// Command to refactor selected code using Ollama
    /// </summary>
    [Command(PackageIds.RefactorCodeCommand)]
    internal sealed class RefactorCodeCommand : BaseCommand<RefactorCodeCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Get selected text
                var codeEditorService = new CodeEditorService();
                string selectedText = await codeEditorService.GetSelectedTextAsync();

                if (string.IsNullOrEmpty(selectedText))
                {
                    await VS.MessageBox.ShowAsync(
                        "Ollama Copilot",
                        "Please select some code to refactor.",
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_INFO,
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK
                    );
                    return;
                }

                string language = await codeEditorService.GetActiveDocumentLanguageAsync();

                // Open tool window
                var window = await MyToolWindow.ShowAsync();
                if (window != null)
                {
                    var control = window.Content as MyToolWindowControl;
                    if (control != null)
                    {
                        await control.RefactorCodeAsync(selectedText, language);
                    }
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync(
                    "Ollama Copilot Error",
                    $"Failed to refactor code: {ex.Message}"
                );
            }
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Enable command only when text is selected
            Command.Enabled = true;
        }
    }
}
