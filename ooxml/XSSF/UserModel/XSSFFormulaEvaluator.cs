/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

using NPOI.HSSF.UserModel;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS;
using NPOI.SS.Formula;
using NPOI.SS.Formula.Eval;
using NPOI.SS.Formula.UDF;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
namespace NPOI.XSSF.UserModel
{

    /**
     * Evaluates formula cells.<p/>
     *
     * For performance reasons, this class keeps a cache of all previously calculated intermediate
     * cell values.  Be sure to call {@link #ClearAllCachedResultValues()} if any workbook cells are Changed between
     * calls to Evaluate~ methods on this class.
     *
     * @author Amol S. Deshmukh &lt; amolweb at ya hoo dot com &gt;
     * @author Josh Micich
     */
    public class XSSFFormulaEvaluator : BaseXSSFFormulaEvaluator
    {

        private readonly XSSFWorkbook _book;

        public XSSFFormulaEvaluator(IWorkbook workbook)
            : this(workbook as XSSFWorkbook, null, null)
        { }
        public XSSFFormulaEvaluator(XSSFWorkbook workbook)
            : this(workbook, null, null)
        { }


        private XSSFFormulaEvaluator(XSSFWorkbook workbook, IStabilityClassifier stabilityClassifier, UDFFinder udfFinder)
            : this(workbook, new WorkbookEvaluator(XSSFEvaluationWorkbook.Create(workbook), stabilityClassifier, udfFinder))
        {
        }

        protected XSSFFormulaEvaluator(XSSFWorkbook workbook, WorkbookEvaluator bookEvaluator)
            : base(bookEvaluator)
        {
            _book = workbook;
        }

        /**
         * @param stabilityClassifier used to optimise caching performance. Pass <code>null</code>
         * for the (conservative) assumption that any cell may have its defInition Changed After
         * Evaluation begins.
         * @param udfFinder pass <code>null</code> for default (AnalysisToolPak only)
         */
        public static XSSFFormulaEvaluator Create(XSSFWorkbook workbook, IStabilityClassifier stabilityClassifier, UDFFinder udfFinder)
        {
            return new XSSFFormulaEvaluator(workbook, stabilityClassifier, udfFinder);
        }

        public override void NotifySetFormula(ICell cell)
        {
            _bookEvaluator.NotifyUpdateCell(new XSSFEvaluationCell((XSSFCell) cell));
        }
        public override void NotifyDeleteCell(ICell cell)
        {
            _bookEvaluator.NotifyDeleteCell(new XSSFEvaluationCell((XSSFCell) cell));
        }
        public override void NotifyUpdateCell(ICell cell)
        {
            _bookEvaluator.NotifyUpdateCell(new XSSFEvaluationCell((XSSFCell) cell));
        }

        /// <summary>
        /// Evaluates a formula cell and stores the result. On the anchor of a dynamic-array formula
        /// (see <see cref="XSSFSheet.SetDynamicArrayFormula"/>) the array result spills: the formula's
        /// range is resized to the result and every cell in it gets its value. If a cell in the way is
        /// not empty, is merged, or the range runs off the sheet, the formula shrinks back to its anchor,
        /// which gets <c>#SPILL!</c>.
        /// </summary>
        public override CellType EvaluateFormulaCell(ICell cell)
        {
            if(cell is XSSFCell xssfCell && IsDynamicArrayAnchor(xssfCell))
            {
                return Spill(xssfCell);
            }
            return base.EvaluateFormulaCell(cell);
        }

        private static bool IsDynamicArrayAnchor(XSSFCell cell)
        {
            CT_Cell ct = cell.GetCTCell();
            return ct.cm != 0 && ct.f != null && ct.f.t == ST_CellFormulaType.array;
        }

