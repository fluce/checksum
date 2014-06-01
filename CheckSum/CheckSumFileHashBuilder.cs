using System.IO;
using System.Linq;

namespace CheckSum
{
    public class CheckSumFileHashBuilder: IHashBuilder
    {
        Options Options { get; set; }

        public CheckSumFileHashBuilder(Options options)
        {
            Options = options;
        }

        public HashResult BuildHash()
        {
            HashResult expectedResult = new HashResult();
            // Check global hash
            expectedResult.GlobalHash = File.ReadAllText(Options.ResolvedGlobalChecksumFileName);
            expectedResult.DetailedHashValues = File.ReadAllLines(Options.ResolvedDetailedChecksumFileName)
                .Select(x => x.TrimEnd('\r', '\n').Split(';'))
                .Where(x => x.Length == 2)
                .Select(x => new HashValue { FileName = x[0], Hash = x[1] })
                .ToList();

            return expectedResult;
        }
    }
}