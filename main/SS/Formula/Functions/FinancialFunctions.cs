using NPOI.SS.Formula.Eval;
using System;
using System.Collections.Generic;

namespace NPOI.SS.Formula.Functions
{
    /// <summary>Date, day-count and coupon-schedule helpers shared by the financial functions.</summary>
    internal static class FinUtil
    {
        private static readonly DateTime Epoch = new DateTime(1899, 12, 30);

        public static DateTime ToDate(double serial)
        {
            double s = Math.Floor(serial);
            if(s < 0 || s > 2958465)
                throw new EvaluationException(ErrorEval.NUM_ERROR);
            return Epoch.AddDays(s);
        }

        public static double ToSerial(DateTime d)
        {
            return (d - Epoch).TotalDays;
        }

        public static int Basis(ValueEval[] args, int idx, int row, int col)
        {
            if(args.Length <= idx || args[idx] is MissingArgEval)
                return 0;
            int b = (int) Math.Truncate(NumericArgs.Scalar(args[idx], row, col));
            if(b < 0 || b > 4)
                throw new EvaluationException(ErrorEval.NUM_ERROR);
            return b;
        }

        public static int Frequency(double f)
        {
            int n = (int) Math.Truncate(f);
            if(n != 1 && n != 2 && n != 4)
                throw new EvaluationException(ErrorEval.NUM_ERROR);
            return n;
        }

        private static bool IsLastDayOfFeb(DateTime d)
        {
            return d.Month == 2 && d.Day == DateTime.DaysInMonth(d.Year, 2);
        }

        /// <summary>30/360 day count; european=false is the US (NASD) method.</summary>
        public static double Days360(DateTime a, DateTime b, bool european)
        {
            int d1 = a.Day, d2 = b.Day;
            if(european)
            {
                if(d1 == 31)
                    d1 = 30;
                if(d2 == 31)
                    d2 = 30;
            }
            else
            {
                if(IsLastDayOfFeb(a) && IsLastDayOfFeb(b))
                    d2 = 30;
                if(IsLastDayOfFeb(a))
                    d1 = 30;
                if(d2 == 31 && d1 >= 30)
                    d2 = 30;
                if(d1 == 31)
                    d1 = 30;
            }
            return (b.Year - a.Year) * 360 + (b.Month - a.Month) * 30 + (d2 - d1);
        }

        /// <summary>Days between two dates according to basis (0/4: 30/360, others actual).</summary>
        public static double Days(DateTime a, DateTime b, int basis)
        {
            if(basis == 0)
                return Days360(a, b, false);
            if(basis == 4)
                return Days360(a, b, true);
            return (b - a).TotalDays;
        }

        private static bool ContainsFeb29(DateTime a, DateTime b)
        {
            for(int y = a.Year; y <= b.Year; y++)
            {
                if(DateTime.IsLeapYear(y))
                {
                    DateTime f = new DateTime(y, 2, 29);
                    if(f >= a && f <= b)
                        return true;
                }
            }
            return false;
        }

        /// <summary>Excel YEARFRAC (a before b).</summary>
        public static double YearFrac(DateTime a, DateTime b, int basis)
        {
            switch(basis)
            {
                case 0:
                    return Days360(a, b, false) / 360.0;
                case 4:
                    return Days360(a, b, true) / 360.0;
                case 2:
                    return (b - a).TotalDays / 360.0;
                case 3:
                    return (b - a).TotalDays / 365.0;
                default:
                    double days = (b - a).TotalDays;
                    double year;
                    if(a.Year == b.Year)
                        year = DateTime.IsLeapYear(a.Year) ? 366 : 365;
                    else if(b <= a.AddYears(1))
                        year = ContainsFeb29(a, b) ? 366 : 365;
                    else
                    {
                        double sum = 0;
                        for(int y = a.Year; y <= b.Year; y++)
                            sum += DateTime.IsLeapYear(y) ? 366 : 365;
                        year = sum / (b.Year - a.Year + 1);
                    }
                    return days / year;
            }
        }

