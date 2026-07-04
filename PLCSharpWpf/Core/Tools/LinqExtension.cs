namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// LinqExtension
    /// </summary>
    public static class LinqExtension
    {
        /// <summary>
        /// DistinctBy
        /// </summary>
        /// <param name="source">按钮列表</param>
        /// <param name="keySelector">keySelector</param>
        /// <returns>返回结果</returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}