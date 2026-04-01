namespace BancsHashGenrator.ViewModels
{
    public class UploadCategory
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DisplayName => $"{Code}: {Description}";
    }
}
