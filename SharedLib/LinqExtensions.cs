using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLib
{
    public class HierarchyNode<T> where T : class
    {
        public T Entity { get; set; }
        public IEnumerable<HierarchyNode<T>> ChildNodes { get; set; }
        public int Depth { get; set; }
    }
    public static class LinqExtensions
    {
        /// <summary>
        /// Returns true if number of elements in enumerable matches provided value
        /// </summary>
        /// <typeparam name="T">Type of elements in enumerable</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="count">Expected number of elements</param>
        /// <returns>Comparison result</returns>
        public static bool CountEquals<T>(this IEnumerable<T> source, int count)
        {
            return source.Count() == count;
        }
        /// <summary>
        /// Add element to collection if none of existing satisfy equality comparison
        /// </summary>
        /// <typeparam name="T">Type of elements in collection</typeparam>
        /// <param name="source">Collection</param>
        /// <param name="value">Value to add</param>
        public static void AddIfNotExists<T>(this ICollection<T> source, T value)
        {
            if (source.All((T p) => !Equals(value, p)))
                source.Add(value);
        }
        /// <summary>
        /// Execute action for each element in enumerable
        /// </summary>
        /// <typeparam name="T">Type of elements in enumerable</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="action">Action to execute</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T entry in source) action(entry);
        }
        /// <summary>
        /// Get enumerable without null entries
        /// </summary>
        /// <typeparam name="T">Type of elements in enumerable</typeparam>
        /// <param name="source">Enumerable</param>
        /// <returns>Enumerable without null entries</returns>
        public static IEnumerable<T> SkipNull<T>(this IEnumerable<T> source) where T : class
        {
            return source.Where((T entry) => entry != null);
        }
        /// <summary>
        /// Add range of entries to collection
        /// </summary>
        /// <typeparam name="T">Type of elements in collection</typeparam>
        /// <param name="source">Collection</param>
        /// <param name="range">Enumerable of values to add</param>
        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> range)
        {
            range.ForEach(entry => source.Add(entry));
        }
        /// <summary>
        /// Split enumerable into sublists of specified length
        /// </summary>
        /// <typeparam name="T">Type of elements in enumerable</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="pageSize">Chunk size</param>
        /// <returns>Enumerable of sublists</returns>
        /// <remarks>If last partition is smaller than page size - returns partition as-is</remarks>
        public static IEnumerable<IList<T>> SplitPages<T>(this IEnumerable<T> source, int pageSize)
        {
            while (source.Any())
            {
                yield return source.Take(pageSize).ToList();
                source = source.Skip(pageSize);
            }
        }
        /// <summary>
        /// Split enumerable into sublists loosely satisfying provided condition
        /// </summary>
        /// <typeparam name="TSource">Type of elements in enumerable</typeparam>
        /// <typeparam name="TAccumulate">Type of accumulator result</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="seed">Starting point of accumulator</param>
        /// <param name="func">Accumulator method</param>
        /// <param name="predicate">Condition for sublists to match</param>
        /// <returns>Enumerable of sublists satisfying specified condition</returns>
        /// <remarks>No data loss, if single element doesn't satisfy condition - returns it as sublist of singular entry.
        /// Method made for loosely aggregating based on predicate, not all sublists in resulting enumerable may match specified condition
        /// For strict condition checking use SplitPagesAggregateStrict</remarks>
        public static IEnumerable<IList<TSource>> SplitPagesAggregate<TSource, TAccumulate>(
            this IEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, bool> predicate)
        {
            while (source.Any())
            {
                var page = source.TakeWhileAggregate(seed, func, predicate).ToList();
                yield return page.Any() ? page : source.Take(1).ToList();
                source = source.Skip(page.Any() ? page.Count() : 1);
            }
        }
        /// <summary>
        /// Split enumerable into sublists satisfying provided condition. Outliers not satisfying condition by themselves get returned in out param
        /// </summary>
        /// <typeparam name="TSource">Type of elements in enumerable</typeparam>
        /// <typeparam name="TAccumulate">Type of accumulator result</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="seed">Starting point of accumulator</param>
        /// <param name="func">Accumulator method</param>
        /// <param name="predicate">Condition for sublists to match</param>
        /// <param name="outliers">Elements not matching the condition</param>
        /// <returns>Enumerable of sublists satisfying specified condition</returns>
        public static IEnumerable<IList<TSource>> SplitPagesAggregateStrict<TSource, TAccumulate>(
            this IEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, bool> predicate,
            out List<TSource> outliers)
        {
            var result = new List<List<TSource>>();
            outliers = new List<TSource>();
            while (source.Any())
            {
                var page = source.TakeWhileAggregate(seed, func, predicate).ToList();
                var skip = page.Any() ? page.Count() : 1;
                if (page.Any())
                    result.Add(source.Take(skip).ToList());
                else
                    outliers.Add(source.First());

                source = source.Skip(skip);
            }
            return result;
        }
        public static IEnumerable<IList<TSource>> SplitPagesLooselyEqual<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, double> selector)
        {
            return (new LoosePageSplitter<TSource>(source, selector)).Paginate();
        }
        /// <summary>
        /// Take from enumerable while aggregation satisfies condition
        /// </summary>
        /// <typeparam name="TSource">Type of elements in enumerable</typeparam>
        /// <typeparam name="TAccumulate">Type of accumulator result</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="seed">Starting point of accumulator</param>
        /// <param name="func">Accumulator method</param>
        /// <param name="predicate">Condition for aggregation to match</param>
        /// <returns>Enumerable aggregation of which satisfies provided condition</returns>
        public static IEnumerable<TSource> TakeWhileAggregate<TSource, TAccumulate>(
            this IEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, bool> predicate)
        {
            TAccumulate accumulator = seed;
            foreach (TSource item in source)
            {
                accumulator = func(accumulator, item);
                if (predicate(accumulator))
                    yield return item;
                else
                    yield break;
            }
        }
        /// <summary>
        /// Select first selector result matching condition
        /// </summary>
        /// <typeparam name="TSource">Type of elements in enumerable</typeparam>
        /// <typeparam name="TResult">Type of selector result</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="selector">Selector method</param>
        /// <param name="predicate">Condition to match</param>
        /// <returns>First or default selector result</returns>
        public static TResult SelectFirstOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, Func<TResult, bool> predicate)
        {
            foreach (TSource entry in source)
            {
                var result = selector(entry);
                if (predicate(result)) return result;
            }
            return default(TResult);
        }
        private static IEnumerable<HierarchyNode<TEntity>> CreateHierarchy<TEntity, TProperty>(
            IEnumerable<TEntity> allItems
            , TEntity parentItem
            , Func<TEntity, TProperty> idProperty
            , Func<TEntity, TProperty> parentIdProperty
            , int depth) where TEntity : class
        {
            IEnumerable<TEntity> childs;

            if (parentItem == null)
                childs = allItems.Where(i => parentIdProperty(i).Equals(default(TProperty)));
            else
                childs = allItems.Where(i => parentIdProperty(i).Equals(idProperty(parentItem)));

            if (childs.Count() > 0)
            {
                depth++;

                foreach (var item in childs)
                    yield return new HierarchyNode<TEntity>()
                    {
                        Entity = item,
                        ChildNodes = CreateHierarchy<TEntity, TProperty>
                      (allItems, item, idProperty, parentIdProperty, depth),
                        Depth = depth
                    };
            }
        }
        public static double Median<TColl>(
            this IEnumerable<TColl> source,
            Func<TColl, double> selector)
        {
            return source.Select<TColl, double>(selector).Median();
        }

        public static double Median(this IEnumerable<double> source)
        {
            int count = source.Count();
            if (count == 0)
                return 0;

            source = source.OrderBy(n => n);

            int midpoint = count / 2;
            if (count % 2 == 0)
                return (source.ElementAt(midpoint - 1) + source.ElementAt(midpoint)) / 2.0;
            else
                return source.ElementAt(midpoint);
        }
        public static IEnumerable<HierarchyNode<TEntity>> AsHierarchy<TEntity, TProperty>(
            this IEnumerable<TEntity> allItems
            , Func<TEntity, TProperty> idProperty
            , Func<TEntity, TProperty> parentIdProperty) where TEntity : class
        {
            return CreateHierarchy(allItems, default(TEntity), idProperty, parentIdProperty, 0);
        }
        /// <summary>
        /// Returns the minimal element of the given sequence, based on
        /// the given projection.
        /// </summary>
        /// <remarks>
        /// If more than one element has the minimal projected value, the first
        /// one encountered will be returned. This overload uses the default comparer
        /// for the projected type. This operator uses immediate execution, but
        /// only buffers a single result (the current minimal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <returns>The minimal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty</exception>

        public static TSource SelectMin<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector)
        {
            return source.SelectMin(selector, null);
        }

        /// <summary>
        /// Returns the minimal element of the given sequence, based on
        /// the given projection and the specified comparer for projected values.
        /// </summary>
        /// <remarks>
        /// If more than one element has the minimal projected value, the first
        /// one encountered will be returned. This operator uses immediate execution, but
        /// only buffers a single result (the current minimal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <param name="comparer">Comparer to use to compare projected values</param>
        /// <returns>The minimal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="selector"/> 
        /// or <paramref name="comparer"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty</exception>

        public static TSource SelectMin<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            comparer = comparer ?? Comparer<TKey>.Default;

            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                var min = sourceIterator.Current;
                var minKey = selector(min);
                while (sourceIterator.MoveNext())
                {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) < 0)
                    {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }
                return min;
            }
        }
        /// <summary>
        /// Returns the maximal element of the given sequence, based on
        /// the given projection.
        /// </summary>
        /// <remarks>
        /// If more than one element has the maximal projected value, the first
        /// one encountered will be returned. This overload uses the default comparer
        /// for the projected type. This operator uses immediate execution, but
        /// only buffers a single result (the current maximal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <returns>The maximal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty</exception>

        public static TSource SelectMax<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector)
        {
            return source.SelectMax(selector, null);
        }

        /// <summary>
        /// Returns the maximal element of the given sequence, based on
        /// the given projection and the specified comparer for projected values. 
        /// </summary>
        /// <remarks>
        /// If more than one element has the maximal projected value, the first
        /// one encountered will be returned. This operator uses immediate execution, but
        /// only buffers a single result (the current maximal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <param name="comparer">Comparer to use to compare projected values</param>
        /// <returns>The maximal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="selector"/> 
        /// or <paramref name="comparer"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty</exception>

        public static TSource SelectMax<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            comparer = comparer ?? Comparer<TKey>.Default;

            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                var max = sourceIterator.Current;
                var maxKey = selector(max);
                while (sourceIterator.MoveNext())
                {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, maxKey) > 0)
                    {
                        max = candidate;
                        maxKey = candidateProjected;
                    }
                }
                return max;
            }
        }
    }
}
