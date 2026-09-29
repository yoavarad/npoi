using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;

namespace TestCases
{
    [TestFixture]
    public class TestPOIDataSamples
    {
        // Test assemblies/TFMs run as parallel processes sharing test-data, so
        // sample files must be opened shareable (regression for exclusive locks).
        [Test]
        public void GetFileAllowsConcurrentOpen()
        {
            POIDataSamples samples = POIDataSamples.GetPOIFSInstance();
            using FileStream first = samples.GetFile("BlockSize512.zvi");
            using FileStream second = samples.GetFile("BlockSize512.zvi");
            ClassicAssert.IsFalse(first.CanWrite);
            ClassicAssert.IsFalse(second.CanWrite);
        }
    }
}