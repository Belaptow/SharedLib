namespace SharedLib
{
    public static class LinqExtensions
    {
        public static void AddIfNotExists<T>(this ICollection<T> source, T value)
        {
            if (source.All((T p) => !Equals(value, p)))
                source.Add(value);
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach(T entry in source) action(entry);
        }
        public static IEnumerable<T> SkipNull<T>(this IEnumerable<T> source) where T : class
        {
            return source.Where((T entry)  => entry != null);
        }
        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> range)
        {
            range.ForEach(entry => source.Add(entry));
        }
        public static IEnumerable<IList<T>> SplitPages<T> (this IEnumerable<T> source, int pageSize)
        {
            while (source.Any())
            {
                yield return source.Take(pageSize).ToList();
                source = source.Skip(pageSize);
            }
        }
        public static TResult SelectFirstOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, Func<TResult, bool> predicate)
        {
            foreach(TSource entry in source)
            {
                var result = selector(entry);
                if (predicate(result)) return result;
            }
            return default(TResult);
        }
    }
}
