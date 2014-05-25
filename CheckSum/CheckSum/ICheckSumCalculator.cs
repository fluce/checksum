using System.IO;

namespace CheckSum.CheckSum
{
    public interface ICheckSumCalculator
    {
        string GetHashFromContent(Stream inputStream);
    }
}