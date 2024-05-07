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
        public static void AddIfNotExists<T>(this ICollection<T> source, T value)
        {
            if (source.All((T p) => !Equals(value, p)))
                source.Add(value);
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T entry in source) action(entry);
        }
        public static IEnumerable<T> SkipNull<T>(this IEnumerable<T> source) where T : class
        {
            return source.Where((T entry) => entry != null);
        }
        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> range)
        {
            range.ForEach(entry => source.Add(entry));
        }
        public static IEnumerable<IList<T>> SplitPages<T>(this IEnumerable<T> source, int pageSize)
        {
            while (source.Any())
            {
                yield return source.Take(pageSize).ToList();
                source = source.Skip(pageSize);
            }
        }
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
                {
                    yield return item;
                }
                else
                {
                    yield break;
                }
            }
        }
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

        /// <summary>
        /// LINQ IEnumerable AsHierachy() extension method
        /// </summary>
        /// <typeparam name="TEntity">Entity class</typeparam>
        /// <typeparam name="TProperty">Property of entity class</typeparam>
        /// <param name="allItems">Flat collection of entities</param>
        /// <param name="idProperty">Reference to Id/Key of entity</param>
        /// <param name="parentIdProperty">Reference to parent Id/Key</param>
        /// <returns>Hierarchical structure of entities</returns>
        public static IEnumerable<HierarchyNode<TEntity>> AsHierarchy<TEntity, TProperty>(
            this IEnumerable<TEntity> allItems
            , Func<TEntity, TProperty> idProperty
            , Func<TEntity, TProperty> parentIdProperty) where TEntity : class
        {
            return CreateHierarchy(allItems, default(TEntity), idProperty, parentIdProperty, 0);
        }

    }
}
