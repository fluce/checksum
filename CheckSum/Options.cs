using CommandLine;
using CommandLine.Text;
using System.IO;
using System.Reflection;

namespace CheckSum
{
    public enum RunMode
    {
        Create,
        Check,
        Clear,
    }

    public enum Verbosity
    {
        Silent=0,
        Terse=1,
        Normal=2,
        Verbose=3
    }

    public enum PackageType
    {
        ZipAndPatch,
        FullDirectory
    }

    public class Options
    {
        [Option('m', "Mode", HelpText = "Run mode (Create,Check, Clear)", DefaultValue = RunMode.Check, MutuallyExclusiveSet = "mode")]
        public RunMode Mode { get; set; }

        [Option("Create", HelpText = "Create checksum files. Existing checksum files are replaced.", MutuallyExclusiveSet = "mode")]
        public bool Create { get; set; }

        [Option("Check", HelpText = "Check existing checksum files", MutuallyExclusiveSet = "mode")]
        public bool Check { get; set; }

        [Option("Clear", HelpText = "Remove checksum files", MutuallyExclusiveSet = "mode")]
        public bool Clear { get; set; }


        [Option('p', "PackagePath", HelpText = "Path to package directory", DefaultValue = ".")]
        public string PackagePath
        {
            get { return _packagePath; }
            set
            {
                _packagePath = value;
                _resolvedPackagePath = null;
            }
        }

        [Option("ChecksumInCurrentDirectory", HelpText = "Checksum files are located in current directory", DefaultValue = false)]
        public bool ChecksumInCurrentDirectory { get; set; }

        [Option('d', "DetailedChecksumFileName", HelpText = "Detailed checksum file", DefaultValue = "checksum_detailed.txt")]
        public string DetailedChecksumFileName { get; set; }

        [Option('g', "GlobalChecksumFileName", HelpText = "Global checksum file", DefaultValue = "checksum.txt")]
        public string GlobalChecksumFileName { get; set; }

        [Option('v', "Verbosity", HelpText = "Output verbosity (Silent,Terse,Normal,Verbose)", DefaultValue = Verbosity.Normal)]
        public Verbosity Verbosity { get; set; }

        [Option('t', "PackageType", HelpText = "Package type (ZipAndPatch,FullDirectory)", DefaultValue = PackageType.ZipAndPatch)]
        public PackageType PackageType { get; set; }

        [Option('a', "Algorithm", HelpText = "Algorithm name (MD5, SHA1, SHA256...)", DefaultValue = "MD5")]
        public string Algorithm { get; set; }

        [Option("HideProgress", HelpText = "Do not display progress details")]
        public bool HideProgress { get; set; }

        [Option("DegreeOfParallelism", HelpText = "Use n CPU for hash calculation ", DefaultValue = null)]
        public int? DegreeOfParallelism { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var name = Assembly.GetExecutingAssembly().GetName();
            var help = new HelpText
            {
                Heading = new HeadingInfo(name.Name, name.Version.ToString()),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("Usage: CheckSum --Create -p PackagePath\r\n       CheckSum --Check -p PackagePath");
            help.AddOptions(this);
            return help;
        }

        public string ResolvedDetailedChecksumFileName
        {
            get
            {
                if (!ChecksumInCurrentDirectory)
                    return Path.Combine(ResolvedPackagePath, DetailedChecksumFileName);
                return DetailedChecksumFileName;
            }
        }

        public string ResolvedGlobalChecksumFileName
        {
            get
            {
                if (!ChecksumInCurrentDirectory)
                    return Path.Combine(ResolvedPackagePath, GlobalChecksumFileName);
                return GlobalChecksumFileName;
            }
        }

        private string _resolvedPackagePath;
        private string _packagePath;

        public string ResolvedPackagePath
        {
            get
            {
                if (_resolvedPackagePath == null)
                {
                    _resolvedPackagePath = Path.GetFullPath(PackagePath);
                }
                return _resolvedPackagePath;
            }
        }

    }
}