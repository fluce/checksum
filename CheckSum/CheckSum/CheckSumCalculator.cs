using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace CheckSum.CheckSum
{
    public class CheckSumCalculator: ICheckSumCalculator
    {
        [ThreadStatic]
        private static HashAlgorithm _hashAlgorithm;

        private Func<HashAlgorithm> _algorithmFactory;

        public CheckSumCalculator(Func<HashAlgorithm> algorithmFactory)
        {
            _algorithmFactory=algorithmFactory;
        }

        public string GetHashFromContent(Stream inputStream)
        {
            if (_hashAlgorithm == null)
                _hashAlgorithm = _algorithmFactory();
            return string.Concat(_hashAlgorithm.ComputeHash(inputStream).Select(b => b.ToString("X2")));
        }

    }
}