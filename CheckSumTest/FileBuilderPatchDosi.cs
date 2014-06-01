using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CheckSum;
using CheckSum.Helpers;
using CheckSum.ListBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CheckSum.CheckSum;

namespace CheckSumTest
{
    [TestClass]
    public class FileBuilderPatchDosi
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

            checkSumChecker = new CheckSumChecker(
                new DirectoryHashBuilder(options, new ZipAndPatchListBuilder(), new CheckSumCalculator(MD5.Create)),
                new CheckSumFileHashBuilder(options));

            currentdir = Directory.GetCurrentDirectory();
            testdir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            testdir2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            fullfiletreeTemplate = FD.Directory(
                FD.Directory("bob",
                    FD.File("a", "a file content"),
                    FD.File("b", "b file content")
                    ),
                FD.Directory("patch_dosi",
                    FD.File("a", "a file content"),
                    FD.File("b", "b file content"),
                    FD.Directory("subdir1",
                        FD.File("a", "a file content"),
                        FD.File("b", "b file content")
                    ),
                    FD.Directory("subdir2",
                        FD.File("a", "a file content"),
                        FD.File("b", "b file content"),
                        FD.Directory("subsubdir1",
                            FD.File("a", "a file content"),
                            FD.File("b", "b file content")
                        )
                    ),
                    FD.Directory("subdir3"),
                    FD.Directory("subdir4",
                        FD.Directory("subsubdir1",
                            FD.File("a", "a file content"),
                            FD.File("b", "b file content")
                        )
                    )
                )
            );
            fullfiletree=fullfiletreeTemplate.Clone().SetName(testdir).CreateFromScratch();

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
        public void TestPackageFileListBuilderPatchDosi()
        {
            fullfiletree.SetCurrent();

            var p = new ZipAndPatchListBuilder();

            var r=p.BuildFileList(".").ToArray();
            foreach (var file in r)
            {
                Console.Out.WriteLine(file);
            }
            Assert.AreEqual(10, r.Count());
            foreach (var file in r)
            {
                Assert.IsTrue(file.StartsWith(".\\patch_dosi"));
            }

        }

        [TestMethod]
        public void TestFullDirectoryListBuilder()
        {
            fullfiletree.SetCurrent();

            var p = new FullDirectoryListBuilder();

            var r = p.BuildFileList(".").ToArray();
            foreach (var file in r)
            {
                Console.Out.WriteLine(file);
            }
            Assert.AreEqual(12, r.Count());
            foreach (var file in r)
            {
                Assert.IsTrue(file.StartsWith(".\\patch_dosi") || file.StartsWith(".\\bob"));
            }

        }

        [TestMethod]
        public void TestCreate()
        {
            fullfiletree.SetCurrent();

            checkSumChecker.Create(options);

            Console.Out.WriteLine("checksum_detailed :");
            Console.Out.WriteLine(File.ReadAllText(options.DetailedChecksumFileName));
            Console.Out.WriteLine("checksum :");
            var content = File.ReadAllText(options.GlobalChecksumFileName);
            Console.Out.WriteLine(content);

            Assert.AreEqual("CCFF246B1AC580D0B162F6A334C86664\r\n", content);


        }

        [TestMethod]
        public void TestCreateAndCheck()
        {

            fullfiletree.SetCurrent();

            CalculateTestDataCheckSum();

            var results = checkSumChecker.Check(options);

            Assert.IsNotNull(results);
            Assert.IsTrue(results.GlobalCheck);
            Assert.IsTrue(results.DetailedCheck.All(x=>x.HashMatch==HashMatch.Same));

        }

