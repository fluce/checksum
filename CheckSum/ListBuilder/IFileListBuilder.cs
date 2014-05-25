using System.Collections.Generic;

namespace CheckSum.ListBuilder
{
    public interface IFileListBuilder
    {
        IEnumerable<string> BuildFileList(string path);
    }
}