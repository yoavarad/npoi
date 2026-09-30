using NPOI.SS.Formula.Eval;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NPOI.SS.Formula.Functions
{
    /// <summary>Shared argument handling for the legacy statistical functions below.</summary>
    internal static class StatArgs
    {
        /// <summary>Flattens one argument into raw cell values (no coercion).</summary>
        public static List<ValueEval> Cells(ValueEval arg)
        {
            List<ValueEval> result = new List<ValueEval>();
            if(arg is TwoDEval area)
            {
                for(int r = 0; r < area.Height; r++)
                    for(int c = 0; c < area.Width; c++)
                        result.Add(area.GetValue(r, c));
            }
            else if(arg is RefEval re)
            {
                result.Add(re.GetInnerValueEval(re.FirstSheetIndex));
            }
            else
            {
                result.Add(arg);
            }
            return result;
        }

        /// <summary>Paired numeric values of two arrays; pairs where either side is not a number are dropped.</summary>
        public static void Pairs(ValueEval a, ValueEval b, out double[] xs, out double[] ys)
        {
            List<ValueEval> ca = Cells(a);
            List<ValueEval> cb = Cells(b);
            if(ca.Count != cb.Count)
                throw new EvaluationException(ErrorEval.NA);
            List<double> lx = new List<double>();
            List<double> ly = new List<double>();
            for(int i = 0; i < ca.Count; i++)
            {
                if(ca[i] is ErrorEval ea)
                    throw new EvaluationException(ea);
                if(cb[i] is ErrorEval eb)
                    throw new EvaluationException(eb);
                if(ca[i] is NumberEval na && cb[i] is NumberEval nb)
                {
                    lx.Add(na.NumberValue);
                    ly.Add(nb.NumberValue);
                }
            }
            xs = lx.ToArray();
            ys = ly.ToArray();
        }

        /// <summary>
        /// "A" variants: numbers count, TRUE/FALSE count as 1/0, text in references counts as 0,
        /// blanks are skipped; direct text arguments are coerced like other functions.
        /// </summary>
        public static double[] WithText(ValueEval[] args)
        {
            List<double> result = new List<double>();
            foreach(ValueEval arg in args)
            {
                bool isRef = arg is TwoDEval || arg is RefEval;
                foreach(ValueEval v in Cells(arg))
                {
                    if(v is ErrorEval err)
                        throw new EvaluationException(err);
                    if(v is NumberEval n)
                        result.Add(n.NumberValue);
                    else if(v is BoolEval b)
                        result.Add(b.BooleanValue ? 1 : 0);
                    else if(v is StringEval)
                        result.Add(isRef ? 0 : OperandResolver.CoerceValueToDouble(v));
                }
            }
            return result.ToArray();
        }
    }

    /// <summary>Base for legacy statistical built-ins with a fixed argument count range.</summary>
    public abstract class LegacyStatFunction : Function
    {
        private readonly int _min;
        private readonly int _max;

        protected LegacyStatFunction(int min, int max)
        {
            _min = min;
            _max = max;
        }

        protected abstract ValueEval Calc(ValueEval[] args, int row, int col);

        public ValueEval Evaluate(ValueEval[] args, int srcRowIndex, int srcColumnIndex)
        {
            if(args.Length < _min || args.Length > _max)
                return ErrorEval.VALUE_INVALID;
            try
            {
                return Calc(args, srcRowIndex, srcColumnIndex);
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

        protected static double Scalar(ValueEval arg, int row, int col)
        {
            return OperandResolver.CoerceValueToDouble(OperandResolver.GetSingleValue(arg, row, col));
        }

        protected static double[] Numbers(ValueEval[] args, int row, int col)
        {
            List<double> result = new List<double>();
            foreach(ValueEval arg in args)
            {
                if(arg is TwoDEval || arg is RefEval)
                {
                    foreach(ValueEval v in StatArgs.Cells(arg))
                    {
                        if(v is ErrorEval err)
                            throw new EvaluationException(err);
                        if(v is NumberEval n)
                            result.Add(n.NumberValue);
                    }
                }
                else
                {
                    result.Add(Scalar(arg, row, col));
                }
            }
            return result.ToArray();
        }
    }

    /// <summary>CORREL, PEARSON, RSQ, COVAR and STEYX share the sums of squares of paired data.</summary>
    public class PairedStatFunction : LegacyStatFunction
    {
        public enum Kind { Correl, Rsq, Covar, Steyx }

        public static readonly Function CORREL = new PairedStatFunction(Kind.Correl);
        public static readonly Function RSQ = new PairedStatFunction(Kind.Rsq);
        public static readonly Function COVAR = new PairedStatFunction(Kind.Covar);
        public static readonly Function STEYX = new PairedStatFunction(Kind.Steyx);

        private readonly Kind _kind;

        private PairedStatFunction(Kind kind) : base(2, 2)
        {
            _kind = kind;
        }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            StatArgs.Pairs(args[0], args[1], out double[] xs, out double[] ys);
            int n = xs.Length;
            if(n == 0)
                return ErrorEval.DIV_ZERO;
            double mx = xs.Average();
            double my = ys.Average();
            double sxx = 0, syy = 0, sxy = 0;
            for(int i = 0; i < n; i++)
            {
                double dx = xs[i] - mx;
                double dy = ys[i] - my;
                sxx += dx * dx;
                syy += dy * dy;
                sxy += dx * dy;
            }
            switch(_kind)
            {
                case Kind.Covar:
                    return Num(sxy / n);
                case Kind.Correl:
                    if(sxx == 0 || syy == 0)
                        return ErrorEval.DIV_ZERO;
                    return Num(sxy / Math.Sqrt(sxx * syy));
                case Kind.Rsq:
                    if(sxx == 0 || syy == 0)
                        return ErrorEval.DIV_ZERO;
                    return Num(sxy * sxy / (sxx * syy));
                default:
                    if(n < 3 || syy == 0)
                        return ErrorEval.DIV_ZERO;
                    return Num(Math.Sqrt(Math.Max(0, (sxx - sxy * sxy / syy) / (n - 2)))); // first arg is known_y
            }
        }
    }

    /// <summary>SKEW(number1, ...)</summary>
    public class Skew : LegacyStatFunction
    {
        public static readonly Function instance = new Skew();
        private Skew() : base(1, 255) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double[] v = Numbers(args, row, col);
            int n = v.Length;
            if(n < 3)
                return ErrorEval.DIV_ZERO;
            double m = v.Average();
            double s = Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (n - 1));
            if(s == 0)
                return ErrorEval.DIV_ZERO;
            double sum = v.Sum(x => Math.Pow((x - m) / s, 3));
            return Num(sum * n / ((n - 1.0) * (n - 2.0)));
        }
    }

    /// <summary>KURT(number1, ...)</summary>
    public class Kurt : LegacyStatFunction
    {
        public static readonly Function instance = new Kurt();
        private Kurt() : base(1, 255) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double[] v = Numbers(args, row, col);
            int n = v.Length;
            if(n < 4)
                return ErrorEval.DIV_ZERO;
            double m = v.Average();
            double s = Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (n - 1));
            if(s == 0)
                return ErrorEval.DIV_ZERO;
            double sum = v.Sum(x => Math.Pow((x - m) / s, 4));
            double a = (double)n * (n + 1) / ((n - 1.0) * (n - 2.0) * (n - 3.0));
            double b = 3.0 * (n - 1.0) * (n - 1.0) / ((n - 2.0) * (n - 3.0));
            return Num(a * sum - b);
        }
    }

    /// <summary>GEOMEAN and HARMEAN (all values must be positive).</summary>
    public class PositiveMean : LegacyStatFunction
    {
        public static readonly Function GEOMEAN = new PositiveMean(true);
        public static readonly Function HARMEAN = new PositiveMean(false);

        private readonly bool _geometric;

        private PositiveMean(bool geometric) : base(1, 255)
        {
            _geometric = geometric;
        }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double[] v = Numbers(args, row, col);
            if(v.Length == 0 || v.Any(x => x <= 0))
                return ErrorEval.NUM_ERROR;
            if(_geometric)
                return Num(Math.Exp(v.Sum(x => Math.Log(x)) / v.Length));
            return Num(v.Length / v.Sum(x => 1.0 / x));
        }
    }

    /// <summary>QUARTILE(array, quart)</summary>
    public class Quartile : LegacyStatFunction
    {
        public static readonly Function instance = new Quartile();
        private Quartile() : base(2, 2) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double[] v = Numbers(new[] { args[0] }, row, col);
            double q = Math.Truncate(Scalar(args[1], row, col));
            if(v.Length == 0 || q < 0 || q > 4)
                return ErrorEval.NUM_ERROR;
            Array.Sort(v);
            double pos = (v.Length - 1) * q / 4.0;
            int lo = (int)Math.Floor(pos);
            int hi = Math.Min(lo + 1, v.Length - 1);
            return Num(v[lo] + (pos - lo) * (v[hi] - v[lo]));
        }
    }

    /// <summary>TRIMMEAN(array, percent)</summary>
    public class TrimMean : LegacyStatFunction
    {
        public static readonly Function instance = new TrimMean();
        private TrimMean() : base(2, 2) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double[] v = Numbers(new[] { args[0] }, row, col);
            double p = Scalar(args[1], row, col);
            if(v.Length == 0 || p < 0 || p >= 1)
                return ErrorEval.NUM_ERROR;
            Array.Sort(v);
            int k = (int)Math.Floor(v.Length * p / 2.0);
            return Num(v.Skip(k).Take(v.Length - 2 * k).Average());
        }
    }

    /// <summary>AVERAGEA, STDEVA, STDEVPA, VARA, VARPA.</summary>
    public class AStatFunction : LegacyStatFunction
    {
        public enum Kind { Average, Stdev, StdevP, Var, VarP }

        public static readonly Function AVERAGEA = new AStatFunction(Kind.Average);
        public static readonly Function STDEVA = new AStatFunction(Kind.Stdev);
        public static readonly Function STDEVPA = new AStatFunction(Kind.StdevP);
        public static readonly Function VARA = new AStatFunction(Kind.Var);
        public static readonly Function VARPA = new AStatFunction(Kind.VarP);

        private readonly Kind _kind;

        private AStatFunction(Kind kind) : base(1, 255)
        {
            _kind = kind;
        }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double[] v = StatArgs.WithText(args);
            int n = v.Length;
            bool sample = _kind == Kind.Stdev || _kind == Kind.Var;
            if(n == 0 || (sample && n < 2))
                return ErrorEval.DIV_ZERO;
            double m = v.Average();
            if(_kind == Kind.Average)
                return Num(m);
            double ss = v.Sum(x => (x - m) * (x - m));
            double var = ss / (sample ? n - 1 : n);
            return Num(_kind == Kind.Stdev || _kind == Kind.StdevP ? Math.Sqrt(var) : var);
        }
    }

    /// <summary>FISHER(x)</summary>
    public class Fisher : LegacyStatFunction
    {
        public static readonly Function instance = new Fisher();
        private Fisher() : base(1, 1) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double x = Scalar(args[0], row, col);
            if(x <= -1 || x >= 1)
                return ErrorEval.NUM_ERROR;
            return Num(0.5 * Math.Log((1 + x) / (1 - x)));
        }
    }

    /// <summary>FISHERINV(y)</summary>
    public class FisherInv : LegacyStatFunction
    {
        public static readonly Function instance = new FisherInv();
        private FisherInv() : base(1, 1) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            return Num(Math.Tanh(Scalar(args[0], row, col)));
        }
    }

    /// <summary>PERMUT(number, number_chosen)</summary>
    public class Permut : LegacyStatFunction
    {
        public static readonly Function instance = new Permut();
        private Permut() : base(2, 2) { }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double n = Math.Truncate(Scalar(args[0], row, col));
            double k = Math.Truncate(Scalar(args[1], row, col));
            if(n <= 0 || k < 0 || n < k)
                return ErrorEval.NUM_ERROR;
            double result = 1;
            for(double i = 0; i < k; i++)
                result *= n - i;
            return Num(result);
        }
    }
}