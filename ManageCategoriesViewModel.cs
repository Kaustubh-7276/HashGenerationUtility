using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace BancsHashGenrator.ViewModels
{
    public class ManageCategoriesViewModel : ViewModelBase
    {
        private bool _isAuthenticated;
        public ObservableCollection<UploadCategory> Categories { get; set; }
        public string NewCode { get; set; }
        public string NewDescription { get; set; }
        public RelayCommand AddCommand { get; }
        public RelayCommand RemoveCommand { get; }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set { _isAuthenticated = value; OnPropertyChanged(); }
        }

        public RelayCommand UnlockCommand { get; }

        public ManageCategoriesViewModel(ObservableCollection<UploadCategory> sharedCategories)
        {
            Header = "Manage Categories";
            Categories = sharedCategories;
            UnlockCommand = new RelayCommand(p =>
            {
                var passwordBox = p as System.Windows.Controls.PasswordBox;
                if (passwordBox?.Password == "SBI@123")
                {
                    IsAuthenticated = true;
                    passwordBox.Clear();
                }
                else
                {
                    System.Windows.MessageBox.Show("Access Denied: Invalid Admin Password", "Security Alert");
                }
            });
            AddCommand = new RelayCommand(_ => AddCategory());
            RemoveCommand = new RelayCommand(p => RemoveCategory(p as UploadCategory));
        }

        private void AddCategory()
        {
            if (string.IsNullOrWhiteSpace(NewCode)) return;
            Categories.Add(new UploadCategory { Code = NewCode, Description = NewDescription });
            SaveToJson();
            NewCode = ""; NewDescription = ""; // Reset inputs
        }

        private void SaveToJson()
        {
            try
            {
                // Navigate to root to save permanently
                string fileName = "UploadTypes.json";
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                DirectoryInfo? directory = new DirectoryInfo(currentDir);
                while (directory != null && !File.Exists(Path.Combine(directory.FullName, fileName)))
                    directory = directory.Parent;

                string path = directory != null ? Path.Combine(directory.FullName, fileName) : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                string json = JsonSerializer.Serialize(Categories, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { /* Handle errors */ }
        }
        private void RemoveCategory(UploadCategory? category)
        {
            if (category != null)
            {
                Categories.Remove(category);
                SaveToJson();
            }
        }
    }
}
