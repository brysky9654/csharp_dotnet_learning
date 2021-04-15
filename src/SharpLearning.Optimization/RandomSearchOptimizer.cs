using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpLearning.Optimization.ParameterSamplers;

namespace SharpLearning.Optimization
{
    /// <summary>
    ///     Random search optimizer initializes random parameters between min and max of the provided parameters.
    ///     Roughly based on: http://www.jmlr.org/papers/volume13/bergstra12a/bergstra12a.pdf
    /// </summary>
    public sealed class RandomSearchOptimizer : IOptimizer
    {
        private readonly bool m_runParallel;
        private readonly IParameterSpec[] m_parameters;
        private readonly int m_iterations;
        private readonly IParameterSampler m_sampler;
        private readonly int m_maxDegreeOfParallelism = -1;

        /// <summary>
        ///     Random search optimizer initializes random parameters between min and max of the provided parameters.
        ///     Roughly based on: http://www.jmlr.org/papers/volume13/bergstra12a/bergstra12a.pdf
        /// </summary>
        /// <param name="parameters">A list of parameter specs, one for each optimization parameter</param>
        /// <param name="iterations">The number of iterations to perform</param>
        /// <param name="seed"></param>
        /// <param name="runParallel">Use multi threading to speed up execution (default is true)</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations (default is -1 (unlimited))</param>
        public RandomSearchOptimizer(
            IParameterSpec[] parameters,
            int iterations,
            int seed = 42,
            bool runParallel = false,
            int maxDegreeOfParallelism = -1)
        {
            m_parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            m_runParallel = runParallel;
            m_sampler = new RandomUniform(seed);
            m_iterations = iterations;
            m_maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        /// <summary>
        ///     Random search optimizer initializes random parameters between min and max of the provided bounds.
        ///     Returns the result which best minimizes the provided function.
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <returns></returns>
        public OptimizerResult OptimizeBest(Func<double[], OptimizerResult> functionToMinimize)
        {
            Optimize(functionToMinimize, 0.0f, out var best);

            return best;
        }

        /// <summary>
        ///     Random search optimizer initializes random parameters between min and max of the provided bounds.
        ///     Returns all results, chronologically ordered.
        ///     Note that the order of results might be affected if running parallel.
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <returns></returns>
        public IReadOnlyCollection<OptimizerResult> Optimize(Func<double[], OptimizerResult> functionToMinimize)
        {
            return Optimize(functionToMinimize, 0.0, out _);
        }

        /// <summary>
        ///     Random search optimizer initializes random parameters between min and max of the provided bounds. Returns the best
        ///     result.
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public OptimizerResult OptimizeFirst(Func<double[], OptimizerResult> functionToMinimize, double threshold)
        {
            Optimize(functionToMinimize, threshold, out var best);

            return best;
        }

        private IReadOnlyCollection<OptimizerResult> Optimize(
            Func<double[], OptimizerResult> functionToMinimize,
            double threshold,
            out OptimizerResult best)
        {
            OptimizerResult bestResult = null;

            // Generate the cartesian product between all parameters
            var parameterSets = SampleRandomParameterSets(m_iterations, m_parameters, m_sampler);

            // Initialize the search
            var results = new ConcurrentBag<OptimizerResult>();

            if (!m_runParallel)
            {
                foreach (var parameterSet in parameterSets)
                {
                    // Get the current parameters for the current point
                    var result = functionToMinimize(parameterSet);
                    results.Add(result);

                    if (bestResult == null)
                    {
                        bestResult = result;
                    }
                    else if (result.Error < bestResult.Error)
                    {
                        bestResult = result;
                    }

                    if (result.Error < threshold)
                    {
                        break;
                    }
                }
            }
            else
            {
                var rangePartitioner = Partitioner.Create(parameterSets, true);
                var options = new ParallelOptions {MaxDegreeOfParallelism = m_maxDegreeOfParallelism};
                Parallel.ForEach(
                    rangePartitioner,
                    options,
                    (param, loopState) =>
                    {
                        // Get the current parameters for the current point
                        var result = functionToMinimize(param);
                        results.Add(result);

                        lock (results)
                        {
                            if (bestResult == null)
                            {
                                bestResult = result;
                            }
                            else if (result.Error < bestResult.Error)
                            {
                                bestResult = result;
                            }
                        }

                        if (result.Error < threshold)
                        {
                            loopState.Break();
                        }
                    }
                );
            }

            best = bestResult;
            return results.ToArray();
        }

        /// <summary>
        ///     Samples a set of random parameter sets.
        /// </summary>
        /// <param name="parameterSetCount"></param>
        /// <param name="parameters"></param>
        /// <param name="sampler"></param>
        /// <returns></returns>
        public static double[][] SampleRandomParameterSets(
            int parameterSetCount,
            IParameterSpec[] parameters,
            IParameterSampler sampler)
        {
            var parameterSets = new double[parameterSetCount][];
            for (var i = 0; i < parameterSetCount; i++)
            {
                parameterSets[i] = SampleParameterSet(parameters, sampler);
            }

            return parameterSets;
        }

        /// <summary>
        ///     Samples a random parameter set.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="sampler"></param>
        /// <returns></returns>
        public static double[] SampleParameterSet(IParameterSpec[] parameters, IParameterSampler sampler)
        {
            var parameterSet = new double[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                parameterSet[i] = parameter.SampleValue(sampler);
            }

            return parameterSet;
        }
    }
}
