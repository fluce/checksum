using System.IO;
using System.Linq;

namespace CheckSum
{
    public class CheckSumFileHashBuilder : IHashBuilder, ICheckSumFileAccessor
    {
        ICheckSumPathProvider PathProvider { get; set; }
        public CheckSumFileHashBuilder(ICheckSumPathProvider pathProvider)
        {
            PathProvider = pathProvider;
        }

        public HashResult BuildHash()
        {
            return Read();
        }

        public HashResult Read()
        {
            if (!File.Exists(PathProvider.GlobalChecksumFileName))
                return null;
            if (!File.Exists(PathProvider.GlobalChecksumFileName))
                return null;


            HashResult expectedResult = new HashResult();
            expectedResult.GlobalHash = File.ReadAllText(PathProvider.GlobalChecksumFileName);
            expectedResult.DetailedHashValues = File.ReadAllLines(PathProvider.DetailedChecksumFileName)
                .Select(x => x.TrimEnd('\r', '\n').Split(';'))
                .Where(x => x.Length == 2)
                .Select(x => new HashValue { FileName = x[0], Hash = x[1] })
                .ToList();
            return expectedResult;
        }

        public void Write(HashResult result)
        {
            File.WriteAllText(PathProvider.DetailedChecksumFileName, result.DetailedHash);
            File.WriteAllText(PathProvider.GlobalChecksumFileName, result.GlobalHash);
        }

        public void Clear()
        {
            File.Delete(PathProvider.DetailedChecksumFileName);
            File.Delete(PathProvider.GlobalChecksumFileName);
        }
    }

    public interface ICheckSumFileAccessor
    {
        HashResult Read();
        void Write(HashResult result);

        void Clear();
    }

}