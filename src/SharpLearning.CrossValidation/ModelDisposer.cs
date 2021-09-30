using System;
using System.Diagnostics.CodeAnalysis;
using SharpLearning.Common.Interfaces;

namespace SharpLearning.CrossValidation
{
    internal static class ModelDisposer
    {
        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        internal static void DisposeIfDisposable<TPrediction>(IPredictorModel<TPrediction> model)
        {
            if (model is IDisposable)
            {
                ((IDisposable)model).Dispose();
            }
        }
    }
}
