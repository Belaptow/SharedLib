using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib
{
    public static class TaskUtils
    {
        public static void ForEachAsync<T>(this IEnumerable<T> source, Action<T> action)
        {
            source.ForEachAsync(action, Environment.ProcessorCount);
        }
        public static void ForEachAsync<T>(this IEnumerable<T> source, Action<T> action, int degreeOfParallelism)
        {
            source.ForEachAsync(action, degreeOfParallelism, false);
        }
        public static void ForEachAsync<T>(this IEnumerable<T> source, Action<T> action, int degreeOfParallelism, bool supressFlow)
        {
            var entries = source.Count();
            if (entries == 0) 
                return;

            Func<Func<Task>, Task> taskCreator;
            Func<T, Task> wrapper = (entry) => { action(entry); return Task.CompletedTask; };

            if (supressFlow)
                taskCreator = RunSupressFlow;
            else
                taskCreator = Task.Run;

            degreeOfParallelism = Math.Min(degreeOfParallelism, entries);
            Task.WaitAll(Task.WhenAll(Partitioner.Create(source).GetPartitions(degreeOfParallelism).Select(partition => taskCreator(async () => { using (partition) while (partition.MoveNext()) await wrapper(partition.Current).ConfigureAwait(continueOnCapturedContext: false); }))));
        }
        public static Task RunSupressFlow(Func<Task> func)
        {
            using (SupressFlow())
                return Task.Run(TransferCurrentContext(func));
        }
        public static IDisposable SupressFlow()
        {
            if (!ExecutionContext.IsFlowSuppressed())
                return ExecutionContext.SuppressFlow();
            return null;
        }
        private static Func<Task> TransferCurrentContext(Func<Task> func)
        {
            return async () => { await func(); };
        }
    }
}
