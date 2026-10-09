namespace NPOI.SS.Formula
{
    /// <summary>
    /// Implemented by evaluation cells of formats that can store dynamic-array (spilling) formulas.
    /// </summary>
    public interface IDynamicArrayEvaluationCell
    {
        /// <summary>
        /// True when the cell belongs to the range of a dynamic-array formula.
        /// </summary>
        bool IsDynamicArrayFormula { get; }
    }
}