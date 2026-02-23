using System;
using System.Windows;
using System.Windows.Controls;

namespace OllamaLocalHostIntergration.Dialogs
{
    public partial class ContextTypeSelectionDialog : UserControl
    {
        public event EventHandler<string> TypeSelected;

        public ContextTypeSelectionDialog()
        {
            InitializeComponent();
        }

        private void ActiveDocument_Click(object sender, RoutedEventArgs e)
        {
            TypeSelected?.Invoke(this, "ActiveDocument");
        }

        private void Selection_Click(object sender, RoutedEventArgs e)
        {
            TypeSelected?.Invoke(this, "Selection");
        }

        private void WholeSolution_Click(object sender, RoutedEventArgs e)
        {
            TypeSelected?.Invoke(this, "WholeSolution");
        }

        private void SearchFiles_Click(object sender, RoutedEventArgs e)
        {
            TypeSelected?.Invoke(this, "SearchFiles");
        }

        private void SearchClasses_Click(object sender, RoutedEventArgs e)
        {
            TypeSelected?.Invoke(this, "SearchClasses");
        }

        private void SearchMethods_Click(object sender, RoutedEventArgs e)
        {
            TypeSelected?.Invoke(this, "SearchMethods");
        }
    }
}
