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
        public static int Main(string[] args)
        {
            var options = new Options();
            using (var parser=new CommandLine.Parser(with =>
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
                        Logger.Error("Package path not found ({0})", options.ResolvedPackagePath);
                        return 0;
                    }

                    var checker = new CheckSumChecker(GetListBuilder(options.PackageType), GetCheckSumCalculator(options.Algorithm));
                    switch (options.Mode)
                    {
                        case RunMode.Create:
                            return CreateCheckSum(checker, options);
                        case RunMode.Check:
                            return CheckCheckSum(checker, options);
                        case RunMode.Clear:
                            File.Delete(options.ResolvedDetailedChecksumFileName);
                            File.Delete(options.ResolvedGlobalChecksumFileName);
                            break;
                    }
                }
            return 0;
        }

        private static CheckSumCalculator GetCheckSumCalculator(string algorithm)
        {
            return new CheckSumCalculator(()=>HashAlgorithm.Create(algorithm));
        }

        private static int CheckCheckSum(CheckSumChecker checker, Options options)
        {
            var chres = checker.Check(options);
            if (chres == null)
            {
                Logger.Error(Resource.Error_Missing_checksum_files);
                Logger.Info(options.GetUsage());
                return 0;
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
                    if ((detailedHashValue.HashMatch == CheckSumChecker.HashMatch.Same
                         && options.Verbosity == Verbosity.Verbose)
                        || detailedHashValue.HashMatch != CheckSumChecker.HashMatch.Same)
                    {
                        switch (detailedHashValue.HashMatch)
                        {
                            case CheckSumChecker.HashMatch.Same:
                                Logger.ResultSuccess(string.Format(" {0}: ", detailedHashValue.FileName), "{0}", detailedHashValue.HashMatch);
                                break;
                            case CheckSumChecker.HashMatch.Unexpected:
                                Logger.ResultWarning(string.Format(" {0}: ", detailedHashValue.FileName), "{0}", detailedHashValue.HashMatch);
                                break;
                            case CheckSumChecker.HashMatch.Missing:
                            case CheckSumChecker.HashMatch.Different:
                                Logger.ResultFailure(string.Format(" {0}: ", detailedHashValue.FileName), "{0}", detailedHashValue.HashMatch);
                                break;
                        }
                    }
                }
            }
            if (chres.GlobalCheck)
                return 1;
            return 0;
        }

        private static int CreateCheckSum(CheckSumChecker checker, Options options)
        {
            var crres = checker.Create(options);
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
            return 1;
        }

        private static IFileListBuilder GetListBuilder(PackageType packageType)
        {
            switch (packageType)
            {
                    case PackageType.FullDirectory:return new FullDirectoryListBuilder();
                    case PackageType.ZipAndPatch:return new ZipAndPatchListBuilder();
            }
            return null;
        }

    }
}
