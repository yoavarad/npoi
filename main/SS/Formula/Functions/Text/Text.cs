using NPOI.SS.Formula.Eval;
using NPOI.SS.UserModel;
using System;

namespace NPOI.SS.Formula.Functions
{
    /**
	 * An implementation of the TEXT function
	 * TEXT returns a number value formatted with the given number formatting string. 
	 * This function is not a complete implementation of the Excel function, but
	 *  handles most of the common cases. All work is passed down to 
	 *  {@link DataFormatter} to be done, as this works much the same as the
	 *  display focused work that that does. 
	 */
    public class Text : Fixed2ArgFunction, IDate1904AwareFunction
    {
        public static DataFormatter Formatter = new DataFormatter();
        public override ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1)
        {
            return Evaluate(srcRowIndex, srcColumnIndex, arg0, arg1, false);
        }

        public ValueEval Evaluate(ValueEval[] args, int srcRowIndex, int srcColumnIndex, bool use1904windowing)
        {
            if(args.Length != 2)
            {
                return ErrorEval.VALUE_INVALID;
            }
            return Evaluate(srcRowIndex, srcColumnIndex, args[0], args[1], use1904windowing);
        }

        private static ValueEval Evaluate(int srcRowIndex, int srcColumnIndex, ValueEval arg0, ValueEval arg1, bool use1904windowing)
        {
            ValueEval resolved;
            try
            {
                resolved = OperandResolver.GetSingleValue(arg0, srcRowIndex, srcColumnIndex);
            }
            catch(EvaluationException e)
            {
                return e.GetErrorEval();
            }

            // Excel formats numeric text like a number (TEXT("123","0.00") is "123.00");
            // other text is returned unchanged
            if(resolved is StringEval se && double.IsNaN(OperandResolver.ParseDouble(se.StringValue)))
                return resolved;

            double s0;
            String s1;
            try
            {
                s0 = OperandResolver.CoerceValueToDouble(resolved);
                s1 = TextFunction.EvaluateStringArg(arg1, srcRowIndex, srcColumnIndex);
            }
            catch(EvaluationException e)
            {
                return e.GetErrorEval();
            }
            try
            {
                // Ask DataFormatter to handle the String for us
                String formattedStr = Formatter.FormatRawCellContents(s0, -1, s1, use1904windowing);
                return new StringEval(formattedStr);
            }
            catch
            {
                return ErrorEval.VALUE_INVALID;
            }
        }
    }
}