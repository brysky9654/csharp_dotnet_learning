using System;
using System.Collections.Generic;

namespace SharpLearning.Optimization
{
    /// <summary>
    /// 
    /// </summary>
    public interface IOptimizer
    {
        /// <summary>
        /// Returns the result which best minimises the provided function.
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <returns></returns>
        OptimizerResult OptimizeBest(Func<double[], OptimizerResult> functionToMinimize);
        
        /// <summary>
        /// Returns all results ordered from best to worst (minimized). 
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <returns></returns>
        IReadOnlyCollection<OptimizerResult> Optimize(Func<double[], OptimizerResult> functionToMinimize);


        /// <summary>
        /// Returns the first result which minimizes the provided function below the threshold.
        /// </summary>
        /// <param name="functionToMinimize"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        OptimizerResult OptimizeFirst(Func<double[], OptimizerResult> functionToMinimize, double threshold);


    }
}