        private CellType Spill(XSSFCell anchor)
        {
            XSSFSheet sheet = (XSSFSheet) anchor.Sheet;
            ValueEval result = _bookEvaluator.EvaluateArrayResult(new XSSFEvaluationCell(anchor));
            if(result is RefEval refEval)
            {
                result = refEval.GetInnerValueEval(refEval.FirstSheetIndex);
            }
            AreaEval area = result as AreaEval;
            int rows = area == null ? 1 : area.Height;
            int cols = area == null ? 1 : area.Width;

            CellRangeAddress current = anchor.ArrayFormulaRange;
            var target = new CellRangeAddress(anchor.RowIndex, anchor.RowIndex + rows - 1,
                anchor.ColumnIndex, anchor.ColumnIndex + cols - 1);
            bool blocked = IsSpillBlocked(sheet, target, current);
            if(blocked)
            {
                target = new CellRangeAddress(anchor.RowIndex, anchor.RowIndex, anchor.ColumnIndex, anchor.ColumnIndex);
            }
            if(!target.Equals(current))
            {
                string formula = anchor.CellFormula;
                uint cm = anchor.GetCTCell().cm;
                sheet.RemoveArrayFormula(anchor);
                sheet.SetArrayFormula(formula, target);
                anchor.GetCTCell().cm = cm;
                ClearAllCachedResultValues();
                _spillRangeChanges++;
            }

            if(blocked)
            {
                SetCellValue(anchor, CellValue.GetError(ErrorEval.SPILL.ErrorCode));
                return CellType.Error;
            }
            CellType anchorType = CellType._None;
            for(int r = 0; r < rows; r++)
            {
                IRow row = sheet.GetRow(target.FirstRow + r);
                for(int c = 0; c < cols; c++)
                {
                    CellValue cv = ToCellValue(area == null ? result : area.GetRelativeValue(r, c));
                    SetCellValue(row.GetCell(target.FirstColumn + c), cv);
                    if(r == 0 && c == 0)
                    {
                        anchorType = cv.CellType;
                    }
                }
            }
            return anchorType;
        }

        private static bool IsSpillBlocked(XSSFSheet sheet, CellRangeAddress target, CellRangeAddress current)
        {
            SpreadsheetVersion version = SpreadsheetVersion.EXCEL2007;
            if(target.LastRow > version.LastRowIndex || target.LastColumn > version.LastColumnIndex)
            {
                return true;
            }
            for(int r = target.FirstRow; r <= target.LastRow; r++)
            {
                IRow row = sheet.GetRow(r);
                if(row == null)
                {
                    continue;
                }
                for(int c = target.FirstColumn; c <= target.LastColumn; c++)
                {
                    if(current.IsInRange(r, c))
                    {
                        continue;
                    }
                    ICell cell = row.GetCell(c);
                    if(cell != null && cell.CellType != CellType.Blank)
                    {
                        return true;
                    }
                }
            }
            foreach(CellRangeAddress merged in sheet.MergedRegions)
            {
                if(merged.Intersects(target))
                {
                    return true;
                }
            }
            return false;
        }

        private static CellValue ToCellValue(ValueEval value)
        {
            switch(value)
            {
                case NumberEval n:
                    return new CellValue(n.NumberValue);
                case BoolEval b:
                    return CellValue.ValueOf(b.BooleanValue);
                case StringEval s:
                    return new CellValue(s.StringValue);
                case ErrorEval e:
                    return CellValue.GetError(e.ErrorCode);
                case BlankEval:
                    return new CellValue(0.0);
                default:
                    return CellValue.GetError(ErrorEval.VALUE_INVALID.ErrorCode);
            }
        }
        /**
         * Loops over all cells in all sheets of the supplied
         *  workbook.
         * For cells that contain formulas, their formulas are
         *  Evaluated, and the results are saved. These cells
         *  remain as formula cells.
         * For cells that do not contain formulas, no Changes
         *  are made.
         * This is a helpful wrapper around looping over all
         *  cells, and calling EvaluateFormulaCell on each one.
         */
        public static void EvaluateAllFormulaCells(XSSFWorkbook wb)
        {
            new XSSFFormulaEvaluator(wb).EvaluateAll();
        }
        /**
         * Loops over all cells in all sheets of the supplied
         *  workbook.
         * For cells that contain formulas, their formulas are
         *  Evaluated, and the results are saved. These cells
         *  remain as formula cells.
         * For cells that do not contain formulas, no Changes
         *  are made.
         * This is a helpful wrapper around looping over all
         *  cells, and calling EvaluateFormulaCell on each one.
         */
        public override void EvaluateAll()
        {
            // a spill that changes its range clears the cache, so cells evaluated earlier in the
            // pass may hold stale results: repeat until no spill range changes
            for(int pass = 0; pass < MaxSpillPasses; pass++)
            {
                int changesBefore = _spillRangeChanges;
                EvaluateAllFormulaCells(_book, this);
                if(_spillRangeChanges == changesBefore)
                {
                    return;
                }
            }
        }

        private const int MaxSpillPasses = 10;
        private int _spillRangeChanges;

        /**
	     * Turns a XSSFCell into a XSSFEvaluationCell
	     */
        protected override IEvaluationCell ToEvaluationCell(ICell cell)
        {
            if(cell is not XSSFCell xssfCell)
            {
                throw new ArgumentException("Unexpected type of cell: " + cell.GetType().Name + "." +
                        " Only XSSFCells can be evaluated.");
            }

            return new XSSFEvaluationCell(xssfCell);
        }
    }
}