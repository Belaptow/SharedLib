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
        public static void AddToMin<TSource>(this List<List<Tuple<TSource, double>>> tableEntry, Tuple<TSource, double> val)
        {
            tableEntry.SelectMin(page => page.Sum(tuple => tuple.Item2)).Add(val);
        }
    }
    public class LoosePageSplitter<TSource>
    {
        IEnumerable<TSource> source;
        Func<TSource, double> selector;
        ReadOnlyCollection<Tuple<TSource, double>> map;
        double Max;
        double Min;
        ConcurrentDictionary<int, Tuple<double, List<List<Tuple<TSource, double>>>>> deviationTable;
        public LoosePageSplitter(IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            this.source = source;
            this.selector = selector;
            map = new ReadOnlyCollection<Tuple<TSource, double>>(source.Select((entry) =>  new Tuple<TSource, double>(entry, selector(entry))).OrderByDescending(tuple => tuple.Item2).ToList());
            Max = map.Max(tuple => tuple.Item2);
            Min = map.Max(tuple => tuple.Item2);
            deviationTable = new ConcurrentDictionary<int, Tuple<double, List<List<Tuple<TSource, double>>>>>();
            //distinctEntries = map.Values.Select(tuple => tuple.Item2).Distinct().OrderBy(x => x);
        }
        public IEnumerable<IList<TSource>> Paginate()
        {
            if (source.Count() < 2)
                return new List<IList<TSource>>() { source.ToList() };

            Enumerable.Range(2, source.Count())
                .ForEachAsync(AddTableEntry);
            deviationTable.ForEach(LogKeyValuePair);
            var minDeviation = deviationTable.Values.Min(tuple => tuple.Item1);
            Console.WriteLine($"minDeviation {minDeviation}");
            var result = deviationTable.Values.Where(tuple => Equals(tuple.Item1, minDeviation)).SelectMax(tuple => tuple.Item2.Count()).Item2.Select(page => page.Select(tuple => tuple.Item1).ToList());
            Console.WriteLine($"RESULT pages {result.Count()} \n[{String.Join(",\n", result.Select(page => $"[{String.Join(", ", page)}]"))}]");
            return result;
        }
        private void LogKeyValuePair(KeyValuePair<int, Tuple<double, List<List<Tuple<TSource, double>>>>> pair)
        {
            Console.WriteLine($"Number of pages {pair.Key}. Deviation {pair.Value.Item1}.\n[{String.Join("\n", pair.Value.Item2.Select(page => $"SUM({page.Sum(tuple => tuple.Item2)})[{String.Join(", ", page.Select(tuple => tuple.Item2))}]"))}]\n");
        }
        protected void AddTableEntry(int numOfPages)
        {
            deviationTable.TryAdd(numOfPages, GetPageEntry(numOfPages));
        }
        protected Tuple<double, List<List<Tuple<TSource, double>>>> GetPageEntry(int numOfPages) 
        {
            var starter = map.Take(numOfPages).Select(tuple => new List<Tuple<TSource, double>>() { tuple }).ToList();
            map.Skip(numOfPages).ForEach(mapped => starter.AddToMin(mapped));
            return new Tuple<double, List<List<Tuple<TSource, double>>>>(CalculateDeviation(starter), starter);
        }

        protected double CalculateDeviation(List<List<Tuple<TSource, double>>> tableEntry)
        {
            var sums = tableEntry.Select(page => page.Sum(tuple => tuple.Item2));
            return sums.Max() - sums.Min();
        }
    }
}
