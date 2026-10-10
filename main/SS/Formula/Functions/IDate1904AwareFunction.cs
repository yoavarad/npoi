namespace NPOI.SS.Formula.Functions
{
    using NPOI.SS.Formula.Eval;

    /// <summary>
    /// A function whose result depends on the workbook's date system (1900 or 1904 date windowing).
    /// The evaluator calls this overload with the evaluating workbook's setting.
    /// </summary>
    public interface IDate1904AwareFunction : Function
    {
        ValueEval Evaluate(ValueEval[] args, int srcRowIndex, int srcColumnIndex, bool use1904windowing);
    }
}