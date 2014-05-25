using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckSum.ListBuilder
{
    public class ZipAndPatchListBuilder : IFileListBuilder
    {
        public IEnumerable<string> BuildFileList(string path)
        {
            return
                HardenedEnumerateFiles(Path.Combine(path, "patch_dosi"))
                    .Concat(
                        HardenedEnumerateDirectory(Path.Combine(path, "zip"))
                            .Select(
                                d => Directory.EnumerateFiles(d)
                                    .OrderByDescending(f=>File.GetLastWriteTime(f))
                                    .FirstOrDefault()
                            )
                            .Where(f=>!string.IsNullOrEmpty(f))
                    );
        }

        private static IEnumerable<string> HardenedEnumerateFiles(string directory)
        {
            if (!Directory.Exists(directory)) return new string[] {};
            return
                DirectoryHelper.EnumerateFiles(directory);
        }

        private static IEnumerable<string> HardenedEnumerateDirectory(string directory)
        {
            if (!Directory.Exists(directory)) return new string[] { };
            return
                new[] { directory }.Concat(
                    DirectoryHelper.EnumerateDirectories(directory)
                    );
        }
    }
}