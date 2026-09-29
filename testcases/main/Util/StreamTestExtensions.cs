using System.IO;

namespace TestCases
{
    /// <summary>Test helper: ReadExactly is not available on net472.</summary>
    public static class StreamTestExtensions
    {
        public static void ReadFully(this Stream stream, byte[] buffer, int offset, int count)
        {
            while(count > 0)
            {
                int n = stream.Read(buffer, offset, count);
                if(n <= 0)
                {
                    throw new EndOfStreamException();
                }
                offset += n;
                count -= n;
            }
        }
    }
}