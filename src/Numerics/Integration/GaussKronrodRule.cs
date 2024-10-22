﻿// <copyright file="GaussKronrodRule.cs" company="Math.NET">
// Math.NET Numerics, part of the Math.NET Project
// http://numerics.mathdotnet.com
// http://github.com/mathnet/mathnet-numerics
//
// Copyright (c) 2009-2019 Math.NET
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

// This file uses code from the Boost Project.
//  Copyright John Maddock 2017.
//  Copyright Nick Thompson 2017.
//  Use, modification and distribution are subject to the
//  Boost Software License, Version 1.0. (See accompanying file
//  LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)
//  https://github.com/boostorg/math/blob/develop/include/boost/math/quadrature/gauss_kronrod.hpp

using MathNet.Numerics.Integration.GaussRule;
using System;
using System.Numerics;

namespace MathNet.Numerics.Integration
{
    public class GaussKronrodRule
    {
        private readonly GaussPointPair gaussKronrodPoint;

        /// <summary>
        /// Getter for the order.
        /// </summary>
        public int Order
        {
            get
            {
                return gaussKronrodPoint.Order;
            }
        }

        /// <summary>
        /// Getter that returns a clone of the array containing the Kronrod abscissas.
        /// </summary>
        public double[] KronrodAbscissas
        {
            get
            {
                return gaussKronrodPoint.Abscissas.Clone() as double[];
            }
        }

        /// <summary>
        /// Getter that returns a clone of the array containing the Kronrod weights.
        /// </summary>
        public double[] KronrodWeights
        {
            get
            {
                return gaussKronrodPoint.Weights.Clone() as double[];
            }
        }

        /// <summary>
        /// Getter that returns a clone of the array containing the Gauss weights.
        /// </summary>
        public double[] GaussWeights
        {
            get
            {
                return gaussKronrodPoint.SecondWeights.Clone() as double[];
            }
        }

        public GaussKronrodRule(int order)
        {
            gaussKronrodPoint = GaussKronrodPointFactory.GetGaussPoint(order);
        }

        /// <summary>
        /// Performs adaptive Gauss-Kronrod quadrature on function f over the range (a,b)
        /// </summary>
        /// <param name="f">The analytic smooth function to integrate</param>
        /// <param name="intervalBegin">Where the interval starts</param>
        /// <param name="intervalEnd">Where the interval stops</param>
        /// <param name="error">The difference between the (N-1)/2 point Gauss approximation and the N-point Gauss-Kronrod approximation</param>
        /// <param name="L1Norm">The L1 norm of the result, if there is a significant difference between this and the returned value, then the result is likely to be ill-conditioned.</param>
        /// <param name="targetRelativeError">The maximum relative error in the result</param>
        /// <param name="maximumDepth">The maximum number of interval splittings permitted before stopping</param>
        /// <param name="order">The number of Gauss-Kronrod points. Pre-computed for 15, 21, 31, 41, 51 and 61 points</param>
        public static double Integrate(Func<double, double> f, double intervalBegin, double intervalEnd, out double error, out double L1Norm, double targetRelativeError = 1E-10, int maximumDepth = 15, int order = 15)
        {
            // Formula used for variable subsitution from 
            // 1. Shampine, L. F. (2008). Vectorized adaptive quadrature in MATLAB. Journal of Computational and Applied Mathematics, 211(2), 131-140.
            // 2. quadgk.m, GNU Octave

            if (f == null)
            {
                throw new ArgumentNullException(nameof(f));
            }

            if (intervalBegin > intervalEnd)
            {
                return -Integrate(f, intervalEnd, intervalBegin, out error, out L1Norm, targetRelativeError, maximumDepth, order);
            }

            GaussPointPair gaussKronrodPoint = GaussKronrodPointFactory.GetGaussPoint(order);

            // (-oo, oo) => [-1, 1]
            //
            // integral_(-oo)^(oo) f(x) dx = integral_(-1)^(1) f(g(t)) g'(t) dt
            // g(t) = t / (1 - t^2)
            // g'(t) = (1 + t^2) / (1 - t^2)^2
            if ((intervalBegin < double.MinValue) && (intervalEnd > double.MaxValue))
            {
                Func<double, double> u = (t) =>
                {
                    return (f(t / (1 - (t * t))) * (1 + (t * t))) / ((1 - (t * t)) * (1 - (t * t)));
                };
                return recursive_adaptive_integrate(u, -1, 1, maximumDepth, targetRelativeError, 0, out error, out L1Norm, gaussKronrodPoint);
            }
            // [a, oo) => [0, 1]
            //
            // integral_(a)^(oo) f(x) dx = integral_(0)^(oo) f(a + t^2) 2 t dt
            //                           = integral_(0)^(1) f(a + g(s)^2) 2 g(s) g'(s) ds
            // g(s) = s / (1 - s)
            // g'(s) = 1 / (1 - s)^2
            else if (intervalEnd > double.MaxValue)
            {
                Func<double, double> u = (s) =>
                {
                    return (2 * s * f(intervalBegin + ((s / (1 - s)) * (s / (1 - s))))) / ((1 - s) * (1 - s) * (1 - s));
                };
                return recursive_adaptive_integrate(u, 0, 1, maximumDepth, targetRelativeError, 0, out error, out L1Norm, gaussKronrodPoint);
            }
            // (-oo, b] => [-1, 0]
            //
            // integral_(-oo)^(b) f(x) dx = -integral_(-oo)^(0) f(b - t^2) 2 t dt
            //                            = -integral_(-1)^(0) f(b - g(s)^2) 2 g(s) g'(s) ds
            // g(s) = s / (1 + s)
            // g'(s) = 1 / (1 + s)^2
            else if (intervalBegin < double.MinValue)
            {
                Func<double, double> u = (s) =>
                {
                    return (-2 * s * f(intervalEnd - ((s / (1 + s)) * (s / (1 + s))))) / ((1 + s) * (1 + s) * (1 + s));
                };
                return recursive_adaptive_integrate(u, -1, 0, maximumDepth, targetRelativeError, 0, out error, out L1Norm, gaussKronrodPoint);
            }
            // [a, b] => [-1, 1]
            //
            // integral_(a)^(b) f(x) dx = integral_(-1)^(1) f(g(t)) g'(t) dt
            // g(t) = (b - a) * t * (3 - t^2) / 4 + (b + a) / 2
            // g'(t) = 3 / 4 * (b - a) * (1 - t^2)
            else
            {
                Func<double, double> u = (t) =>
                {
                    return ((f((((intervalEnd - intervalBegin) / 4) * t * (3 - (t * t))) + ((intervalEnd + intervalBegin) / 2)) * 3 * (intervalEnd - intervalBegin)) / 4) * (1 - (t * t));
                };
                return recursive_adaptive_integrate(u, -1, 1, maximumDepth, targetRelativeError, 0d, out error, out L1Norm, gaussKronrodPoint);
            }
        }

