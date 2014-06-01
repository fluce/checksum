using System;
using CheckSum.CheckSum;
using CheckSum.Helpers;
using CheckSum.ListBuilder;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using CheckSum.Res;

namespace CheckSum
{
    public class Program
    {
        private struct Return
        {
            public const int Success = 0;
            public const int Error = 1;
            public const int CheckFailed = 2;
        }


        public static int Main(string[] args)
        {
            try
            {

                var options = new Options();
                using (var parser = new CommandLine.Parser(with =>
                {
                    with.CaseSensitive = false;
                    with.MutuallyExclusive = true;
                    with.HelpWriter = Logger.ErrorTextWriter;
                    with.ParsingCulture = CultureInfo.InvariantCulture;
                }))
                    if (parser.ParseArguments(args, options))
                    {
                        if (options.Create) options.Mode = RunMode.Create;
                        if (options.Check) options.Mode = RunMode.Check;
                        if (options.Clear) options.Mode = RunMode.Clear;

                        if (!options.HideProgress)
                            ProgressManager.Current.ProgressUpdated += ProgressManager.RenderProgressToConsole;
                        if (!Directory.Exists(options.PackagePath))
                        {
                            Logger.Error("Package path not found ({0})", options.PackagePath);
                            return Return.Error;
                        }

                        var csfhb = new CheckSumFileHashBuilder(options);
                        var checker = new CheckSumChecker(
                            new DirectoryHashBuilder(options, GetListBuilder(options.PackageType),
                                GetCheckSumCalculator(options.Algorithm),
                                options.DegreeOfParallelism),
                            csfhb,
                            new HashResultComparator(),
                            csfhb);

                        switch (options.Mode)
                        {
                            case RunMode.Create:
                                return CreateCheckSum(checker, options);
                            case RunMode.Check:
                                return CheckCheckSum(checker, options);
                            case RunMode.Clear:
                                csfhb.Clear();
                                return Return.Success;
                        }
                    }
                return Return.Error;
            }
            catch (Exception e)
            {
                Logger.Error("Exception {0} : {1}", e.GetType().Name, e.Message);
                return Return.Error;
            }
        }

        private static CheckSumCalculator GetCheckSumCalculator(string algorithm)
        {
            return new CheckSumCalculator(() => HashAlgorithm.Create(algorithm));
        }

        private static int CheckCheckSum(CheckSumChecker checker, Options options)
        {
            var chres = checker.Check();
            if (chres == null)
            {
                Logger.Error(Resource.Error_Missing_checksum_files);
                Logger.Info(options.GetUsage());
                return Return.Error;
            }
            if (options.Verbosity > Verbosity.Silent)
            {
                if (chres.GlobalCheck)
                {
                    Logger.ResultSuccess(string.Format(Resource.Checksum_files_verified,
                        chres.DetailedCheck.Count), Resource.Checksum_files_verified_SUCCESS);
                }
                else
                {
                    Logger.ResultFailure(string.Format(Resource.Checksum_files_verified,
                        chres.DetailedCheck.Count), Resource.Checksum_files_verified_FAILED);
                }
            }
            if (options.Verbosity >= Verbosity.Normal)
            {
                foreach (var detailedHashValue in chres.DetailedCheck)
                {
                    if ((detailedHashValue.HashMatch == HashMatch.Same
                         && options.Verbosity == Verbosity.Verbose)
                        || detailedHashValue.HashMatch != HashMatch.Same)
                    {
                        switch (detailedHashValue.HashMatch)
                        {
                            case HashMatch.Same:
                                Logger.ResultSuccess(string.Format(" {0}: ", detailedHashValue.FileName), "{0}",
                                    detailedHashValue.HashMatch);
                                break;
                            case HashMatch.Unexpected:
                                Logger.ResultWarning(string.Format(" {0}: ", detailedHashValue.FileName), "{0}",
                                    detailedHashValue.HashMatch);
                                break;
                            case HashMatch.Missing:
                            case HashMatch.Different:
                                Logger.ResultFailure(string.Format(" {0}: ", detailedHashValue.FileName), "{0}",
                                    detailedHashValue.HashMatch);
                                break;
                        }
                    }
                }
            }
            if (chres.GlobalCheck)
                return Return.Success;
            return Return.CheckFailed;
        }

        private static int CreateCheckSum(CheckSumChecker checker, Options options)
        {
            var crres = checker.Create();
            if (options.Verbosity > Verbosity.Silent)
                Logger.Info(Resource.Checksum_files_created,
                    crres.DetailedHashValues.Count);
            if (options.Verbosity >= Verbosity.Verbose)
            {
                Logger.Output(Resource.CreateCheckSum_Global_hash, crres.GlobalHash.Trim('\r', '\n'));
                foreach (var detailedHashValue in crres.DetailedHashValues)
                {
                    Logger.Output(" {0}: {1}", detailedHashValue.FileName,
                        detailedHashValue.Hash);
                }
            }
            return Return.Success;
        }

        private static IFileListBuilder GetListBuilder(PackageType packageType)
        {
            switch (packageType)
            {
                case PackageType.FullDirectory:
                    return new FullDirectoryListBuilder();
                case PackageType.ZipAndPatch:
                    return new ZipAndPatchListBuilder();
            }
            return null;
        }

    }
}
