using System.Collections.Generic;

namespace Soso.Net.Extensions
{
    public static class ListExtensions
    {
        public static void RemoveCount<T1, T2>(this SortedList<T1, T2> list, int count)
        {
            for (int i = 0; i < count && i < list.Count; i++)
            {
                list.RemoveAt(0);
            }
        }
    }
}