        /// <summary>
        /// Performs adaptive Gauss-Kronrod quadrature on function f over the range (a,b)
        /// </summary>
        /// <param name="f">The analytic smooth complex function to integrate, defined on the real axis.</param>
        /// <param name="intervalBegin">Where the interval starts</param>
        /// <param name="intervalEnd">Where the interval stops</param>
        /// <param name="error">The difference between the (N-1)/2 point Gauss approximation and the N-point Gauss-Kronrod approximation</param>
        /// <param name="L1Norm">The L1 norm of the result, if there is a significant difference between this and the returned value, then the result is likely to be ill-conditioned.</param>
        /// <param name="targetRelativeError">The maximum relative error in the result</param>
        /// <param name="maximumDepth">The maximum number of interval splittings permitted before stopping</param>
        /// <param name="order">The number of Gauss-Kronrod points. Pre-computed for 15, 21, 31, 41, 51 and 61 points</param>
        /// <returns></returns>
        public static Complex ContourIntegrate(Func<double, Complex> f, double intervalBegin, double intervalEnd, out double error, out double L1Norm, double targetRelativeError = 1E-10, int maximumDepth = 15, int order = 15)
        {
            // Formula used for variable subsitution from 
            // 1. Shampine, L. F. (2008). Vectorized adaptive quadrature in MATLAB. Journal of Computational and Applied Mathematics, 211(2), 131-140.
            // 2. quadgk.m, GNU Octave

            if (f == null)
            {
                throw new ArgumentNullException(nameof(f));
            }

            if (intervalBegin > intervalEnd)
            {
                return -ContourIntegrate(f, intervalEnd, intervalBegin, out error, out L1Norm, targetRelativeError, maximumDepth, order);
            }

            GaussPointPair gaussKronrodPoint = GaussKronrodPointFactory.GetGaussPoint(order);

            // (-oo, oo) => [-1, 1]
            //
            // integral_(-oo)^(oo) f(x) dx = integral_(-1)^(1) f(g(t)) g'(t) dt
            // g(t) = t / (1 - t^2)
            // g'(t) = (1 + t^2) / (1 - t^2)^2
            if ((intervalBegin < double.MinValue) && (intervalEnd > double.MaxValue))
            {
                Func<double, Complex> u = (t) =>
                {
                    return (f(t / (1 - (t * t))) * (1 + (t * t))) / ((1 - (t * t)) * (1 - (t * t)));
                };
                return contour_recursive_adaptive_integrate(u, -1, 1, maximumDepth, targetRelativeError, 0, out error, out L1Norm, gaussKronrodPoint);
            }
            // [a, oo) => [0, 1]
            //
            // integral_(a)^(oo) f(x) dx = integral_(0)^(oo) f(a + t^2) 2 t dt
            //                           = integral_(0)^(1) f(a + g(s)^2) 2 g(s) g'(s) ds
            // g(s) = s / (1 - s)
            // g'(s) = 1 / (1 - s)^2
            else if (intervalEnd > double.MaxValue)
            {
                Func<double, Complex> u = (s) =>
                {
                    return (2 * s * f(intervalBegin + ((s / (1 - s)) * (s / (1 - s))))) / ((1 - s) * (1 - s) * (1 - s));
                };
                return contour_recursive_adaptive_integrate(u, 0, 1, maximumDepth, targetRelativeError, 0, out error, out L1Norm, gaussKronrodPoint);
            }
            // (-oo, b] => [-1, 0]
            //
            // integral_(-oo)^(b) f(x) dx = -integral_(-oo)^(0) f(b - t^2) 2 t dt
            //                            = -integral_(-1)^(0) f(b - g(s)^2) 2 g(s) g'(s) ds
            // g(s) = s / (1 + s)
            // g'(s) = 1 / (1 + s)^2
            else if (intervalBegin < double.MinValue)
            {
                Func<double, Complex> u = (s) =>
                {
                    return (-2 * s * f(intervalEnd - ((s / (1 + s)) * (s / (1 + s))))) / ((1 + s) * (1 + s) * (1 + s));
                };
                return contour_recursive_adaptive_integrate(u, -1, 0, maximumDepth, targetRelativeError, 0, out error, out L1Norm, gaussKronrodPoint);
            }
            // [a, b] => [-1, 1]
            //
            // integral_(a)^(b) f(x) dx = integral_(-1)^(1) f(g(t)) g'(t) dt
            // g(t) = (b - a) * t * (3 - t^2) / 4 + (b + a) / 2
            // g'(t) = 3 / 4 * (b - a) * (1 - t^2)
            else
            {
                Func<double, Complex> u = (t) =>
                {
                    return ((f((((intervalEnd - intervalBegin) / 4) * t * (3 - (t * t))) + ((intervalEnd + intervalBegin) / 2)) * 3 * (intervalEnd - intervalBegin)) / 4) * (1 - (t * t));
                };
                return contour_recursive_adaptive_integrate(u, -1, 1, maximumDepth, targetRelativeError, 0d, out error, out L1Norm, gaussKronrodPoint);
            }
        }

