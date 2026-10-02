using NPOI.SS.Formula.Eval;
using System;
using System.Collections.Generic;
using System.Globalization;
using SysComplex = System.Numerics.Complex;

namespace NPOI.SS.Formula.Functions
{
    /// <summary>Excel base conversions BIN2HEX, BIN2OCT, DEC2OCT, HEX2BIN, HEX2OCT, OCT2BIN, OCT2HEX.</summary>
    public class BaseConvert : SimpleNumericFunction
    {
        public static readonly FreeRefFunction BIN2HEX = new BaseConvert(2, 16);
        public static readonly FreeRefFunction BIN2OCT = new BaseConvert(2, 8);
        public static readonly FreeRefFunction DEC2OCT = new BaseConvert(10, 8);
        public static readonly FreeRefFunction HEX2BIN = new BaseConvert(16, 2);
        public static readonly FreeRefFunction HEX2OCT = new BaseConvert(16, 8);
        public static readonly FreeRefFunction OCT2BIN = new BaseConvert(8, 2);
        public static readonly FreeRefFunction OCT2HEX = new BaseConvert(8, 16);

        private readonly int _from;
        private readonly int _to;

        private BaseConvert(int from, int to) : base(1, 2)
        {
            _from = from;
            _to = to;
        }

        private static int BitsOf(int b)
        {
            return b == 2 ? 10 : b == 8 ? 30 : 40;
        }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            string text = OperandResolver.CoerceValueToString(OperandResolver.GetSingleValue(args[0], row, col));
            long value;
            if(_from == 10)
            {
                double d = text.Trim().Length == 0 ? 0 : OperandResolver.ParseDouble(text);
                if(double.IsNaN(d))
                    return ErrorEval.VALUE_INVALID;
                value = (long) Math.Truncate(d);
                if(value < -(1L << 39) || value > (1L << 39) - 1)
                    return ErrorEval.NUM_ERROR;
            }
            else
            {
                text = text.Trim();
                if(text.Length > 10)
                    return ErrorEval.NUM_ERROR;
                value = 0;
                foreach(char ch in text)
                {
                    int digit = ch >= '0' && ch <= '9' ? ch - '0'
                        : ch >= 'A' && ch <= 'F' ? ch - 'A' + 10
                        : ch >= 'a' && ch <= 'f' ? ch - 'a' + 10 : 99;
                    if(digit >= _from)
                        return ErrorEval.NUM_ERROR;
                    value = value * _from + digit;
                }
                long full = 1L << BitsOf(_from);
                if(text.Length == 10 && value >= full / 2)
                    value -= full;
            }

            int places = 0;
            if(args.Length == 2)
            {
                double p = NumericArgs.Scalar(args[1], row, col);
                places = (int) Math.Truncate(p);
                if(places <= 0 || places > 10)
                    return ErrorEval.NUM_ERROR;
            }

            long toFull = 1L << BitsOf(_to);
            if(value < -toFull / 2 || value >= toFull / 2)
                return ErrorEval.NUM_ERROR;

            string result;
            if(value < 0)
            {
                result = ToBase(value + toFull, _to);
                result = result.PadLeft(10, _to == 16 ? 'F' : _to == 8 ? '7' : '1');
            }
            else
            {
                result = ToBase(value, _to);
                if(places > 0)
                {
                    if(result.Length > places)
                        return ErrorEval.NUM_ERROR;
                    result = result.PadLeft(places, '0');
                }
            }
            return new StringEval(result);
        }

