using System;
using System.Collections.Generic;
using System.Linq;

namespace CheckSum.Helpers
{
    public static class IEnumerableHelper
    {
        public static IEnumerable<T> Checkpoint<T>(this IEnumerable<T> enumerable, Action<IEnumerable<T>> action)
        {
            var list = enumerable.ToList();
            action(list);
            return list;
        }
        public static IEnumerable<U> Checkpoint<T,U>(this IEnumerable<T> enumerable, Func<IEnumerable<T>, IEnumerable<U>> filter)
        {
            return filter(enumerable);
        }

        public static IEnumerable<T> Match<T>(IEnumerable<T> list1, IEnumerable<T> list2, Func<T, T, int> comparatorFunction,
            Func<T, T, T> selectFunctionMatch, Func<T, T> selectFunctionNoMatch) where T:class
        {
            var i1 = list1.GetEnumerator();
            var i2 = list2.GetEnumerator();
            bool c1=i1.MoveNext(),
                c2=i2.MoveNext();
            while (c1||c2)
            {
                int compare;
                if (i1.Current == null)
                    compare = 1;
                else
                    if (i2.Current == null)
                        compare = -1;
                    else
                        compare = comparatorFunction(i1.Current, i2.Current);
                
                if (compare == 0)
                {
                    yield return selectFunctionMatch(i1.Current, i2.Current);
                    c1=i1.MoveNext();
                    c2=i2.MoveNext();
                }
                else if (compare < 0)
                {
                    yield return selectFunctionNoMatch(i1.Current);
                    c1=i1.MoveNext();
                }
                else
                {
                    yield return selectFunctionNoMatch(i2.Current);
                    c2=i2.MoveNext();
                }
            }
        }
    }
}