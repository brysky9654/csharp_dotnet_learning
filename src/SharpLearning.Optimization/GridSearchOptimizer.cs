using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharpLearning.Optimization
{
    /// <summary>
    /// Simple grid search that tries all combinations of the provided parameters
    /// </summary>
    public sealed class GridSearchOptimizer : IOptimizer
    {
        readonly bool m_runParallel;
        readonly IParameterSpec[] m_parameters;
        readonly int m_maxDegreeOfParallelism = -1;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters">A list of parameter specs, one for each optimization parameter</param>
        /// <param name="runParallel">Use multi threading to speed up execution (default is true)</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations (default is -1 (unlimited))</param>
        public GridSearchOptimizer(IParameterSpec[] parameters, 
            bool runParallel = false, 
            int maxDegreeOfParallelism = -1)
        {
            m_parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            m_runParallel = runParallel;
            m_maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        /// <summary>
        /// Simple grid search that tries all combinations of the provided parameters.
        /// Returns the result which best minimizes the provided function.
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <returns></returns>
        public OptimizerResult OptimizeBest(Func<double[], OptimizerResult> functionToMinimize)
        {
            Optimize(functionToMinimize, 0.0f, out var best);

            return best;
        }

        /// <summary>
        /// Simple grid search that tries all combinations of the provided parameters.
        /// Returns all results, chronologically ordered. 
        /// Note that the order of results might be affected if running parallel.
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <returns></returns>
        public IReadOnlyCollection<OptimizerResult> Optimize(Func<double[], OptimizerResult> functionToMinimize)
        {
            return Optimize(functionToMinimize, 0.0, out _);
        }

        /// <summary>
        /// Simple grid search that tries all combinations of the provided parameters.
        /// Returns the first result which minimizes the provided function past the treshold.
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public OptimizerResult OptimizeFirst(Func<double[], OptimizerResult> functionToMinimize, double threshold)
        {
            Optimize(functionToMinimize, threshold, out var best);

            return best;
        }

        private IReadOnlyCollection<OptimizerResult> Optimize(Func<double[], OptimizerResult> functionToMinimize, double threshold, out OptimizerResult best)
        {
            OptimizerResult bestResult = null;
            
            // Generate the cartesian product between all parameters
            var grid = CartesianProduct(m_parameters);

            // Initialize the search
            var results = new ConcurrentBag<OptimizerResult>();
            if (!m_runParallel)
            {
                foreach (var param in grid)
                {
                    // Get the current parameters for the current point
                    var result = functionToMinimize(param);
                    results.Add(result);
                    
                    if (bestResult == null) bestResult = result;
                    else if (result.Error < bestResult.Error) bestResult = result;

                    if (result.Error < threshold)
                    {
                        break;
                    }
                }
            }
            else
            {
                var rangePartitioner = Partitioner.Create(grid, true);
                var options = new ParallelOptions { MaxDegreeOfParallelism = m_maxDegreeOfParallelism };

                Parallel.ForEach(rangePartitioner, options, (param, loopState) =>
                {
                    // Get the current parameters for the current point
                    var result = functionToMinimize(param);
                    results.Add(result);

                    lock (results)
                    {
                        if (bestResult == null) bestResult = result;
                        else if (result.Error < bestResult.Error) bestResult = result;
                    }
                    
                    if (result.Error < threshold)
                    {
                        loopState.Break();
                    }
                });
            }

            best = bestResult;
            return results;
        }

        static double[][] CartesianProduct(IParameterSpec[] sequences)
        {
            var cartesian = CartesianProductEnumerable(sequences.Select(p => p.GetAllValues()));
            return cartesian.Select(row => row.ToArray()).ToArray();
        }

        static IEnumerable<IEnumerable<T>> CartesianProductEnumerable<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] { item }));
        }
    }

}
