using System.ComponentModel;

namespace OllamaLocalHostIntergration.Models
{
    /// <summary>
    /// Model for file context list items
    /// </summary>
    public class FileContextItem : INotifyPropertyChanged
    {
        private string _filePath;
        private string _fileName;
        private int _tokenCount;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
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

        public string DisplayText => $"{FileName} (~{TokenCount} tokens)";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
