using CheckSum.CheckSum;
using CheckSum.Helpers;
using CheckSum.ListBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CheckSum
{
    public class CheckSumChecker
    {
        IFileListBuilder FileListBuilder { get; set; }
        ICheckSumCalculator CheckSumCalculator { get; set; }

        public CheckSumChecker(IFileListBuilder fileListBuilder, ICheckSumCalculator checkSumCalculator)
        {
            FileListBuilder = fileListBuilder;
            CheckSumCalculator = checkSumCalculator;
        }

        public HashResult Create(Options options)
        {
            var result=CalculateHash(options);

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

            var actualResult=CalculateHash(options);

            // Check global hash
            var expectedGlobalHash = File.ReadAllText(options.ResolvedGlobalChecksumFileName);
            var expectedFileHashs = File.ReadAllLines(options.ResolvedDetailedChecksumFileName)
                .Select(x => x.TrimEnd('\r', '\n').Split(';'))
                .Where(x => x.Length == 2)
                .Select(x => new HashValue {FileName = x[0], Hash = x[1]})
                .ToList();

            var result = new CheckResult();

            result.GlobalCheck = actualResult.GlobalHash == expectedGlobalHash;
            //result.DetailedCheck =
            var list1=expectedFileHashs.Select(
                    x => new HashValue {FileName = x.FileName, Hash = x.Hash, HashMatch = HashMatch.Missing}).ToList();
            var list2=actualResult.DetailedHashValues.Select(
                    x => new HashValue {FileName = x.FileName, Hash = x.Hash, HashMatch = HashMatch.Unexpected}).ToList();

            result.DetailedCheck=IEnumerableHelper.Match(list1, list2, 
                (i1, i2) => String.Compare(i1.FileName, i2.FileName, StringComparison.Ordinal),
                (i1, i2) =>
                    new HashValue
                    {
                        FileName = RemovePrefix(i1.FileName, options.ResolvedPackagePath + "\\"),
                        Hash = i1.Hash,
                        HashMatch = i1.Hash == i2.Hash ? HashMatch.Same : HashMatch.Different
                    },
                i=>i)
                .ToList();

            return result;
        }

        private HashResult CalculateHash(Options options)
        {
            ProgressManager.Current["GLOBAL"].SetProgress(1,3).SetMessage("Building file list");
            var list = FileListBuilder
                .BuildFileList(options.ResolvedPackagePath)
                .Where(x => x != options.ResolvedDetailedChecksumFileName && x != options.ResolvedGlobalChecksumFileName)
                .Select(x =>
                {
                    ProgressManager.Current["FILELIST"].IncrementProgress();
                    return new {FileName = x, Length = Win32File.GetFileSize(x, false)};
                })
                .Checkpoint(x =>
                {
                    ProgressManager.Current["FILELIST"].Complete();
                    ProgressManager.Current["HASH"].Reset()
                        .SetRenderIndexAsHumanReadableSize()
                        .SetProgress(0, x.Sum(y => y.Length));
                })
                .AsParallel()
                .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .WithDegreeOfParallelism(options.DegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount))
                .Select(x =>
                {
                    ProgressManager.Current["GLOBAL"].SetProgress(2, 3).SetMessage("Calculating hashes");
                    var r = new HashValue
                    {
                        FileName = RemovePrefix(x.FileName, options.ResolvedPackagePath + "\\"),
                        FileLength = x.Length
                    };
                    ProgressManager.Current["HASH"].SetMessage(r.FileName);
                    var progressId = "FILE[" + Thread.CurrentThread.ManagedThreadId + "]";
                    ProgressManager.Current[progressId].Reset()
                        .SetRenderIndexAsHumanReadableSize()
                        .SetDisplayThresholdDuration(TimeSpan.FromSeconds(1));
                    r.Hash = CheckSumCalculator.GetHashFromFile(x.FileName,
                        (i, n) => ProgressManager.Current[progressId].SetProgress(i, n).SetMessage(r.FileName));
                    ProgressManager.Current[progressId].Delete();
                    ProgressManager.Current["HASH"].IncrementProgress(x.Length);
                    return r;
                })
                .OrderBy(x => x.FileName)
                .ToList();
            ProgressManager.Current["HASH"].SetMessage(null).Complete();
            ProgressManager.Current["GLOBAL"].SetProgress(3, 3).SetMessage("Writing hash files");

            var detailedHash = list.Select(x => string.Format("{0};{1}", x.FileName, x.Hash ))
                .Aggregate(new StringBuilder(), (a, i) => a.Append(i + "\r\n")).ToString();
            var globalHash = CheckSumCalculator.GetHashFromString(detailedHash) + "\r\n";

            ProgressManager.Current["GLOBAL"].SetMessage(null).Complete();

            return new HashResult {DetailedHash = detailedHash, GlobalHash = globalHash, DetailedHashValues = list};
        }

        private string RemovePrefix(string a, string p)
        {
            if (a.StartsWith(p))
                return a.Substring(p.Length);
            return a;
        }

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

        public class HashResult
        {
            public List<HashValue> DetailedHashValues;
            public string DetailedHash;
            public string GlobalHash;
        }

        public class CheckResult
        {
            public bool GlobalCheck { get; set; }
            public List<HashValue> DetailedCheck { get; set; }
        }

    }
}