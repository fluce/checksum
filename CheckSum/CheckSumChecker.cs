using CheckSum.Helpers;
using System;
using System.IO;
using System.Linq;

namespace CheckSum
{
    public class CheckSumChecker
    {
        IHashBuilder ActualHashBuilder { get; set; }
        IHashBuilder ExpectedHashBuilder { get; set; }

        public CheckSumChecker(IHashBuilder actualHashBuilder, IHashBuilder expectedHashBuilder)
        {
            ActualHashBuilder = actualHashBuilder;
            ExpectedHashBuilder = expectedHashBuilder;
        }

        public HashResult Create(Options options)
        {
            var result=ActualHashBuilder.BuildHash();

            File.WriteAllText(options.ResolvedDetailedChecksumFileName, result.DetailedHash);
            File.WriteAllText(options.ResolvedGlobalChecksumFileName, result.GlobalHash);
            return result;
        }

        public CheckResult Check(Options options)
        {
            if (!File.Exists(options.ResolvedGlobalChecksumFileName))
                return null;
            if (!File.Exists(options.ResolvedDetailedChecksumFileName))
                return null;

            var actualResult=ActualHashBuilder.BuildHash();
            var expectedResult = ExpectedHashBuilder.BuildHash();

            var result = new CheckResult();

            result.GlobalCheck = actualResult.GlobalHash == expectedResult.GlobalHash;
            //result.DetailedCheck =
            var list1 = expectedResult.DetailedHashValues.Select(
                    x => new HashValue {FileName = x.FileName, Hash = x.Hash, HashMatch = HashMatch.Missing}).ToList();
            var list2=actualResult.DetailedHashValues.Select(
                    x => new HashValue {FileName = x.FileName, Hash = x.Hash, HashMatch = HashMatch.Unexpected}).ToList();

            result.DetailedCheck=IEnumerableHelper.Match(list1, list2, 
                (i1, i2) => String.Compare(i1.FileName, i2.FileName, StringComparison.Ordinal),
                (i1, i2) =>
                    new HashValue
                    {
                        FileName = i1.FileName.RemovePrefix(options.ResolvedPackagePath + "\\"),
                        Hash = i1.Hash,
                        HashMatch = i1.Hash == i2.Hash ? HashMatch.Same : HashMatch.Different
                    },
                i=>i)
                .ToList();

            return result;
        }

    }


}