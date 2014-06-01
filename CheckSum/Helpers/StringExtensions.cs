namespace CheckSum.Helpers
{
    public static class StringExtensions
    {
        public static string RemovePrefix(this string a, string p)
        {
            if (a.StartsWith(p))
                return a.Substring(p.Length);
            return a;
        }
    }
}