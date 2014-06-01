using System.Collections.Generic;

namespace CheckSum
{
    public class CheckResult
    {
        public bool GlobalCheck { get; set; }
        public List<HashValue> DetailedCheck { get; set; }
    }
}