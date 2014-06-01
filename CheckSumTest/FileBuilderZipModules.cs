using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using CheckSum;
using CheckSum.CheckSum;
using CheckSum.Helpers;
using CheckSum.ListBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CheckSumTest
{
    [TestClass]
    public class FileBuilderZipModules
    {

        private string testdir;
        private string testdir2;
        private string currentdir;
        private FD.FDDirectory fullfiletreeTemplate;
        private FD.FDDirectory fullfiletree;
        private Options options;
        private CheckSumChecker checkSumChecker;

        [TestInitialize]
        public void Init()
        {
            ProgressManager.Current.ProgressUpdated += ProgressManager.RenderProgressToConsole;
            options = new Options()
            {
                PackagePath = ".",
                DetailedChecksumFileName = "checksum_detailed.txt",
                GlobalChecksumFileName = "checksum.txt"
            };

            var checkSumFileHashBuilder = new CheckSumFileHashBuilder(options);
            checkSumChecker = new CheckSumChecker(
                new DirectoryHashBuilder(options, new ZipAndPatchListBuilder(), new CheckSumCalculator(MD5.Create), options.DegreeOfParallelism),
                checkSumFileHashBuilder,
                new HashResultComparator(),
                checkSumFileHashBuilder);

            currentdir = Directory.GetCurrentDirectory();
            testdir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            testdir2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            fullfiletreeTemplate = FD.Directory(
                FD.Directory("bob",
                    FD.File("a", "a file content"),
                    FD.File("b", "b file content")
                    ),
                FD.Directory("zip",
                    FD.Directory("mod1",
                        FD.File("a", "a file content"),
                        FD.File("b", "b file content").OffsetDate(TimeSpan.FromHours(1)),
                        FD.File("c", "c file content")),
                    FD.Directory("mod2",
                        FD.File("a", "a file content"),
                        FD.File("b", "b file content").OffsetDate(TimeSpan.FromHours(1)),
                        FD.File("c", "c file content")
                    )
                )
            );
            fullfiletree = fullfiletreeTemplate.Clone().SetName(testdir).CreateFromScratch();

        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.SetCurrentDirectory(currentdir);
            if (Directory.Exists(testdir))
                Directory.Delete(testdir, true);
            if (Directory.Exists(testdir2))
                Directory.Delete(testdir2, true);
        }

        [TestMethod]
        public void TestPackageFileListBuilderZipModules()
        {
            fullfiletree.SetCurrent();

            var p = new ZipAndPatchListBuilder();

            var r=p.BuildFileList(".").ToArray();
            foreach (var file in r)
            {
                Console.Out.WriteLine(file);
            }
            Assert.AreEqual(2, r.Count());
            Assert.AreEqual(".\\zip\\mod1\\b", r[0]);
            Assert.AreEqual(".\\zip\\mod2\\b", r[1]);

        }

        [TestMethod]
        public void TestCreateAndCheckNewModuleInZip()
        {
            var dir = fullfiletree.Clone().SetName(testdir2).CreateFromScratch();

            dir.SetCurrent();

            CalculateTestDataCheckSum();

            dir.Delete();

            dir["zip"].AsDirectory()["mod1"].AsDirectory().Children.Add(FD.File("d", "this is additionnal file d").OffsetDate(TimeSpan.FromHours(2)));

            dir.Create();

            var results = checkSumChecker.Check();

            foreach (var hashValue in results.DetailedCheck)
            {
                Console.Out.WriteLine("{0} ({1}) : {2}", hashValue.FileName, hashValue.Hash, hashValue.HashMatch);
            }

            Assert.IsNotNull(results);
            Assert.IsFalse(results.GlobalCheck);
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName == ".\\zip\\mod1\\b").All(x => x.HashMatch == HashMatch.Missing));
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName == ".\\zip\\mod1\\d").All(x => x.HashMatch == HashMatch.Unexpected));
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName == ".\\zip\\mod2\\b").All(x => x.HashMatch == HashMatch.Same));

        }


        private void CalculateTestDataCheckSum()
        {
            checkSumChecker.Create();

            Console.Out.WriteLine("checksum_detailed :");
            Console.Out.WriteLine(File.ReadAllText(options.DetailedChecksumFileName));
            Console.Out.WriteLine("checksum :");
            var content = File.ReadAllText(options.GlobalChecksumFileName);
            Console.Out.WriteLine(content);
        }


    }
}
