using System;
using System.Windows;
using System.Windows.Controls;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Controls
{
    /// <summary>
    /// Interaction logic for PendingChangeControl.xaml
    /// </summary>
    public partial class PendingChangeControl : UserControl
    {
        public PendingChangeControl()
        {
            InitializeComponent();
        }

        private void ViewDiffClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is CodeEdit codeEdit)
            {
                var args = new RoutedEventArgs(ViewDiffEvent, codeEdit);
                RaiseEvent(args);
            }
        }

        private void KeepClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is CodeEdit codeEdit)
            {
                var args = new RoutedEventArgs(KeepChangeEvent, codeEdit);
                RaiseEvent(args);
            }
        }

        private void UndoClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is CodeEdit codeEdit)
            {
                var args = new RoutedEventArgs(UndoChangeEvent, codeEdit);
                RaiseEvent(args);
            }
        }

        // Define routed events
        public static readonly RoutedEvent ViewDiffEvent = EventManager.RegisterRoutedEvent(
            "ViewDiff",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PendingChangeControl));

        public static readonly RoutedEvent KeepChangeEvent = EventManager.RegisterRoutedEvent(
            "KeepChange",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PendingChangeControl));

        public static readonly RoutedEvent UndoChangeEvent = EventManager.RegisterRoutedEvent(
            "UndoChange",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PendingChangeControl));

        // Provide CLR accessors for the events
        public event RoutedEventHandler ViewDiff
        {
            add { AddHandler(ViewDiffEvent, value); }
            remove { RemoveHandler(ViewDiffEvent, value); }
        }

        public event RoutedEventHandler KeepChange
        {
            add { AddHandler(KeepChangeEvent, value); }
            remove { RemoveHandler(KeepChangeEvent, value); }
        }

        public event RoutedEventHandler UndoChange
        {
            add { AddHandler(UndoChangeEvent, value); }
            remove { RemoveHandler(UndoChangeEvent, value); }
        }
    }
}
