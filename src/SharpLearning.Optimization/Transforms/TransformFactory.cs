using System;

namespace SharpLearning.Optimization.Transforms
{
    /// <summary>
    /// Return a transform from predefined selections.
    /// </summary>
    public static class TransformFactory
    {
        /// <summary>
        /// Return a transform from predefined selections.
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static ITransform Create(TransformType transformType)
        {
            switch (transformType)
            {
                case TransformType.Linear:
                    return new LinearTransform();
                case TransformType.Log10:
                    return new Log10Transform();
                case TransformType.ExponentialAverage:
                    return new ExponentialAverageTransform();
                default:
                    throw new ArgumentException("Unsupported transform: " + transformType);
            }
        }
    }
}
