namespace CheckSum
{
    public interface ICheckSumPathProvider
    {
        string DetailedChecksumFileName { get; }
        string GlobalChecksumFileName { get; }

        string PackagePath { get; }
    }
}