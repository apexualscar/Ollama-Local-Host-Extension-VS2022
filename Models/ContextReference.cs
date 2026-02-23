using System;
using System.ComponentModel;

namespace OllamaLocalHostIntergration.Models
{
    /// <summary>
    /// Represents a context reference type
    /// </summary>
    public enum ContextReferenceType
    {
        /// <summary>
        /// File from solution
        /// </summary>
        File,
        
        /// <summary>
        /// Current editor selection
        /// </summary>
        Selection,
        
        /// <summary>
        /// Specific method
        /// </summary>
        Method,
        
        /// <summary>
        /// Specific class
        /// </summary>
        Class,
        
        /// <summary>
        /// Entire solution
        /// </summary>
        Solution,
        
        /// <summary>
        /// Specific project
        /// </summary>
        Project
    }

    /// <summary>
    /// Represents a context reference that can be added to the chat
    /// </summary>
    public class ContextReference : INotifyPropertyChanged
    {
        private ContextReferenceType _type;
        private string _displayText;
        private string _filePath;
        private string _className;
        private string _methodName;
        private string _projectName;
        private string _content;
        private int _tokenCount;

        public event PropertyChangedEventHandler PropertyChanged;

        public ContextReferenceType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(TypeLabel));
            }
        }

        public string DisplayText
        {
            get => _displayText;
            set
            {
                _displayText = value;
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public string ClassName
        {
            get => _className;
            set
            {
                _className = value;
                OnPropertyChanged(nameof(ClassName));
            }
        }

        public string MethodName
        {
            get => _methodName;
            set
            {
                _methodName = value;
                OnPropertyChanged(nameof(MethodName));
            }
        }

        public string ProjectName
        {
            get => _projectName;
            set
            {
                _projectName = value;
                OnPropertyChanged(nameof(ProjectName));
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public int TokenCount
        {
            get => _tokenCount;
            set
            {
                _tokenCount = value;
                OnPropertyChanged(nameof(TokenCount));
            }
        }

        /// <summary>
        /// Gets an icon for the context reference type
        /// </summary>
        public string Icon
        {
            get
            {
                return Type switch
                {
                    ContextReferenceType.File => "\uE8A5",      // Document
                    ContextReferenceType.Class => "\uE8D3",     // Class
                    ContextReferenceType.Method => "\uE8E3",    // Method
                    ContextReferenceType.Selection => "\uE8E6", // Selection
                    ContextReferenceType.Project => "\uE8F1",   // Project
                    _ => "\uE8A5"
                };
            }
        }

        /// <summary>
        /// Phase 6.5: Gets a label for the context reference type
        /// </summary>
        public string TypeLabel
        {
            get
            {
                return Type switch
                {
                    ContextReferenceType.File => "FILE",
                    ContextReferenceType.Class => "CLASS",
                    ContextReferenceType.Method => "METHOD",
                    ContextReferenceType.Selection => "SELECTION",
                    ContextReferenceType.Project => "PROJECT",
                    _ => "ITEM"
                };
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
