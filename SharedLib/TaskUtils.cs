using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLib
{
    public static class TaskUtils
    {
        /// <summary>
        /// Execute action for each element in enumerable asynchronously
        /// </summary>
        /// <typeparam name="T">Type of elements in enumerable</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="action">Action to execute</param>
        /// <remarks>Default degree of parallelism - number of logical processors available to current process</remarks>
        public static void ForEachAsync<T>(this IEnumerable<T> source, Action<T> action)
        {
            source.ForEachAsync(action, Environment.ProcessorCount);
        }
        /// <summary>
        /// Execute action for each element in enumerable asynchronously with specified degree of parallelisation
        /// </summary>
        /// <typeparam name="T">Type of elements in enumerable</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="action">Action to execute</param>
        /// <param name="degreeOfParallelism">Degree of parallelism</param>
        public static void ForEachAsync<T>(this IEnumerable<T> source, Action<T> action, int degreeOfParallelism)
        {
            source.ForEachAsync(action, degreeOfParallelism, false);
        }
        /// <summary>
        /// Execute action for each element in enumerable asynchronously with specified degree of parallelisation
        /// </summary>
        /// <typeparam name="T">Type of elements in enumerable</typeparam>
        /// <param name="source">Enumerable</param>
        /// <param name="action">Action to execute</param>
        /// <param name="degreeOfParallelism">Degree of parallelism</param>
        /// <param name="supressFlow">Suppress execution context flow</param>
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
            Task.WhenAll(Partitioner.Create(source).GetPartitions(degreeOfParallelism).Select(partition => taskCreator(async () => { using (partition) while (partition.MoveNext()) await wrapper(partition.Current).ConfigureAwait(continueOnCapturedContext: false); }))).Wait();
        }
        /// <summary>
        /// Run task in suppressed flow context
        /// </summary>
        /// <param name="func">Task factory</param>
        /// <returns>Task</returns>
        public static Task RunSupressFlow(Func<Task> func)
        {
            using (SupressFlow())
                return Task.Run(TransferCurrentContext(func));
        }
        /// <summary>
        /// Suppress flow of execution context across async threads
        /// </summary>
        /// <returns>AsyncFlowControl structure</returns>
        public static IDisposable SupressFlow()
        {
            if (!ExecutionContext.IsFlowSuppressed())
                return ExecutionContext.SuppressFlow();
            return null;
        }
        /// <summary>
        /// Transfer context of current thread
        /// </summary>
        /// <param name="func">Method to execute</param>
        /// <returns>Wrapped method</returns>
        /// <remarks>In current context does nothing. Just a plug for future reference</remarks>
        private static Func<Task> TransferCurrentContext(Func<Task> func)
        {
            return async () => { await func(); };
        }
    }
}
