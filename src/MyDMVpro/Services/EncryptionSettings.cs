namespace MyDMVpro.Services
{
    public class EncryptionSettings
    {
        public string Key { get; set; }
        public string IV { get; set; }
        public int NumberToEncrypt { get; set; }
    }

    public class OCRSettings
    {
        public string OCRWebBaseUrl { get; set; }
        public string OCRBackendBaseUrl { get; set; }
    }
}
