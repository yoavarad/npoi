using System;

namespace NPOI.Util
{
    /// <summary>
    /// Zip-bomb limits shared between assemblies. <c>ZipSecureFile</c> (OpenXml4Net) setters write here,
    /// and readers living in this assembly (e.g. VBAMacroReader) read from here.
    /// </summary>
    public static class ZipLimits
    {
        public static double MinInflateRatio { get; set; } = 0.01d;
        public static long MaxEntrySize { get; set; } = 0xFFFFFFFFL;
        // don't alert for expanded sizes smaller than 100k
        public static long GraceEntrySize { get; set; } = 100 * 1024L;
        public static long MaxEntryCount { get; set; } = 10000;
    }
}