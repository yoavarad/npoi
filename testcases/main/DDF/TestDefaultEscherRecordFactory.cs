namespace TestCases.DDF
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NPOI.DDF;
    using NPOI.Util;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    [TestFixture]
    public class TestDefaultEscherRecordFactory
    {
        private class NoRecordId
        {
        }

        [Test]
        public void TestRecordsToMapAttachesCause()
        {
            MethodInfo m = typeof(DefaultEscherRecordFactory).GetMethod("RecordsToMap",
                BindingFlags.NonPublic | BindingFlags.Static);
            ClassicAssert.IsNotNull(m);

            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(
                () => m.Invoke(null, [new Type[] { typeof(NoRecordId) }]));

            RecordFormatException ex = tie.InnerException as RecordFormatException;
            ClassicAssert.IsNotNull(ex);
            ClassicAssert.IsNotNull(ex.InnerException);
        }
    }
}
