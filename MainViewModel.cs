using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace BancsHashGenrator.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ObservableCollection<ViewModelBase> TabItems { get; set; }
        private ViewModelBase _selectedTab;
        public ViewModelBase SelectedTab
        {
            get => _selectedTab;
            set { _selectedTab = value; OnPropertyChanged(); }
        }

        public RelayCommand SwitchTabCommand { get; }
        public MainViewModel()
        {
            // 1. Setup Data
            var sharedList = new ObservableCollection<UploadCategory>();

            var hashVM = new SingleHashViewModel();
            hashVM.Categories = sharedList;

            var manageVM = new ManageCategoriesViewModel(sharedList);

            TabItems = new ObservableCollection<ViewModelBase> { hashVM, manageVM };

            // 2. Set Default Tab (Index 0)
            SelectedTab = TabItems[0];

            // 3. FIXED: Point the command to the validation method
            SwitchTabCommand = new RelayCommand(p => ValidateAndSwitch(p));
        }

        private void ValidateAndSwitch(object? parameter)
        {
            var passwordBox = parameter as PasswordBox;

            if (passwordBox?.Password == "SBI@123")
            {
                SelectedTab = TabItems[1]; // Switch to Manage Categories
                passwordBox.Clear();

                // Optional: Let the Manage Tab know it's unlocked
                if (TabItems[1] is ManageCategoriesViewModel manageVM)
                {
                    manageVM.IsAuthenticated = true;
                }
            }
            else
            {
                MessageBox.Show("Access Denied: Invalid Admin Password", "Security", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
