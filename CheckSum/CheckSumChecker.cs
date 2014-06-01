using System.IO;

namespace CheckSum
{
    public class CheckSumChecker
    {
        IHashBuilder ActualHashBuilder { get; set; }
        IHashBuilder ExpectedHashBuilder { get; set; }

        IHashResultComparator Comparator { get; set; }

        ICheckSumFileAccessor Accessor { get; set; }

        public CheckSumChecker(IHashBuilder actualHashBuilder, IHashBuilder expectedHashBuilder, IHashResultComparator comparator, ICheckSumFileAccessor checkSumFileAccessor)
        {
            ActualHashBuilder = actualHashBuilder;
            ExpectedHashBuilder = expectedHashBuilder;
            Comparator = comparator;
            Accessor = checkSumFileAccessor;
        }

        public HashResult Create()
        {
            var result=ActualHashBuilder.BuildHash();

            Accessor.Write(result);

            return result;
        }

        public CheckResult Check()
        {
            var actualResult=ActualHashBuilder.BuildHash();
            var expectedResult = ExpectedHashBuilder.BuildHash();

            return Comparator.Compare(actualResult,expectedResult);

        }

    }
}