        private static string ToBase(long v, int b)
        {
            if(v == 0)
                return "0";
            var sb = new System.Text.StringBuilder();
            while(v > 0)
            {
                sb.Insert(0, "0123456789ABCDEF"[(int) (v % b)]);
                v /= b;
            }
            return sb.ToString();
        }
    }

    /// <summary>Excel ERF(lower, [upper]) and ERFC(x).</summary>
    public class ErfFunction : SimpleNumericFunction
    {
        public static readonly FreeRefFunction ERF = new ErfFunction(false);
        public static readonly FreeRefFunction ERFC = new ErfFunction(true);

        private readonly bool _complement;

        private ErfFunction(bool complement) : base(1, complement ? 1 : 2)
        {
            _complement = complement;
        }

        internal static double Erfc(double x)
        {
            if(x < 0)
                return 2 - Erfc(-x);
            if(x < 1.5)
                return 1 - ErfSeries(x);
            double t = x;
            for(int k = 300; k >= 1; k--)
                t = x + (k / 2.0) / t;
            return Math.Exp(-x * x) / (Math.Sqrt(Math.PI) * t);
        }

        internal static double Erf(double x)
        {
            if(x < 0)
                return -Erf(-x);
            return x < 3 ? ErfSeries(x) : 1 - Erfc(x);
        }

        // erf(x) = 2/sqrt(pi) * exp(-x^2) * sum 2^n x^(2n+1) / (1*3*...*(2n+1)); all terms positive.
        private static double ErfSeries(double x)
        {
            double term = x;
            double sum = x;
            for(int n = 1; n < 200; n++)
            {
                term *= 2 * x * x / (2 * n + 1);
                sum += term;
                if(term < sum * 1e-17)
                    break;
            }
            return 2 / Math.Sqrt(Math.PI) * Math.Exp(-x * x) * sum;
        }

        protected override ValueEval Calc(ValueEval[] args, int row, int col)
        {
            double a = NumericArgs.Scalar(args[0], row, col);
            if(_complement)
                return Num(Erfc(a));
            if(args.Length == 1)
                return Num(Erf(a));
            double b = NumericArgs.Scalar(args[1], row, col);
            return Num(Erf(b) - Erf(a));
        }
    }

    /// <summary>Excel complex-number functions IMABS ... IMSUM operating on text such as "3+4i".</summary>
    public class ComplexFunction : FreeRefFunction
    {
        private enum Op
        {
            Abs, Argument, Conjugate, Cos, Div, Exp, Ln, Log10, Log2, Power, Product, Sin, Sqrt, Sub, Sum
        }

        public static readonly FreeRefFunction IMABS = new ComplexFunction(Op.Abs, 1, 1);
        public static readonly FreeRefFunction IMARGUMENT = new ComplexFunction(Op.Argument, 1, 1);
        public static readonly FreeRefFunction IMCONJUGATE = new ComplexFunction(Op.Conjugate, 1, 1);
        public static readonly FreeRefFunction IMCOS = new ComplexFunction(Op.Cos, 1, 1);
        public static readonly FreeRefFunction IMDIV = new ComplexFunction(Op.Div, 2, 2);
        public static readonly FreeRefFunction IMEXP = new ComplexFunction(Op.Exp, 1, 1);
        public static readonly FreeRefFunction IMLN = new ComplexFunction(Op.Ln, 1, 1);
        public static readonly FreeRefFunction IMLOG10 = new ComplexFunction(Op.Log10, 1, 1);
        public static readonly FreeRefFunction IMLOG2 = new ComplexFunction(Op.Log2, 1, 1);
        public static readonly FreeRefFunction IMPOWER = new ComplexFunction(Op.Power, 2, 2);
        public static readonly FreeRefFunction IMPRODUCT = new ComplexFunction(Op.Product, 1, 255);
        public static readonly FreeRefFunction IMSIN = new ComplexFunction(Op.Sin, 1, 1);
        public static readonly FreeRefFunction IMSQRT = new ComplexFunction(Op.Sqrt, 1, 1);
        public static readonly FreeRefFunction IMSUB = new ComplexFunction(Op.Sub, 2, 2);
        public static readonly FreeRefFunction IMSUM = new ComplexFunction(Op.Sum, 1, 255);

        private readonly Op _op;
        private readonly int _min;
        private readonly int _max;

        private ComplexFunction(Op op, int min, int max)
        {
            _op = op;
            _min = min;
            _max = max;
        }

        public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
        {
            if(args.Length < _min || args.Length > _max)
                return ErrorEval.VALUE_INVALID;
            try
            {
                return Calc(args, ec.RowIndex, ec.ColumnIndex);
            }
            catch(EvaluationException e)
            {
                return e.GetErrorEval();
            }
        }

        private static EvaluationException NumErr()
        {
            return new EvaluationException(ErrorEval.NUM_ERROR);
        }

        private static bool TryParseDouble(string s, out double d)
        {
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)
                && !double.IsNaN(d) && !double.IsInfinity(d);
        }

        private static double ParseCoefficient(string s)
        {
            if(s == "" || s == "+")
                return 1;
            if(s == "-")
                return -1;
            if(!TryParseDouble(s, out double d))
                throw NumErr();
            return d;
        }

        /// <summary>Parses "x", "yi", "x+yi" (i or j suffix); suffix is reported through <paramref name="suffix"/>.</summary>
        private static SysComplex Parse(string text, ref char suffix)
        {
            string s = text.Trim();
            if(s.Length == 0)
                return SysComplex.Zero;
            char last = s[s.Length - 1];
            if(last != 'i' && last != 'j')
            {
                if(!TryParseDouble(s, out double real))
                    throw NumErr();
                return new SysComplex(real, 0);
            }
            if(suffix != '\0' && suffix != last)
                throw new EvaluationException(ErrorEval.VALUE_INVALID);
            suffix = last;
            string body = s.Substring(0, s.Length - 1);
            int split = -1;
            for(int k = body.Length - 1; k > 0; k--)
            {
                if((body[k] == '+' || body[k] == '-') && body[k - 1] != 'E' && body[k - 1] != 'e')
                {
                    split = k;
                    break;
                }
            }
            if(split < 0)
                return new SysComplex(0, ParseCoefficient(body));
            if(!TryParseDouble(body.Substring(0, split), out double re))
                throw NumErr();
            return new SysComplex(re, ParseCoefficient(body.Substring(split)));
        }

        private static string Num(double d)
        {
            return d.ToString("G15", CultureInfo.InvariantCulture);
        }

        private static string Format(SysComplex z, char suffix)
        {
            if(double.IsNaN(z.Real) || double.IsInfinity(z.Real) || double.IsNaN(z.Imaginary) || double.IsInfinity(z.Imaginary))
                throw NumErr();
            char u = suffix == '\0' ? 'i' : suffix;
            // floating-point noise relative to the magnitude counts as zero
            double scale = Math.Max(Math.Abs(z.Real), Math.Abs(z.Imaginary));
            double re = Math.Abs(z.Real) < scale * 1e-15 ? 0 : z.Real;
            double im = Math.Abs(z.Imaginary) < scale * 1e-15 ? 0 : z.Imaginary;
            string r = Num(re);
            string i = Num(im);
            if(im == 0)
                return r;
            string imPart = i == "1" ? "" : i == "-1" ? "-" : i;
            if(re == 0)
                return imPart + u;
            return r + (im > 0 ? "+" : "") + imPart + u;
        }

        private static SysComplex ParseArg(ValueEval arg, int row, int col, ref char suffix)
        {
            ValueEval v = OperandResolver.GetSingleValue(arg, row, col);
            return Parse(OperandResolver.CoerceValueToString(v), ref suffix);
        }

        private static List<SysComplex> ParseAll(ValueEval[] args, int row, int col, ref char suffix)
        {
            var list = new List<SysComplex>();
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
                            if(v is BlankEval)
                                continue;
                            list.Add(Parse(OperandResolver.CoerceValueToString(v), ref suffix));
                        }
                    }
                }
                else
                {
                    list.Add(ParseArg(arg, row, col, ref suffix));
                }
            }
            return list;
        }

        private ValueEval Calc(ValueEval[] args, int row, int col)
        {
            char sfx = '\0';
            if(_op == Op.Product || _op == Op.Sum)
            {
                SysComplex acc = _op == Op.Product ? SysComplex.One : SysComplex.Zero;
                foreach(SysComplex z in ParseAll(args, row, col, ref sfx))
                    acc = _op == Op.Product ? acc * z : acc + z;
                return new StringEval(Format(acc, sfx));
            }

            SysComplex a = ParseArg(args[0], row, col, ref sfx);
            SysComplex result;
            switch(_op)
            {
                case Op.Abs:
                    return new NumberEval(SysComplex.Abs(a));
                case Op.Argument:
                    if(a == SysComplex.Zero)
                        return ErrorEval.DIV_ZERO;
                    return new NumberEval(Math.Atan2(a.Imaginary, a.Real));
                case Op.Conjugate:
                    result = SysComplex.Conjugate(a);
                    break;
                case Op.Cos:
                    result = SysComplex.Cos(a);
                    break;
                case Op.Sin:
                    result = SysComplex.Sin(a);
                    break;
                case Op.Exp:
                    result = SysComplex.Exp(a);
                    break;
                case Op.Sqrt:
                    result = SysComplex.Sqrt(a);
                    break;
                case Op.Ln:
                case Op.Log10:
                case Op.Log2:
                    if(a == SysComplex.Zero)
                        return ErrorEval.NUM_ERROR;
                    result = SysComplex.Log(a);
                    if(_op == Op.Log10)
                        result /= Math.Log(10);
                    else if(_op == Op.Log2)
                        result /= Math.Log(2);
                    break;
                case Op.Div:
                {
                    SysComplex b = ParseArg(args[1], row, col, ref sfx);
                    if(b == SysComplex.Zero)
                        return ErrorEval.NUM_ERROR;
                    result = a / b;
                    break;
                }
                case Op.Sub:
                    result = a - ParseArg(args[1], row, col, ref sfx);
                    break;
                case Op.Power:
                {
                    double n = NumericArgs.Scalar(args[1], row, col);
                    if(a == SysComplex.Zero)
                    {
                        if(n <= 0)
                            return ErrorEval.NUM_ERROR;
                        result = SysComplex.Zero;
                    }
                    else
                    {
                        if(n == Math.Truncate(n) && Math.Abs(n) <= 64)
                        {
                            result = SysComplex.One;
                            for(int k = 0; k < (int) Math.Abs(n); k++)
                                result *= a;
                            if(n < 0)
                                result = SysComplex.One / result;
                        }
                        else
                        {
                            result = SysComplex.Pow(a, n);
                        }
                    }
                    break;
                }
                default:
                    return ErrorEval.VALUE_INVALID;
            }
            return new StringEval(Format(result, sfx));
        }
    }
}