using NPOI.SS.Formula.Eval;
using System;
using System.Collections.Generic;

namespace NPOI.SS.Formula.Functions
{
    /// <summary>
    /// Shared helpers for the simple numeric Analysis ToolPak functions below.
    /// </summary>
    internal static class NumericArgs
    {
        /// <summary>Flattens scalars and areas into doubles (blank/text cells in areas are skipped).</summary>
        public static List<double> Flatten(ValueEval[] args, int row, int col)
        {
            List<double> result = new List<double>();
            foreach(ValueEval arg in args)
            {
                if(arg is TwoDEval area)
                {
                    for(int r = 0; r < area.Height; r++)
                    {
                        for(int c = 0; c < area.Width; c++)
                        {
                            ValueEval v = area.GetValue(r, c);
                            if(v is ErrorEval err)
                                throw new EvaluationException(err);
                            if(v is NumberEval n)
                                result.Add(n.NumberValue);
                        }
                    }
                }
                else
                {
                    ValueEval v = OperandResolver.GetSingleValue(arg, row, col);
                    result.Add(OperandResolver.CoerceValueToDouble(v));
                }
            }
            return result;
        }

        public static double Scalar(ValueEval arg, int row, int col)
        {
            return OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(arg, row, col));
        }
    }

    /// <summary>Base for Analysis ToolPak numeric functions with a fixed argument count range.</summary>
    public abstract class SimpleNumericFunction : FreeRefFunction
    {
        private readonly int _min;
        private readonly int _max;

        protected SimpleNumericFunction(int min, int max)
        {
            _min = min;
            _max = max;
        }

        protected abstract ValueEval Calc(ValueEval[] args, int row, int col);

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if(args.Length < _min || args.Length > _max)
            {
                return ErrorEval.VALUE_INVALID;
            }
            try
            {
                return Calc(args, ec.RowIndex, ec.ColumnIndex);
            }
            catch(EvaluationException e)
            {
                return e.GetErrorEval();
            }
        }

        protected static ValueEval Num(double d)
        {
            if(double.IsNaN(d) || double.IsInfinity(d))
                return ErrorEval.NUM_ERROR;
            return new NumberEval(d);
        }
    }

    /// <summary>Excel GCD(number1, [number2], ...)</summary>
    public class Gcd : SimpleNumericFunction
    {
        public static readonly FreeRefFunction instance = new Gcd();
        private Gcd() : base(1, 255) { }

        internal static double Of(double a, double b)
        {
            while(b != 0)
            {
                double t = a % b;
                a = b;
                b = t;
            }
            return a;
        }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double g = 0;
            foreach(double d in NumericArgs.Flatten(args, row, col))
            {
                double n = Math.Truncate(d);
                if(n < 0 || n >= 9007199254740992d)
                    return ErrorEval.NUM_ERROR;
                g = Of(g, n);
            }
            return new NumberEval(g);
        }
    }

    /// <summary>Excel LCM(number1, [number2], ...)</summary>
    public class Lcm : SimpleNumericFunction
    {
        public static readonly FreeRefFunction instance = new Lcm();
        private Lcm() : base(1, 255) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double l = 1;
            bool zero = false;
            foreach(double d in NumericArgs.Flatten(args, row, col))
            {
                double n = Math.Truncate(d);
                if(n < 0 || n >= 9007199254740992d)
                    return ErrorEval.NUM_ERROR;
                if(n == 0)
                {
                    zero = true;
                    continue;
                }
                l = l / Gcd.Of(l, n) * n;
                if(l >= 9007199254740992d)
                    return ErrorEval.NUM_ERROR;
            }
            return new NumberEval(zero ? 0 : l);
        }
    }

    /// <summary>Excel SQRTPI(number)</summary>
    public class SqrtPi : SimpleNumericFunction
    {
        public static readonly FreeRefFunction instance = new SqrtPi();
        private SqrtPi() : base(1, 1) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double n = NumericArgs.Scalar(args[0], row, col);
            return n < 0 ? ErrorEval.NUM_ERROR : Num(Math.Sqrt(n * Math.PI));
        }
    }

    /// <summary>Excel EFFECT(nominal_rate, npery)</summary>
    public class Effect : SimpleNumericFunction
    {
        public static readonly FreeRefFunction instance = new Effect();
        private Effect() : base(2, 2) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double rate = NumericArgs.Scalar(args[0], row, col);
            double npery = Math.Truncate(NumericArgs.Scalar(args[1], row, col));
            if(rate <= 0 || npery < 1)
                return ErrorEval.NUM_ERROR;
            return Num(Math.Pow(1 + rate / npery, npery) - 1);
        }
    }

    /// <summary>Excel NOMINAL(effect_rate, npery)</summary>
    public class Nominal : SimpleNumericFunction
    {
        public static readonly FreeRefFunction instance = new Nominal();
        private Nominal() : base(2, 2) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double rate = NumericArgs.Scalar(args[0], row, col);
            double npery = Math.Truncate(NumericArgs.Scalar(args[1], row, col));
            if(rate <= 0 || npery < 1)
                return ErrorEval.NUM_ERROR;
            return Num(npery * (Math.Pow(rate + 1, 1 / npery) - 1));
        }
    }

    /// <summary>Excel MULTINOMIAL(number1, [number2], ...)</summary>
    public class Multinomial : SimpleNumericFunction
    {
        public static readonly FreeRefFunction instance = new Multinomial();
        private Multinomial() : base(1, 255) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            // product of binomials avoids factorial overflow
            double total = 0;
            double result = 1;
            foreach(double d in NumericArgs.Flatten(args, row, col))
            {
                double n = Math.Truncate(d);
                if(n < 0 || n > 1000)
                    return ErrorEval.NUM_ERROR;
                for(double k = 1; k <= n; k++)
                {
                    total++;
                    result = result * total / k;
                }
            }
            return Num(Math.Round(result));
        }
    }

    /// <summary>Excel GESTEP(number, [step])</summary>
    public class GeStep : SimpleNumericFunction
    {
        public static readonly FreeRefFunction instance = new GeStep();
        private GeStep() : base(1, 2) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double n = NumericArgs.Scalar(args[0], row, col);
            double step = args.Length > 1 ? NumericArgs.Scalar(args[1], row, col) : 0;
            return new NumberEval(n >= step ? 1 : 0);
        }
    }
}