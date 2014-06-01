using System;
using System.Linq;
using System.Text;
using System.Threading;
using CheckSum.CheckSum;
using CheckSum.Helpers;
using CheckSum.ListBuilder;

namespace CheckSum
{
    public class DirectoryHashBuilder : IHashBuilder
    {
        ICheckSumPathProvider PathProvider { get; set; }
        IFileListBuilder FileListBuilder { get; set; }
        ICheckSumCalculator CheckSumCalculator { get; set; }
        int? DegreeOfParallelism { get; set; }

        public DirectoryHashBuilder(
            ICheckSumPathProvider pathProvider, 
            IFileListBuilder fileListBuilder, 
            ICheckSumCalculator checkSumCalculator,
            int? degreeOfParallelism)
        {
            PathProvider = pathProvider;
            FileListBuilder = fileListBuilder;
            CheckSumCalculator = checkSumCalculator;
            DegreeOfParallelism = degreeOfParallelism;
        }

        public HashResult BuildHash()
        {
            ProgressManager.Current["GLOBAL"].SetProgress(1, 3).SetMessage("Building file list");
            var list = FileListBuilder
                .BuildFileList(PathProvider.PackagePath)
                .Where(x => x != PathProvider.DetailedChecksumFileName && x != PathProvider.GlobalChecksumFileName)
                .Select(x =>
                {
                    ProgressManager.Current["FILELIST"].IncrementProgress();
                    return new { FileName = x, Length = Win32File.GetFileSize(x, false) };
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
                .WithDegreeOfParallelism(DegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount))
                .Select(x =>
                {
                    ProgressManager.Current["GLOBAL"].SetProgress(2, 3).SetMessage("Calculating hashes");
                    var r = new HashValue
                    {
                        FileName = x.FileName.RemovePrefix(PathProvider.PackagePath + "\\"),
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

            var detailedHash = list.Select(x => string.Format("{0};{1}", x.FileName, x.Hash))
                .Aggregate(new StringBuilder(), (a, i) => a.Append(i + "\r\n")).ToString();
            var globalHash = CheckSumCalculator.GetHashFromString(detailedHash) + "\r\n";

            ProgressManager.Current["GLOBAL"].SetMessage(null).Complete();

            return new HashResult { DetailedHash = detailedHash, GlobalHash = globalHash, DetailedHashValues = list };
        }
    }
}