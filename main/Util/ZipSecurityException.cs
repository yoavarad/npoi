using System.IO;

namespace NPOI.OpenXml4Net.Util
{
    /// <summary>
    /// Thrown when a zip violates a zip-bomb limit. Lives in NPOI.Core so VBAMacroReader can throw it;
    /// OpenXml4Net type-forwards it, so existing ZipSecureFile callers are unaffected.
    /// </summary>
    public class ZipSecurityException : IOException
    {
        public ZipSecurityException(string message) : base(message) { }
    }
}