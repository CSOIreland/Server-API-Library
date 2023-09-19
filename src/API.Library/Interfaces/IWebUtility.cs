namespace API
{
    public interface IWebUtility
    {
        string GetRandomMD5(string salsa);
        string GetRandomSHA256(string salsa);
        string GetIP();
        string GetUserAgent();
        string GetUserAcceptLanguage();
    }
}
