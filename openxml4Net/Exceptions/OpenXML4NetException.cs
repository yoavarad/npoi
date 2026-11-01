using NPOI.OpenXml4Net.Exceptions;
using System;

namespace NPOI.OpenXml4Net
{
    public class OpenXml4NetException : Exception
    {
        public OpenXml4NetException(String msg)
            : base(msg)
        {

        }

        public OpenXml4NetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}