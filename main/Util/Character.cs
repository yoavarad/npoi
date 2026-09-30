namespace NPOI.Util
{
    public class Character
    {
        public static int GetNumericValue(char src)
        {
            if(src >= '0' && src <= '9')
            {
                return (int) src;
            }
            if(src >= 'A' && src <= 'Z')
            {
                return ((int) src) - 55;
            }
            if(src >= 'a' && src <= 'z')
            {
                return ((int) src) - 87;
            }
            return -1;
        }

        // Note: unlike Java's Character.isWhitespace, char.IsWhiteSpace also treats
        // non-breaking spaces (U+00A0, U+2007, U+202F) as whitespace.
        public static bool isWhitespace(char src)
        {
            return char.IsWhiteSpace(src);
        }
    }
}