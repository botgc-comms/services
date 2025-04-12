namespace Services
{
    public class AppSettings
    {
        public string APIUrlrophyFilePath { get; set; }

        public API AzureFaceApi { get; set; } = new API();
    }

    public class API
    {
        public string XApiKey { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
