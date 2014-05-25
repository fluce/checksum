using System;
using System.IO;
using System.Text;
using CheckSum.Helpers;

namespace CheckSum.CheckSum
{
    public static class CheckSumCalculatorExtension
    {
        public static string GetHashFromFile(this ICheckSumCalculator checkSumCalculator , string filename, Action<long,long> progress)
        {
            try
            {
                using (var s = new StreamWithProgress(Win32File.OpenRead(filename),progress))
                    return checkSumCalculator.GetHashFromContent(s);
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        public static string GetHashFromString(this ICheckSumCalculator checkSumCalculator, string content)
        {
            using (var s = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                return checkSumCalculator.GetHashFromContent(s);
        }

    }
}