        [TestMethod]
        public void TestCreateAndCheckMissingFile()
        {
            var dir=fullfiletree.Clone().SetName(testdir2).CreateFromScratch();

            dir.SetCurrent();

            CalculateTestDataCheckSum();

            dir.Delete();

            dir["patch_dosi"].AsDirectory().Children.RemoveAll(x => x.Name == "a");

            dir.Create();
            
            var results = checkSumChecker.Check(options);

            foreach (var hashValue in results.DetailedCheck)
            {
                Console.Out.WriteLine("{0} ({1}) : {2}",hashValue.FileName,hashValue.Hash,hashValue.HashMatch);
            }

            Assert.IsNotNull(results);
            Assert.IsFalse(results.GlobalCheck);
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName != "patch_dosi\\a").All(x => x.HashMatch == HashMatch.Same));
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName == "patch_dosi\\a").All(x => x.HashMatch == HashMatch.Missing));

        }

        private void CalculateTestDataCheckSum()
        {
            checkSumChecker.Create(options);

            Console.Out.WriteLine("checksum_detailed :");
            Console.Out.WriteLine(File.ReadAllText(options.DetailedChecksumFileName));
            Console.Out.WriteLine("checksum :");
            var content = File.ReadAllText(options.GlobalChecksumFileName);
            Console.Out.WriteLine(content);

            Assert.AreEqual("CCFF246B1AC580D0B162F6A334C86664\r\n", content);
        }

        [TestMethod]
        public void TestCreateAndCheckAdditionnalFile()
        {
            var dir = fullfiletree.Clone().SetName(testdir2).CreateFromScratch();

            dir.SetCurrent();

            CalculateTestDataCheckSum();

            dir.Delete();

            dir["patch_dosi"].AsDirectory().Children.Add(FD.File("c","this is additionnal file c"));

            dir.Create();

            var results = checkSumChecker.Check(options);

            foreach (var hashValue in results.DetailedCheck)
            {
                Console.Out.WriteLine("{0} ({1}) : {2}", hashValue.FileName, hashValue.Hash, hashValue.HashMatch);
            }

            Assert.IsNotNull(results);
            Assert.IsFalse(results.GlobalCheck);
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName != "patch_dosi\\c").All(x => x.HashMatch == HashMatch.Same));
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName == "patch_dosi\\c").All(x => x.HashMatch == HashMatch.Unexpected));

        }

        [TestMethod]
        public void TestCreateAndCheckModifiedFile()
        {
            var dir = fullfiletree.Clone().SetName(testdir2).CreateFromScratch();

            dir.SetCurrent();

            CalculateTestDataCheckSum();

            dir.Delete();

            dir["patch_dosi"].AsDirectory()["subdir1"].AsDirectory()["a"].AsFile().SetContent("this is a new content for file a");

            dir.Create();

            var results = checkSumChecker.Check(options);

            foreach (var hashValue in results.DetailedCheck)
            {
                Console.Out.WriteLine("{0} ({1}) : {2}", hashValue.FileName, hashValue.Hash, hashValue.HashMatch);
            }

            Assert.IsNotNull(results);
            Assert.IsFalse(results.GlobalCheck);
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName != "patch_dosi\\subdir1\\a").All(x => x.HashMatch == HashMatch.Same));
            Assert.IsTrue(results.DetailedCheck.Where(x => x.FileName == "patch_dosi\\subdir1\\a").All(x => x.HashMatch == HashMatch.Different));

        }

        [TestMethod]
        public void TestCreateProgram()
        {
            fullfiletree.SetCurrent();

            var ret=Program.Main(new[] {"--Create", "-v", "Verbose"});

            Assert.AreEqual(0, ret);

            Console.Out.WriteLine("checksum_detailed :");
            Console.Out.WriteLine(File.ReadAllText(options.DetailedChecksumFileName));
            Console.Out.WriteLine("checksum :");
            var content = File.ReadAllText(options.GlobalChecksumFileName);
            Console.Out.WriteLine(content);

            Assert.AreEqual("CCFF246B1AC580D0B162F6A334C86664\r\n", content);


        }

        [TestMethod]
        public void TestProgramHelp()
        {
            fullfiletree.SetCurrent();

            var ret = Program.Main(new[] {"--Help"});

            Assert.AreEqual(1, ret);

        }

        [TestMethod]
        public void TestCreateAndCheckWithProgram()
        {

            fullfiletree.SetCurrent();

            CalculateTestDataCheckSum();

            var ret=Program.Main(new[] { "--Check" });

            Assert.AreEqual(0, ret);

        }

        [TestMethod]
        public void TestCreateAndCheckModifiedWithProgram()
        {
            var dir = fullfiletree.Clone().SetName(testdir2).CreateFromScratch();

            dir.SetCurrent();

            CalculateTestDataCheckSum();

            dir.Delete();

            dir["patch_dosi"].AsDirectory()["subdir1"].AsDirectory()["a"].AsFile().SetContent("this is a new content for file a");

            dir.Create();

            var ret = Program.Main(new[] { "--Check","-v","Verbose" });

            Assert.AreEqual(2, ret);

        }

    }
}
