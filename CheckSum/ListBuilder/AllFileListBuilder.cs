using System.Collections.Generic;

namespace CheckSum.ListBuilder
{
    public class FullDirectoryListBuilder : IFileListBuilder
    {
        public IEnumerable<string> BuildFileList(string path)
        {
            return DirectoryHelper.EnumerateFiles(path);
        }
    }
}