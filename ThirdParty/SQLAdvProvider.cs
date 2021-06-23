namespace ThirdParty
{
    // Imaginary third party component
    // We need to use this, but modifications are not allowed (third party component)
    public static class SQLAdvProvider
    {
        public static Advertisement GetAdv(string webId)
        {
            return new Advertisement() { WebId = webId, Name = $"Advertisement #{webId}" };
        }
    }
}