        private static double integrate_non_adaptive_m1_1(Func<double, double> f, out double error, out double pL1, GaussPointPair gaussKronrodPoint)
        {
            int gauss_start = 2;
            int kronrod_start = 1;
            int gauss_order = (gaussKronrodPoint.Order - 1) / 2;

            double kronrod_result = 0d;
            double gauss_result = 0d;
            double fp, fm;

            var KAbscissa = gaussKronrodPoint.Abscissas;
            var KWeights = gaussKronrodPoint.Weights;
            var GWeights = gaussKronrodPoint.SecondWeights;

            if ((gauss_order & 1) == 1)
            {
                fp = f(0);
                kronrod_result = fp * KWeights[0];
                gauss_result += fp * GWeights[0];
            }
            else
            {
                fp = f(0);
                kronrod_result = fp * KWeights[0];
                gauss_start = 1;
                kronrod_start = 2;
            }
            double L1 = Math.Abs(kronrod_result);

            for (int i = gauss_start; i < KAbscissa.Length; i += 2)
            {
                fp = f(KAbscissa[i]);
                fm = f(-KAbscissa[i]);
                kronrod_result += (fp + fm) * KWeights[i];
                L1 += (Math.Abs(fp) + Math.Abs(fm)) * KWeights[i];
                gauss_result += (fp + fm) * GWeights[i / 2];
            }
            for (int i = kronrod_start; i < KAbscissa.Length; i += 2)
            {
                fp = f(KAbscissa[i]);
                fm = f(-KAbscissa[i]);
                kronrod_result += (fp + fm) * KWeights[i];
                L1 += (Math.Abs(fp) + Math.Abs(fm)) * KWeights[i];
            }
            pL1 = L1;
            error = Math.Max(Math.Abs(kronrod_result - gauss_result), Math.Abs(kronrod_result * Precision.MachineEpsilon * 2d));
            return kronrod_result;
        }

