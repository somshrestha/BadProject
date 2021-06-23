namespace ThirdParty
{
    // Imaginary third party component
    // We need to use this, but modifications are not allowed (third party component)
    public class NoSqlAdvProvider
    {
        public Advertisement GetAdv(string webId)
        {
            return new Advertisement() { WebId = webId, Name = $"Advertisement #{webId}" };
        }
    }
}