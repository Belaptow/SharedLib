using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SharedLib
{
    public static class LoosePageSplitterExtension
    {
        public static void AddToMin<TSource>(this List<List<EntryWrapper<TSource>>> tableEntry, EntryWrapper<TSource> val)
        {
            tableEntry.SelectMin(page => page.Sum(entry => entry.Value)).Add(val);
        }
    }
    public class LoosePageSplitter<TSource>
    {
        int count;
        int maxPages;
        double roungDeviationUpTo;
        IEnumerable<TSource> source;
        Func<TSource, double> selector;
        ReadOnlyCollection<EntryWrapper<TSource>> map;
        double Max;
        double Min;
        ConcurrentDictionary<int, PagesWrapper<TSource>> deviationTable;
        public LoosePageSplitter(IEnumerable<TSource> source, Func<TSource, double> selector, double roungDeviationUpTo, int maxPages)
        {
            this.source = source;
            this.selector = selector;
            this.count = source.Count();
            map = new ReadOnlyCollection<EntryWrapper<TSource>>(source.Select((entry) => new EntryWrapper<TSource>(entry, selector(entry))).OrderByDescending(entry => entry.Value).ToList());
            Max = map.Max(entry => entry.Value);
            Min = map.Max(entry => entry.Value);
            deviationTable = new ConcurrentDictionary<int, PagesWrapper<TSource>>();
            this.roungDeviationUpTo = roungDeviationUpTo;
            this.maxPages = maxPages;
        }
        public IEnumerable<IList<TSource>> Paginate()
        {
            if (count < 2)
                return new List<IList<TSource>>() { source.ToList() };

            Enumerable.Range(2, count > maxPages ? maxPages : count)
                .ForEachAsync(AddTableEntry);

            var bestRatio = deviationTable.Values.Max(page => page.Ratio);
            var result = deviationTable.Values.Where(page => Equals(page.Ratio, bestRatio)).SelectMax(page => page.NumberOfPages);
            return result.Pages.Select(page => page.Select(entry => entry.Entry).ToList());
        }
        protected void AddTableEntry(int numOfPages)
        {
            deviationTable.TryAdd(numOfPages, GetPageEntry(numOfPages));
        }
        protected PagesWrapper<TSource> GetPageEntry(int numOfPages)
        {
            var starter = map.Take(numOfPages).Select(entry => new List<EntryWrapper<TSource>>() { entry }).ToList();
            map.Skip(numOfPages).ForEach(mapped => starter.AddToMin(mapped));
            return new PagesWrapper<TSource>(numOfPages, CalculateDeviation(starter), starter);
        }

        protected double CalculateDeviation(List<List<EntryWrapper<TSource>>> tableEntry)
        {
            var sums = tableEntry.Select(page => page.Sum(tuple => tuple.Value));
            var deviation = sums.Max() - sums.Min();
            return deviation < roungDeviationUpTo ? roungDeviationUpTo : deviation;
        }
    }
    public class EntryWrapper<TSource>
    {
        public TSource Entry { get; }
        public double Value { get; }
        public EntryWrapper(TSource entry, double val)
        {
            Entry = entry;
            Value = val;
        }
    }
    public class PagesWrapper<TSource>
    {
        public int NumberOfPages { get; }
        public double Deviation { get; }
        public double Ratio { get; }
        public List<List<EntryWrapper<TSource>>> Pages { get; }
        public PagesWrapper(int numberOfPages, double deviation, List<List<EntryWrapper<TSource>>> pages)
        {
            NumberOfPages = numberOfPages;
            Deviation = deviation;
            Pages = pages;
            Ratio = numberOfPages / deviation;
        }
    }
}
