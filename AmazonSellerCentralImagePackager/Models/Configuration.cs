namespace AmazonSellerCentralImagePackager.Models
{
    public sealed record Configuration
    {
        public string LastUsedImageCode { get; set; }
        public int LastUsedImagesPerZip { get; set; } = 200;
        public bool OpenOutputFolderWhenDone { get; set; } = true;
    }
}
