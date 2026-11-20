namespace Snake.Application.ViewModel
{
    public class FileViewModel
    {
        public bool Result { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public byte[] Bytes { get; set; }
        public string Base64 { get; set; }
    }
}
