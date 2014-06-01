using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using CheckSum.Helpers;

namespace CheckSum
{
    public class HashResultComparator : IHashResultComparator
    {
        public CheckResult Compare(HashResult actualResult, HashResult expectedResult)
        {
            var result = new CheckResult();

            result.GlobalCheck = actualResult!=null && expectedResult!=null && actualResult.GlobalHash == expectedResult.GlobalHash;
            //result.DetailedCheck =
            var list1 = expectedResult==null ? 
                            new List<HashValue>() 
                          : expectedResult.DetailedHashValues.Select(
                                x => new HashValue { FileName = x.FileName, Hash = x.Hash, HashMatch = HashMatch.Missing }).ToList();
            var list2 = actualResult==null ?
                            new List<HashValue>()
                          : actualResult.DetailedHashValues.Select(
                                x => new HashValue { FileName = x.FileName, Hash = x.Hash, HashMatch = HashMatch.Unexpected }).ToList();

            result.DetailedCheck = IEnumerableHelper.Match(list1, list2,
                (i1, i2) => String.Compare(i1.FileName, i2.FileName, StringComparison.Ordinal),
                (i1, i2) =>
                    new HashValue
                    {
                        FileName = i1.FileName,
                        Hash = i1.Hash,
                        HashMatch = i1.Hash == i2.Hash ? HashMatch.Same : HashMatch.Different
                    },
                i => i)
                .ToList();

            return result;
            
        }
    }
}