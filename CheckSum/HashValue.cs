namespace CheckSum
{

    public enum HashMatch
    {
        Same,
        Different,
        Missing,
        Unexpected
    }

    public class HashValue
    {
        public string FileName { get; set; }
        public string Hash { get; set; }
        public long FileLength { get; set; }

        public HashMatch? HashMatch { get; set; }
    }
}