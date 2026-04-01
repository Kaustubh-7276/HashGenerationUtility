using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BancsHashGenrator.ViewModels
{
    public class SingleHashViewModel : ViewModelBase
    {
        // 1. Private Fields
        private static readonly string RepoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Repository");
        private bool _isProcessing;
        private string _selectedAlgorithm = "SHA256";
        private string _uploadBranch = "";
        private string _filePath = "";
        private string _resultHash = "";
        private string _passKey = "";
        private string _newFileName = "";
        private UploadCategory? _selectedCategory;

        // 2. Public Properties (Bindings)
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public string SelectedAlgorithm
        {
            get => _selectedAlgorithm;
            set { _selectedAlgorithm = value; OnPropertyChanged(); }
        }

        public string UploadBranch
        {
            get => _uploadBranch;
            set { _uploadBranch = value; OnPropertyChanged(); }
        }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public string ResultHash
        {
            get => _resultHash;
            set { _resultHash = value; OnPropertyChanged(); }
        }

        public string PassKey
        {
            get => _passKey;
            set { _passKey = value; OnPropertyChanged(); }
        }

        public string NewFileName
        {
            get => _newFileName;
            set { _newFileName = value; OnPropertyChanged(); }
        }

        public UploadCategory? SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public bool CanInput => string.IsNullOrEmpty(ResultHash) && !IsProcessing;
        public bool CanReset => !string.IsNullOrEmpty(ResultHash) && !IsProcessing;

        public ObservableCollection<UploadCategory> Categories { get; set; } = new ObservableCollection<UploadCategory>();

        // 3. Commands
        public RelayCommand BrowseCommand { get; }
        public RelayCommand GenerateCommand { get; }
        public RelayCommand CopyClipboardCommand { get; }
        public RelayCommand SaveFileCommand { get; }
        public RelayCommand PrintReportCommand { get; }

        public RelayCommand ResetCommand { get; }

        // 4. Constructor
        public SingleHashViewModel()
        {
            Header = "File Hash Generation";

            // Initialize Lists
            LoadCategoriesFromFile();

            // Initialize Commands
            BrowseCommand = new RelayCommand(_ =>
            {
                var openFileDialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt" };
                if (openFileDialog.ShowDialog() == true) FilePath = openFileDialog.FileName;
            });

            GenerateCommand = new RelayCommand(async _ => await ProcessHashAsync());

            CopyClipboardCommand = new RelayCommand(_ =>
            {
                if (string.IsNullOrEmpty(ResultHash)) return;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Original File Name: {Path.GetFileName(FilePath)}");
                sb.AppendLine($"Generated File Name: {NewFileName}");
                sb.AppendLine($"Branch: {UploadBranch}");
                sb.AppendLine($"PassKey: {PassKey}");
                sb.AppendLine($"File Hash Value: {ResultHash}");
                Clipboard.SetText(sb.ToString());
                MessageBox.Show("Report data copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            SaveFileCommand = new RelayCommand(_ =>
            {
                if (string.IsNullOrEmpty(ResultHash)) return;
                var sfd = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt", FileName = NewFileName };
                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        File.WriteAllText(sfd.FileName, File.ReadAllText(FilePath));
                        MessageBox.Show("File saved successfully!");
                    }
                    catch (Exception ex) { MessageBox.Show("Save error: " + ex.Message); }
                }
            });

            PrintReportCommand = new RelayCommand(_ =>
            {
                if (string.IsNullOrEmpty(ResultHash)) return;
                PrintHashReport();
                //PrintDialog pd = new PrintDialog();
                //if (pd.ShowDialog() == true)
                //{
                //    FlowDocument doc = new FlowDocument(new Paragraph(new Run("SBI FILE HASH REPORT"))) { PagePadding = new Thickness(50) };
                //    doc.Blocks.Add(new Paragraph(new Run($"Date: {DateTime.Now:dd-MMM-yyyy HH:mm:ss}")));
                //    doc.Blocks.Add(new Paragraph(new Run($"Original File: {Path.GetFileName(FilePath)}")));
                //    doc.Blocks.Add(new Paragraph(new Run($"Branch Code: {UploadBranch}")));
                //    doc.Blocks.Add(new Paragraph(new Run($"Security PassKey: {PassKey}")) { FontWeight = FontWeights.Bold });
                //    doc.Blocks.Add(new Paragraph(new Run($"Hash Value: {ResultHash}")) { Foreground = System.Windows.Media.Brushes.DarkBlue });
                //    pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Hash Report");
                //}
            });

            ResetCommand = new RelayCommand(_ =>
            {
                ResultHash = "";
                FilePath = "";
                UploadBranch = "";
                SelectedCategory = null;
                // Notify UI to enable/disable buttons immediately
                RefreshButtonStates();
            });
        }
        private void RefreshButtonStates()
        {
            OnPropertyChanged(nameof(CanInput));
            OnPropertyChanged(nameof(CanReset));
        }

        private void PrintHashReport()
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                // Container
                Grid reportGrid = new Grid { Width = 793, Height = 1122, Background = Brushes.White, Margin = new Thickness(40) };

                // 1. CONFIDENTIAL Watermark
                TextBlock watermark = new TextBlock
                {
                    Text = "CONFIDENTIAL",
                    FontSize = 80,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0)),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    RenderTransform = new RotateTransform(-45),
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };
                reportGrid.Children.Add(watermark);

                // 2. Content
                StackPanel content = new StackPanel();
                content.Children.Add(new TextBlock { Text = "FILE HASH REPORT", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Brushes.DarkBlue });
                content.Children.Add(new TextBlock { Text = "State Bank of India - Confidential", FontSize = 10, Margin = new Thickness(0, 0, 0, 30) });

                // Add Data Rows (File Name, Branch, Passkey, etc.)
                // ... (Use TextBlocks for OrignalFileName, GeneratedFileName, etc.)

                // 3. Signature Area
                content.Children.Add(new TextBlock { Text = "Authorised Official Signature", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 50, 0, 0), FontWeight = FontWeights.Bold });

                // 4. Footer
                content.Children.Add(new TextBlock { Text = $"TELLER ID: 2413166 | PRINT REPORT Timestamp: {DateTime.Now:dd-MMM-yyyy HH:mm:ss}", FontSize = 9, Foreground = Brushes.Gray, Margin = new Thickness(0, 20, 0, 0) });

                reportGrid.Children.Add(content);
                pd.PrintVisual(reportGrid, "SBI Hash Report");
            }
        }


        // 5. Core Logic (The Generate Method)
        private async Task ProcessHashAsync()
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                MessageBox.Show("Please select a valid .txt file."); return;
            }

            string fileNameOnly = Path.GetFileNameWithoutExtension(FilePath);
            if (fileNameOnly.Length > 20 || !Regex.IsMatch(fileNameOnly, @"^[a-zA-Z0-9_]+$"))
            {
                MessageBox.Show("Filename must be Alphanumeric and max 20 chars."); return;
            }

            IsProcessing = true;
            //ResultHash = ""; // Reset for visual effect
            RefreshButtonStates();

            try
            {
                await Task.Run(() =>
                {
                    // Mimic work for progress bar visibility
                    Task.Delay(1200).Wait();

                    // Read content
                    string fileContent = File.ReadAllText(FilePath);
                    byte[] arrData = Encoding.UTF8.GetBytes(fileContent);

                    // Choose Algorithm
                    byte[] hashBytes;
                    using (var provider = (SelectedAlgorithm == "MD5") ? (HashAlgorithm)MD5.Create() : SHA256.Create())
                    {
                        hashBytes = provider.ComputeHash(arrData);
                    }

                    // Convert to Hex
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes) sb.Append(b.ToString("X2"));

                    ResultHash = sb.ToString();

                    // Extract PassKey (5, 8, 15)
                    if (ResultHash.Length >= 17)
                    {
                        PassKey = ResultHash.Substring(5, 2) + ResultHash.Substring(8, 2) + ResultHash.Substring(15, 2);
                    }

                    // Handle Local Repository
                    NewFileName = $"{fileNameOnly}-[{ResultHash}].txt";
                    if (!Directory.Exists(RepoPath)) Directory.CreateDirectory(RepoPath);
                    File.Copy(FilePath, Path.Combine(RepoPath, NewFileName), true);
                });
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            finally
            {
                IsProcessing = false;
                RefreshButtonStates();
            }
        }

        // 6. JSON Loading Logic
        private void LoadCategoriesFromFile()
        {
            try
            {
                string fileName = "UploadTypes.json";
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                DirectoryInfo? directory = new DirectoryInfo(currentDir);

                while (directory != null && !File.Exists(Path.Combine(directory.FullName, fileName)))
                {
                    directory = directory.Parent;
                }

                string filePath = directory != null ? Path.Combine(directory.FullName, fileName) : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var list = JsonSerializer.Deserialize<List<UploadCategory>>(jsonContent, options);
                    if (list != null)
                    {
                        Categories.Clear();
                        foreach (var item in list) Categories.Add(item);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error loading categories: {ex.Message}"); }
        }
    }
}