        private static Complex contour_integrate_non_adaptive_m1_1(Func<double, Complex> f, out double error, out double pL1, GaussPointPair gaussKronrodPoint)
        {
            int gauss_start = 2;
            int kronrod_start = 1;
            int gauss_order = (gaussKronrodPoint.Order - 1) / 2;

            Complex kronrod_result = new Complex();
            Complex gauss_result = new Complex();
            Complex fp, fm;

            var KAbscissa = gaussKronrodPoint.Abscissas;
            var KWeights = gaussKronrodPoint.Weights;
            var GWeights = gaussKronrodPoint.SecondWeights;

            if (gauss_order.IsOdd())
            {
                fp = f(0);
                kronrod_result = fp * KWeights[0];
                gauss_result += fp * GWeights[0];
            }
            else
            {
                fp = f(0);
                kronrod_result = fp * KWeights[0];
                gauss_start = 1;
                kronrod_start = 2;
            }
            double L1 = Complex.Abs(kronrod_result);

            for (int i = gauss_start; i < KAbscissa.Length; i += 2)
            {
                fp = f(KAbscissa[i]);
                fm = f(-KAbscissa[i]);
                kronrod_result += (fp + fm) * KWeights[i];
                L1 += (Complex.Abs(fp) + Complex.Abs(fm)) * KWeights[i];
                gauss_result += (fp + fm) * GWeights[i / 2];
            }
            for (int i = kronrod_start; i < KAbscissa.Length; i += 2)
            {
                fp = f(KAbscissa[i]);
                fm = f(-KAbscissa[i]);
                kronrod_result += (fp + fm) * KWeights[i];
                L1 += (Complex.Abs(fp) + Complex.Abs(fm)) * KWeights[i];
            }
            pL1 = L1;
            error = Math.Max(Complex.Abs(kronrod_result - gauss_result), Complex.Abs(kronrod_result * Precision.MachineEpsilon * 2d));
            return kronrod_result;
        }

        private static double recursive_adaptive_integrate(Func<double, double> f, double a, double b, int max_levels, double rel_tol, double abs_tol, out double error, out double L1, GaussPointPair gaussKronrodPoint)
        {
            double error_local;
            double mean = (b + a) / 2;
            double scale = (b - a) / 2;

            var r1 = integrate_non_adaptive_m1_1((x) => f((scale * x) + mean), out error_local, out L1, gaussKronrodPoint);
            var estimate = scale * r1;

            var tmp = estimate * rel_tol;
            var abs_tol1 = Math.Abs(tmp);
            if (abs_tol == 0)
            {
                abs_tol = abs_tol1;
            }

            if ((max_levels > 0) && (abs_tol1 < error_local) && (abs_tol < error_local))
            {
                double mid = (a + b) / 2d;
                double L1_local;
                estimate = recursive_adaptive_integrate(f, a, mid, max_levels - 1, rel_tol, abs_tol / 2, out error, out L1, gaussKronrodPoint);
                estimate += recursive_adaptive_integrate(f, mid, b, max_levels - 1, rel_tol, abs_tol / 2, out error_local, out L1_local, gaussKronrodPoint);
                error += error_local;
                L1 += L1_local;
                return estimate;
            }
            L1 *= scale;
            error = error_local;
            return estimate;
        }

        private static Complex contour_recursive_adaptive_integrate(Func<double, Complex> f, double a, double b, int max_levels, double rel_tol, double abs_tol, out double error, out double L1, GaussPointPair gaussKronrodPoint)
        {
            double error_local;
            double mean = (b + a) / 2;
            double scale = (b - a) / 2;

            var r1 = contour_integrate_non_adaptive_m1_1((x) => f((scale * x) + mean), out error_local, out L1, gaussKronrodPoint);
            var estimate = scale * r1;

            var tmp = estimate * rel_tol;
            var abs_tol1 = Complex.Abs(tmp);
            if (abs_tol == 0)
            {
                abs_tol = abs_tol1;
            }

            if ((max_levels > 0) && (abs_tol1 < error_local) && (abs_tol < error_local))
            {
                double mid = (a + b) / 2d;
                double L1_local;
                estimate = contour_recursive_adaptive_integrate(f, a, mid, max_levels - 1, rel_tol, abs_tol / 2, out error, out L1, gaussKronrodPoint);
                estimate += contour_recursive_adaptive_integrate(f, mid, b, max_levels - 1, rel_tol, abs_tol / 2, out error_local, out L1_local, gaussKronrodPoint);
                error += error_local;
                L1 += L1_local;
                return estimate;
            }
            L1 *= scale;
            error = error_local;
            return estimate;
        }
    }
}
