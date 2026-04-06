using System.Collections.Generic;

namespace UniBridge.Editor
{
    public static class CollectionUtility
    {
        public static bool AddUnique<T>(this IList<T> list, T item)
        {
            if (null == list) return false;

            if (!list.Contains(item))
            {
                list.Add(item);
                return true;
            }
            return false;
        }
    }
}
