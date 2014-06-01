namespace CheckSum
{
    public interface IHashResultComparator
    {
        CheckResult Compare(HashResult actualResult, HashResult expectedResult);
    }
}