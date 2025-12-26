using System;
using System.Windows;
using System.Windows.Controls;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Controls
{
    /// <summary>
    /// Interaction logic for ContextChipControl.xaml
    /// </summary>
    public partial class ContextChipControl : UserControl
    {
        public ContextChipControl()
        {
            InitializeComponent();
        }

        private void RemoveClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is ContextReference contextRef)
            {
                // Find the parent MyToolWindowControl and remove from its collection
                // We'll use a different approach - raise a routed event
                var args = new RoutedEventArgs(RemoveContextEvent, contextRef);
                RaiseEvent(args);
            }
        }

        // Define routed event for removal
        public static readonly RoutedEvent RemoveContextEvent = EventManager.RegisterRoutedEvent(
            "RemoveContext",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ContextChipControl));

        // Provide CLR accessors for the event
        public event RoutedEventHandler RemoveContext
        {
            add { AddHandler(RemoveContextEvent, value); }
            remove { RemoveHandler(RemoveContextEvent, value); }
        }
    }
}
