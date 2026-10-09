using NPOI.SS.Formula.Eval;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NPOI.SS.Formula.Atp
{
    /// <summary>
    /// The Excel 365 dynamic-array functions FILTER, SORT, UNIQUE and SEQUENCE. Each returns an
    /// array (<see cref="CacheAreaEval"/>); a dynamic-array formula spills it into neighbouring
    /// cells, any other formula reduces it to one value as for other array results.
    /// </summary>
    public static class DynamicArrayFunctions
    {
        private const int MaxRows = 1048576;
        private const int MaxColumns = 16384;
        // NPOI holds the whole result in memory; Excel's own limit is the sheet size
        private const long MaxCells = 10_000_000;

        public static readonly FreeRefFunction FILTER = new Function(Filter);
        public static readonly FreeRefFunction SORT = new Function(Sort);
        public static readonly FreeRefFunction UNIQUE = new Function(Unique);
        public static readonly FreeRefFunction SEQUENCE = new Function(Sequence);

        private sealed class Function : FreeRefFunction
        {
            private readonly Func<ValueEval[], OperationEvaluationContext, ValueEval> _impl;

            public Function(Func<ValueEval[], OperationEvaluationContext, ValueEval> impl)
            {
                _impl = impl;
            }

            public ValueEval Evaluate(ValueEval[] args, OperationEvaluationContext ec)
            {
                try
                {
                    return _impl(args, ec);
                }
                catch(EvaluationException e)
                {
                    return e.GetErrorEval();
                }
            }
        }

        // FILTER(array, include, [if_empty])
        private static ValueEval Filter(ValueEval[] args, OperationEvaluationContext ec)
        {
            if(args.Length < 2 || args.Length > 3)
            {
                return ErrorEval.VALUE_INVALID;
            }
            ValueEval[][] grid = ToGrid(args[0]);
            ValueEval[][] include = ToGrid(args[1]);
            int height = grid.Length;
            int width = grid[0].Length;
            bool byRow;
            if(include[0].Length == 1 && include.Length == height)
            {
                byRow = true;
            }
            else if(include.Length == 1 && include[0].Length == width)
            {
                byRow = false;
            }
            else
            {
                return ErrorEval.VALUE_INVALID;
            }

            int count = byRow ? height : width;
            var keep = new List<int>();
            for(int i = 0; i < count; i++)
            {
                if(IsIncluded(byRow ? include[i][0] : include[0][i]))
                {
                    keep.Add(i);
                }
            }

            if(keep.Count == 0)
            {
                if(args.Length == 3 && args[2] is not MissingArgEval)
                {
                    return args[2] is RefEval re ? re.GetInnerValueEval(re.FirstSheetIndex) : args[2];
                }
                return ErrorEval.CALC;
            }

            ValueEval[][] result = byRow
                ? keep.Select(r => grid[r]).ToArray()
                : grid.Select(row => keep.Select(c => row[c]).ToArray()).ToArray();
            return ToArray(result, ec);
        }

        private static bool IsIncluded(ValueEval value)
        {
            switch(value)
            {
                case ErrorEval error:
                    throw new EvaluationException(error);
                case BoolEval b:
                    return b.BooleanValue;
                case NumericValueEval n:
                    return n.NumberValue != 0;
                case BlankEval:
                    return false;
                default:
                    throw new EvaluationException(ErrorEval.VALUE_INVALID);
            }
        }

        // SORT(array, [sort_index], [sort_order], [by_col])
        private static ValueEval Sort(ValueEval[] args, OperationEvaluationContext ec)
        {
            if(args.Length < 1 || args.Length > 4)
            {
                return ErrorEval.VALUE_INVALID;
            }
            ValueEval[][] grid = ToGrid(args[0]);
            bool byCol = args.Length > 3 && BoolArg(args[3], ec);
            if(byCol)
            {
                grid = Transpose(grid);
            }
            int width = grid[0].Length;

            int[] indexes = args.Length > 1 && args[1] is not MissingArgEval
                ? ToGrid(args[1]).SelectMany(r => r).Select(v => ToInt(v)).ToArray()
                : new[] { 1 };
            int[] orders = args.Length > 2 && args[2] is not MissingArgEval
                ? ToGrid(args[2]).SelectMany(r => r).Select(v => ToInt(v)).ToArray()
                : new[] { 1 };
            if(orders.Length != 1 && orders.Length != indexes.Length)
            {
                return ErrorEval.VALUE_INVALID;
            }
            if(indexes.Any(i => i < 1 || i > width) || orders.Any(o => o != 1 && o != -1))
            {
                return ErrorEval.VALUE_INVALID;
            }

            // stable: ties keep their original order
            ValueEval[][] sorted = Enumerable.Range(0, grid.Length)
                .OrderBy(r => r, Comparer<int>.Create((a, b) =>
                {
                    for(int k = 0; k < indexes.Length; k++)
                    {
                        int order = orders.Length == 1 ? orders[0] : orders[k];
                        int c = CompareForSort(grid[a][indexes[k] - 1], grid[b][indexes[k] - 1], order);
                        if(c != 0)
                        {
                            return c;
                        }
                    }
                    return a.CompareTo(b);
                }))
                .Select(r => grid[r])
                .ToArray();
            return ToArray(byCol ? Transpose(sorted) : sorted, ec);
        }

        // Excel's ascending order: numbers, text (case-insensitive), FALSE, TRUE, errors;
        // descending reverses it. Blanks sort last either way.
        private static int CompareForSort(ValueEval a, ValueEval b, int order)
        {
            int ra = SortRank(a);
            int rb = SortRank(b);
            if(ra == 4 || rb == 4)
            {
                return ra == rb ? 0 : (ra == 4 ? 1 : -1);
            }
            int c;
            if(ra != rb)
            {
                c = ra.CompareTo(rb);
            }
            else if(a is BoolEval ba)
            {
                c = ba.BooleanValue.CompareTo(((BoolEval) b).BooleanValue);
            }
            else if(a is NumericValueEval na)
            {
                c = na.NumberValue.CompareTo(((NumericValueEval) b).NumberValue);
            }
            else if(a is StringValueEval sa)
            {
                c = string.Compare(sa.StringValue, ((StringValueEval) b).StringValue, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                c = 0;
            }
            return c * order;
        }

        private static int SortRank(ValueEval v)
        {
            if(v is BoolEval)
            {
                return 2;
            }
            if(v is NumericValueEval)
            {
                return 0;
            }
            if(v is StringValueEval)
            {
                return 1;
            }
            if(v is ErrorEval)
            {
                return 3;
            }
            return 4;
        }

        // UNIQUE(array, [by_col], [exactly_once])
        private static ValueEval Unique(ValueEval[] args, OperationEvaluationContext ec)
        {
            if(args.Length < 1 || args.Length > 3)
            {
                return ErrorEval.VALUE_INVALID;
            }
            ValueEval[][] grid = ToGrid(args[0]);
            bool byCol = args.Length > 1 && BoolArg(args[1], ec);
            bool exactlyOnce = args.Length > 2 && BoolArg(args[2], ec);
            if(byCol)
            {
                grid = Transpose(grid);
            }

            var firstRowForKey = new Dictionary<string, int>();
            var counts = new Dictionary<string, int>();
            var keysInOrder = new List<string>();
            for(int r = 0; r < grid.Length; r++)
            {
                string key = string.Join("\u0001", grid[r].Select(UniqueKey));
                if(counts.TryGetValue(key, out int n))
                {
                    counts[key] = n + 1;
                }
                else
                {
                    counts[key] = 1;
                    firstRowForKey[key] = r;
                    keysInOrder.Add(key);
                }
            }

            ValueEval[][] result = keysInOrder
                .Where(k => !exactlyOnce || counts[k] == 1)
                .Select(k => grid[firstRowForKey[k]])
                .ToArray();
            if(result.Length == 0)
            {
                return ErrorEval.CALC;
            }
            return ToArray(byCol ? Transpose(result) : result, ec);
        }

        private static string UniqueKey(ValueEval v)
        {
            switch(v)
            {
                case BoolEval b:
                    return "b" + b.BooleanValue;
                case NumericValueEval n:
                    return "n" + (n.NumberValue == 0 ? 0d : n.NumberValue).ToString("R", CultureInfo.InvariantCulture);
                case StringValueEval s:
                    return "s" + s.StringValue.ToUpperInvariant();
                case ErrorEval e:
                    return "e" + e.ErrorCode;
                default:
                    return "z";
            }
        }

        // SEQUENCE(rows, [columns], [start], [step])
        private static ValueEval Sequence(ValueEval[] args, OperationEvaluationContext ec)
        {
            if(args.Length < 1 || args.Length > 4)
            {
                return ErrorEval.VALUE_INVALID;
            }
            double rowsArg = NumberArg(args, 0, ec, 1);
            double colsArg = NumberArg(args, 1, ec, 1);
            double start = NumberArg(args, 2, ec, 1);
            double step = NumberArg(args, 3, ec, 1);
            if(rowsArg < 0 || colsArg < 0 || rowsArg > MaxRows || colsArg > MaxColumns)
            {
                return ErrorEval.VALUE_INVALID;
            }
            int rows = (int) rowsArg;
            int cols = (int) colsArg;
            if(rows == 0 || cols == 0)
            {
                return ErrorEval.CALC;
            }
            if((long) rows * cols > MaxCells)
            {
                return ErrorEval.NUM_ERROR;
            }

            var result = new ValueEval[rows][];
            for(int r = 0; r < rows; r++)
            {
                result[r] = new ValueEval[cols];
                for(int c = 0; c < cols; c++)
                {
                    result[r][c] = new NumberEval(start + step * ((double) r * cols + c));
                }
            }
            return ToArray(result, ec);
        }

        private static double NumberArg(ValueEval[] args, int index, OperationEvaluationContext ec, double defaultValue)
        {
            if(index >= args.Length || args[index] is MissingArgEval)
            {
                return defaultValue;
            }
            ValueEval v = OperandResolver.GetSingleValue(args[index], ec.RowIndex, ec.ColumnIndex);
            return OperandResolver.CoerceValueToDouble(v);
        }

        private static bool BoolArg(ValueEval arg, OperationEvaluationContext ec)
        {
            if(arg is MissingArgEval)
            {
                return false;
            }
            ValueEval v = OperandResolver.GetSingleValue(arg, ec.RowIndex, ec.ColumnIndex);
            return OperandResolver.CoerceValueToBoolean(v, false) ?? false;
        }

        private static int ToInt(ValueEval v)
        {
            if(v is ErrorEval e)
            {
                throw new EvaluationException(e);
            }
            return (int) OperandResolver.CoerceValueToDouble(v);
        }

        private static ValueEval[][] ToGrid(ValueEval arg)
        {
            switch(arg)
            {
                case ErrorEval error:
                    throw new EvaluationException(error);
                case MissingArgEval:
                    throw new EvaluationException(ErrorEval.VALUE_INVALID);
                case TwoDEval area:
                    var grid = new ValueEval[area.Height][];
                    for(int r = 0; r < area.Height; r++)
                    {
                        grid[r] = new ValueEval[area.Width];
                        for(int c = 0; c < area.Width; c++)
                        {
                            grid[r][c] = area.GetValue(r, c);
                        }
                    }
                    return grid;
                case RefEval re:
                    return new[] { new[] { re.GetInnerValueEval(re.FirstSheetIndex) } };
                default:
                    return new[] { new[] { arg } };
            }
        }

        private static ValueEval[][] Transpose(ValueEval[][] grid)
        {
            int height = grid.Length;
            int width = grid[0].Length;
            var result = new ValueEval[width][];
            for(int c = 0; c < width; c++)
            {
                result[c] = new ValueEval[height];
                for(int r = 0; r < height; r++)
                {
                    result[c][r] = grid[r][c];
                }
            }
            return result;
        }

        private static CacheAreaEval ToArray(ValueEval[][] grid, OperationEvaluationContext ec)
        {
            int height = grid.Length;
            int width = grid[0].Length;
            int firstRow = Math.Max(ec.RowIndex, 0);
            int firstCol = Math.Max(ec.ColumnIndex, 0);
            return new CacheAreaEval(firstRow, firstCol, firstRow + height - 1, firstCol + width - 1,
                grid.SelectMany(r => r).ToArray());
        }
    }
}