        /// <summary>Coupon date k periods before maturity (k=0 is maturity).</summary>
        public static DateTime CouponDate(DateTime maturity, int freq, int k)
        {
            int months = -(12 / freq) * k;
            DateTime d = maturity.AddMonths(months);
            if(maturity.Day == DateTime.DaysInMonth(maturity.Year, maturity.Month))
                d = new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));
            return d;
        }

        /// <summary>Coupon schedule data around a settlement date.</summary>
        public sealed class Coupon
        {
            public DateTime Pcd, Ncd;
            public int Num;
            public double DayBs, DaySnc, Days;
        }

        public static Coupon Schedule(DateTime settle, DateTime maturity, int freq, int basis)
        {
            if(settle >= maturity)
                throw new EvaluationException(ErrorEval.NUM_ERROR);
            int k = 1;
            while(CouponDate(maturity, freq, k) > settle)
                k++;
            Coupon c = new Coupon();
            c.Pcd = CouponDate(maturity, freq, k);
            c.Ncd = CouponDate(maturity, freq, k - 1);
            c.Num = k;
            if(basis == 1)
                c.Days = (c.Ncd - c.Pcd).TotalDays;
            else if(basis == 3)
                c.Days = 365.0 / freq;
            else
                c.Days = 360.0 / freq;
            c.DayBs = Days(c.Pcd, settle, basis);
            c.DaySnc = (basis == 0) ? c.Days - c.DayBs : Days(settle, c.Ncd, basis);
            return c;
        }

        /// <summary>Reads a numeric array/range/scalar argument into an array (non-numeric cells are errors).</summary>
        public static double[] ValuesOf(ValueEval arg, int row, int col)
        {
            List<double> r = new List<double>();
            if(arg is TwoDEval area)
            {
                for(int i = 0; i < area.Height; i++)
                {
                    for(int j = 0; j < area.Width; j++)
                    {
                        ValueEval v = area.GetValue(i, j);
                        if(v is ErrorEval e)
                            throw new EvaluationException(e);
                        if(v is NumberEval n)
                            r.Add(n.NumberValue);
                        else
                            throw new EvaluationException(ErrorEval.VALUE_INVALID);
                    }
                }
            }
            else
            {
                r.Add(NumericArgs.Scalar(arg, row, col));
            }
            return r.ToArray();
        }

        /// <summary>Price per 100 face of a coupon bond (Excel PRICE), without validation.</summary>
        public static double BondPrice(Coupon c, int freq, double rate, double yld, double redemption)
        {
            double cpn = 100 * rate / freq;
            double accrued = cpn * c.DayBs / c.Days;
            double dsc = c.DaySnc / c.Days;
            if(c.Num == 1)
                return (redemption + cpn) / (1 + dsc * yld / freq) - accrued;
            double yf = 1 + yld / freq;
            double p = redemption / Math.Pow(yf, c.Num - 1 + dsc);
            for(int k = 1; k <= c.Num; k++)
                p += cpn / Math.Pow(yf, k - 1 + dsc);
            return p - accrued;
        }

        public static double Macaulay(Coupon c, int freq, double rate, double yld, double redemption)
        {
            double cpn = 100 * rate / freq;
            double dsc = c.DaySnc / c.Days;
            double yf = 1 + yld / freq;
            double num = 0, den = 0;
            for(int k = 1; k <= c.Num; k++)
            {
                double t = k - 1 + dsc;
                double cf = cpn + (k == c.Num ? redemption : 0);
                double pv = cf / Math.Pow(yf, t);
                num += t * pv;
                den += pv;
            }
            return num / den / freq;
        }
    }

    /// <summary>Base for built-in (non-ATP) numeric functions with a fixed argument-count range.</summary>
    public abstract class FinFunction : Function
    {
        private readonly int _min, _max;
        protected FinFunction(int min, int max) { _min = min; _max = max; }
        protected abstract double Calc(ValueEval[] a, int row, int col);

        public ValueEval Evaluate(ValueEval[] args, int srcRow, int srcCol)
        {
            if(args.Length < _min || args.Length > _max)
                return ErrorEval.VALUE_INVALID;
            try
            {
                double d = Calc(args, srcRow, srcCol);
                if(double.IsNaN(d) || double.IsInfinity(d))
                    return ErrorEval.NUM_ERROR;
                return new NumberEval(d);
            }
            catch(EvaluationException e)
            {
                return e.GetErrorEval();
            }
        }

        protected static double N(ValueEval[] a, int i, int row, int col)
        {
            return NumericArgs.Scalar(a[i], row, col);
        }

        protected static double N(ValueEval[] a, int i, int row, int col, double dflt)
        {
            if(a.Length <= i || a[i] is MissingArgEval)
                return dflt;
            return NumericArgs.Scalar(a[i], row, col);
        }
    }

    /// <summary>Excel SLN(cost, salvage, life)</summary>
    public class Sln : FinFunction
    {
        public static readonly Function instance = new Sln();
        private Sln() : base(3, 3) { }
        protected override double Calc(ValueEval[] a, int row, int col)
        {
            double life = N(a, 2, row, col);
            if(life == 0)
                throw new EvaluationException(ErrorEval.DIV_ZERO);
            return (N(a, 0, row, col) - N(a, 1, row, col)) / life;
        }
    }

    /// <summary>Excel SYD(cost, salvage, life, per)</summary>
    public class Syd : FinFunction
    {
        public static readonly Function instance = new Syd();
        private Syd() : base(4, 4) { }
        protected override double Calc(ValueEval[] a, int row, int col)
        {
            double cost = N(a, 0, row, col), salvage = N(a, 1, row, col);
            double life = N(a, 2, row, col), per = N(a, 3, row, col);
            if(life <= 0 || per <= 0 || per > life)
                throw new EvaluationException(ErrorEval.NUM_ERROR);
            return (cost - salvage) * (life - per + 1) * 2 / (life * (life + 1));
        }
    }

    /// <summary>Excel DB(cost, salvage, life, period, [month])</summary>
    public class Db : FinFunction
    {
        public static readonly Function instance = new Db();
        private Db() : base(4, 5) { }
        protected override double Calc(ValueEval[] a, int row, int col)
        {
            double cost = N(a, 0, row, col), salvage = N(a, 1, row, col);
            double life = Math.Truncate(N(a, 2, row, col));
            int period = (int) Math.Truncate(N(a, 3, row, col));
            int month = (int) Math.Truncate(N(a, 4, row, col, 12));
            if(cost < 0 || salvage < 0 || salvage > cost || life <= 0 || life > 1e6 || period <= 0 || month < 1 || month > 12
                || period > life + (month < 12 ? 1 : 0))
                throw new EvaluationException(ErrorEval.NUM_ERROR);
            if(cost == 0)
                return 0;
            double rate = Math.Round(1 - Math.Pow(salvage / cost, 1 / life), 3, MidpointRounding.AwayFromZero);
            double total = 0, dep = 0;
            for(int p = 1; p <= period; p++)
            {
                if(p == 1)
                    dep = cost * rate * month / 12;
                else if(p == life + 1)
                    dep = (cost - total) * rate * (12 - month) / 12;
                else
                    dep = (cost - total) * rate;
                if(p < period)
                    total += dep;
            }
            return dep;
        }
    }

    /// <summary>Excel DDB(cost, salvage, life, period, [factor])</summary>
    public class Ddb : FinFunction
    {
        public static readonly Function instance = new Ddb();
        private Ddb() : base(4, 5) { }
        protected override double Calc(ValueEval[] a, int row, int col)
        {
            double cost = N(a, 0, row, col), salvage = N(a, 1, row, col);
            double life = N(a, 2, row, col), period = N(a, 3, row, col);
            double factor = N(a, 4, row, col, 2);
            if(cost < 0 || salvage < 0 || life <= 0 || period <= 0 || factor <= 0 || period > life)
                throw new EvaluationException(ErrorEval.NUM_ERROR);
            return Vdb.Depreciation(cost, salvage, life, period - 1, period, factor, true);
        }
    }

    /// <summary>Excel VDB(cost, salvage, life, start_period, end_period, [factor], [no_switch])</summary>
    public class Vdb : FinFunction
    {
        public static readonly Function instance = new Vdb();
        private Vdb() : base(5, 7) { }

        /// <summary>Depreciation over [start,end]; per-period amounts are weighted by overlap for fractional bounds.</summary>
        internal static double Depreciation(double cost, double salvage, double life, double start, double end,
            double factor, bool noSwitch)
        {
            double total = 0, result = 0;
            int n = (int) Math.Ceiling(end);
            for(int p = 0; p < n; p++)
            {
                double dep = (cost - total) * factor / life;
                double remaining = cost - total - salvage;
                if(!noSwitch && p < life)
                {
                    double sl = remaining / (life - p);
                    if(sl > dep)
                        dep = sl;
                }
                if(dep > remaining)
                    dep = remaining;
                if(dep < 0)
                    dep = 0;
                double overlap = Math.Min(end, p + 1) - Math.Max(start, p);
                if(overlap > 0)
                    result += dep * overlap;
                total += dep;
            }
            return result;
        }

        protected override double Calc(ValueEval[] a, int row, int col)
        {
            double cost = N(a, 0, row, col), salvage = N(a, 1, row, col);
            double life = N(a, 2, row, col), start = N(a, 3, row, col), end = N(a, 4, row, col);
            double factor = N(a, 5, row, col, 2);
            bool noSwitch = false;
            if(a.Length > 6 && !(a[6] is MissingArgEval))
                noSwitch = OperandResolver.CoerceValueToBoolean(
                    OperandResolver.GetSingleValue(a[6], row, col), false) == true;
            if(cost < 0 || salvage < 0 || life <= 0 || start < 0 || end < start || end > life || factor < 0)
                throw new EvaluationException(ErrorEval.NUM_ERROR);
            return Depreciation(cost, salvage, life, start, end, factor, noSwitch);
        }
    }

    // ---- Analysis ToolPak functions ----

    /// <summary>Excel DISC / PRICEDISC / YIELDDISC / INTRATE / RECEIVED / ACCRINTM (single cash-flow securities).</summary>
    public class DiscountSecurity : SimpleNumericFunction
    {
        private readonly int _kind;
        public static readonly FreeRefFunction DISC = new DiscountSecurity(0);
        public static readonly FreeRefFunction PRICEDISC = new DiscountSecurity(1);
        public static readonly FreeRefFunction YIELDDISC = new DiscountSecurity(2);
        public static readonly FreeRefFunction INTRATE = new DiscountSecurity(3);
        public static readonly FreeRefFunction RECEIVED = new DiscountSecurity(4);
        public static readonly FreeRefFunction ACCRINTM = new DiscountSecurity(5);

        private DiscountSecurity(int kind) : base(4, 5) { _kind = kind; }

        protected override ValueEval Calc(ValueEval[] a, int row, int col)
        {
            DateTime s = FinUtil.ToDate(NumericArgs.Scalar(a[0], row, col));
            DateTime m = FinUtil.ToDate(NumericArgs.Scalar(a[1], row, col));
            double x = NumericArgs.Scalar(a[2], row, col);
            double y = NumericArgs.Scalar(a[3], row, col);
            int basis = FinUtil.Basis(a, 4, row, col);
            if(s >= m || x <= 0 || y <= 0)
                return ErrorEval.NUM_ERROR;
            double yf = FinUtil.YearFrac(s, m, basis);
            switch(_kind)
            {
                case 0:
                    return Num((y - x) / y / yf);       // DISC(pr, redemption)
                case 1:
                    return Num(y - x * y * yf);         // PRICEDISC(discount, redemption)
                case 2:
                    return Num((y - x) / x / yf);       // YIELDDISC(pr, redemption)
                case 3:
                    return Num((y - x) / x / yf);       // INTRATE(investment, redemption)
                case 4:
                    if(1 - y * yf <= 0)
                        return ErrorEval.NUM_ERROR;
                    return Num(x / (1 - y * yf));       // RECEIVED(investment, discount)
                default:
                    return Num(y * x * yf);            // ACCRINTM(issue, settlement, rate, par)
            }
        }
    }

    /// <summary>Excel TBILLPRICE / TBILLYIELD / TBILLEQ.</summary>
    public class TBill : SimpleNumericFunction
    {
        private readonly int _kind;
        public static readonly FreeRefFunction PRICE = new TBill(0);
        public static readonly FreeRefFunction YIELD = new TBill(1);
        public static readonly FreeRefFunction EQ = new TBill(2);
        private TBill(int kind) : base(3, 3) { _kind = kind; }

        protected override ValueEval Calc(ValueEval[] a, int row, int col)
        {
            DateTime s = FinUtil.ToDate(NumericArgs.Scalar(a[0], row, col));
            DateTime m = FinUtil.ToDate(NumericArgs.Scalar(a[1], row, col));
            double x = NumericArgs.Scalar(a[2], row, col);
            double dsm = (m - s).TotalDays;
            if(x <= 0 || s >= m || m > s.AddYears(1))
                return ErrorEval.NUM_ERROR;
            switch(_kind)
            {
                case 0:
                    return Num(100 * (1 - x * dsm / 360));
                case 1:
                    return Num((100 - x) / x * 360 / dsm);
                default:
                    if(dsm <= 182)
                        return Num(365 * x / (360 - x * dsm));
                    double p = 100 - x * 100 * dsm / 360;
                    double qa = dsm / 730.0 - 0.25, qb = dsm / 365.0, qc = (p - 100) / p;
                    return Num((-qb + Math.Sqrt(qb * qb - 4 * qa * qc)) / (2 * qa));
            }
        }
    }

    /// <summary>Excel FVSCHEDULE(principal, schedule)</summary>
    public class FvSchedule : SimpleNumericFunction
    {
        public static readonly FreeRefFunction instance = new FvSchedule();
        private FvSchedule() : base(2, 2) { }
        protected override ValueEval Calc(ValueEval[] a, int row, int col)
        {
            double p = NumericArgs.Scalar(a[0], row, col);
            foreach(double r in FinUtil.ValuesOf(a[1], row, col))
                p *= 1 + r;
            return Num(p);
        }
    }

    /// <summary>Excel CUMIPMT / CUMPRINC (rate, nper, pv, start_period, end_period, type)</summary>
    public class CumulativePayment : SimpleNumericFunction
    {
        private readonly bool _interest;
        public static readonly FreeRefFunction CUMIPMT = new CumulativePayment(true);
        public static readonly FreeRefFunction CUMPRINC = new CumulativePayment(false);
        private CumulativePayment(bool interest) : base(6, 6) { _interest = interest; }

        protected override ValueEval Calc(ValueEval[] a, int row, int col)
        {
            double rate = NumericArgs.Scalar(a[0], row, col);
            double nper = NumericArgs.Scalar(a[1], row, col);
            double pv = NumericArgs.Scalar(a[2], row, col);
            int start = (int) Math.Truncate(NumericArgs.Scalar(a[3], row, col));
            int end = (int) Math.Truncate(NumericArgs.Scalar(a[4], row, col));
            int type = (int) Math.Truncate(NumericArgs.Scalar(a[5], row, col));
            if(rate <= 0 || nper <= 0 || nper > 1e7 || pv <= 0 || start < 1 || end < start || end > nper
                || (type != 0 && type != 1))
                return ErrorEval.NUM_ERROR;
            double growth = Math.Pow(1 + rate, nper);
            double pmt = pv * rate * growth / (growth - 1);   // type-0 payment
            double sumInterest = 0;
            for(int k = start; k <= end; k++)
                sumInterest += Interest(rate, pv, pmt, k, type);
            double payment = type == 1 ? pmt / (1 + rate) : pmt;
            double total = _interest ? sumInterest : payment * (end - start + 1) - sumInterest;
            return Num(-total);
        }

        // Interest portion of payment k (positive), type 0 or 1.
        private static double Interest(double r, double pv, double pmt, int k, int type)
        {
            if(type == 1)
            {
                if(k == 1)
                    return 0;
                return Interest(r, pv, pmt, k, 0) / (1 + r);
            }
            double bal = pv * Math.Pow(1 + r, k - 1) - pmt * (Math.Pow(1 + r, k - 1) - 1) / r;
            return bal * r;
        }
    }

    /// <summary>Excel XNPV(rate, values, dates) and XIRR(values, dates, [guess])</summary>
    public class XnpvXirr : SimpleNumericFunction
    {
        private readonly bool _irr;
        public static readonly FreeRefFunction XNPV = new XnpvXirr(false);
        public static readonly FreeRefFunction XIRR = new XnpvXirr(true);
        private XnpvXirr(bool irr) : base(irr ? 2 : 3, 3) { _irr = irr; }

        private static double Npv(double rate, double[] v, double[] d)
        {
            double s = 0;
            for(int i = 0; i < v.Length; i++)
                s += v[i] / Math.Pow(1 + rate, (d[i] - d[0]) / 365.0);
            return s;
        }

        protected override ValueEval Calc(ValueEval[] a, int row, int col)
        {
            double[] v, d;
            double rate;
            if(_irr)
            {
                v = FinUtil.ValuesOf(a[0], row, col);
                d = FinUtil.ValuesOf(a[1], row, col);
                rate = a.Length > 2 && !(a[2] is MissingArgEval) ? NumericArgs.Scalar(a[2], row, col) : 0.1;
            }
            else
            {
                rate = NumericArgs.Scalar(a[0], row, col);
                v = FinUtil.ValuesOf(a[1], row, col);
                d = FinUtil.ValuesOf(a[2], row, col);
            }
            if(v.Length != d.Length || v.Length < 2)
                return ErrorEval.NUM_ERROR;
            for(int i = 0; i < d.Length; i++)
            {
                d[i] = Math.Truncate(d[i]);
                if(d[i] < d[0])
                    return ErrorEval.NUM_ERROR;
            }
            if(!_irr)
            {
                if(rate <= -1)
                    return ErrorEval.NUM_ERROR;
                return Num(Npv(rate, v, d));
            }
            bool pos = false, neg = false;
            foreach(double x in v)
            { if(x > 0) pos = true; if(x < 0) neg = true; }
            if(!pos || !neg || rate <= -1)
                return ErrorEval.NUM_ERROR;
            double r = rate;
            for(int it = 0; it < 100; it++)
            {
                double f = Npv(r, v, d);
                double h = 1e-7;
                double df = (Npv(r + h, v, d) - f) / h;
                if(df == 0 || double.IsNaN(df))
                    break;
                double next = r - f / df;
                if(next <= -1)
                    next = (r - 1) / 2;
                if(Math.Abs(next - r) < 1e-10)
                    return Num(next);
                r = next;
            }
            double lo = -0.999999, hi = 1e6;
            double flo = Npv(lo, v, d), fhi = Npv(hi, v, d);
            if(double.IsNaN(flo) || double.IsNaN(fhi) || double.IsInfinity(flo) || double.IsInfinity(fhi) || flo * fhi > 0)
                return ErrorEval.NUM_ERROR;
            for(int it = 0; it < 300; it++)
            {
                double mid = (lo + hi) / 2, fm = Npv(mid, v, d);
                if(fm == 0)
                    return Num(mid);
                if(flo * fm < 0)
                { hi = mid; }
                else
                { lo = mid; flo = fm; }
            }
            return Num((lo + hi) / 2);
        }
    }

    /// <summary>Excel COUPDAYBS, COUPDAYS, COUPDAYSNC, COUPNCD, COUPNUM, COUPPCD (settlement, maturity, frequency, [basis]).</summary>
    public class CouponFunction : SimpleNumericFunction
    {
        private readonly int _kind;
        public static readonly FreeRefFunction COUPDAYBS = new CouponFunction(0);
        public static readonly FreeRefFunction COUPDAYS = new CouponFunction(1);
        public static readonly FreeRefFunction COUPDAYSNC = new CouponFunction(2);
        public static readonly FreeRefFunction COUPNCD = new CouponFunction(3);
        public static readonly FreeRefFunction COUPNUM = new CouponFunction(4);
        public static readonly FreeRefFunction COUPPCD = new CouponFunction(5);
        private CouponFunction(int kind) : base(3, 4) { _kind = kind; }

        protected override ValueEval Calc(ValueEval[] a, int row, int col)
        {
            DateTime s = FinUtil.ToDate(NumericArgs.Scalar(a[0], row, col));
            DateTime m = FinUtil.ToDate(NumericArgs.Scalar(a[1], row, col));
            int freq = FinUtil.Frequency(NumericArgs.Scalar(a[2], row, col));
            int basis = FinUtil.Basis(a, 3, row, col);
            FinUtil.Coupon c = FinUtil.Schedule(s, m, freq, basis);
            switch(_kind)
            {
                case 0:
                    return Num(c.DayBs);
                case 1:
                    return Num(c.Days);
                case 2:
                    return Num(c.DaySnc);
                case 3:
                    return Num(FinUtil.ToSerial(c.Ncd));
                case 4:
                    return Num(c.Num);
                default:
                    return Num(FinUtil.ToSerial(c.Pcd));
            }
        }
    }

    /// <summary>Excel PRICE / YIELD / DURATION / MDURATION.</summary>
    public class BondFunction : SimpleNumericFunction
    {
        private readonly int _kind;
        public static readonly FreeRefFunction PRICE = new BondFunction(0, 6, 7);
        public static readonly FreeRefFunction YIELD = new BondFunction(1, 6, 7);
        public static readonly FreeRefFunction DURATION = new BondFunction(2, 5, 6);
        public static readonly FreeRefFunction MDURATION = new BondFunction(3, 5, 6);
        private BondFunction(int kind, int min, int max) : base(min, max) { _kind = kind; }

        protected override ValueEval Calc(ValueEval[] a, int row, int col)
        {
            DateTime s = FinUtil.ToDate(NumericArgs.Scalar(a[0], row, col));
            DateTime m = FinUtil.ToDate(NumericArgs.Scalar(a[1], row, col));
            double rate = NumericArgs.Scalar(a[2], row, col);
            double v = NumericArgs.Scalar(a[3], row, col);   // yield, or price for YIELD
            double redemption = 100;
            int freq, basis;
            if(_kind <= 1)
            {
                redemption = NumericArgs.Scalar(a[4], row, col);
                freq = FinUtil.Frequency(NumericArgs.Scalar(a[5], row, col));
                basis = FinUtil.Basis(a, 6, row, col);
            }
            else
            {
                freq = FinUtil.Frequency(NumericArgs.Scalar(a[4], row, col));
                basis = FinUtil.Basis(a, 5, row, col);
            }
            if(rate < 0 || redemption <= 0 || (_kind == 1 ? v <= 0 : v < 0))
                return ErrorEval.NUM_ERROR;
            FinUtil.Coupon c = FinUtil.Schedule(s, m, freq, basis);
            switch(_kind)
            {
                case 0:
                    return Num(FinUtil.BondPrice(c, freq, rate, v, redemption));
                case 2:
                    return Num(FinUtil.Macaulay(c, freq, rate, v, redemption));
                case 3:
                    return Num(FinUtil.Macaulay(c, freq, rate, v, redemption) / (1 + v / freq));
                default:
                    return Num(SolveYield(c, freq, rate, v, redemption));
            }
        }

        private static double SolveYield(FinUtil.Coupon c, int freq, double rate, double price, double redemption)
        {
            // Price is strictly decreasing in yield: bracket then bisect.
            double lo = -freq + 1e-9, hi = 1;
            while(FinUtil.BondPrice(c, freq, rate, hi, redemption) > price && hi < 1e6)
                hi *= 2;
            for(int i = 0; i < 300; i++)
            {
                double mid = (lo + hi) / 2;
                if(FinUtil.BondPrice(c, freq, rate, mid, redemption) > price)
                    lo = mid;
                else
                    hi = mid;
            }
            return (lo + hi) / 2;
        }
    }
}