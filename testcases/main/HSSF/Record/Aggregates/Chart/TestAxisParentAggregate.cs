namespace TestCases.HSSF.Record.Aggregates.Chart
{
    using NPOI.HSSF.Model;
    using NPOI.HSSF.Record;
    using NPOI.HSSF.Record.Aggregates.Chart;
    using NPOI.HSSF.Record.Chart;
    using NPOI.Util;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System.Collections.Generic;

    [TestFixture]
    public class TestAxisParentAggregate
    {
        [Test]
        public void TestInvalidAxesRuleFallsBackToNoAxes()
        {
            // Axis record not followed by CatSerRange/ValueRange makes AxesAggregate throw
            // InvalidOperationException; AxisParentAggregate treats the axes as absent.
            List<Record> recs =
            [
                new AxisParentRecord(), BeginRecord.instance, new PosRecord(),
                new AxisRecord(), BeginRecord.instance,
                new ChartFormatRecord(), BeginRecord.instance, new BarRecord(), new CrtLinkRecord(), EndRecord.instance,
                EndRecord.instance,
            ];
            RecordStream rs = new RecordStream(recs, 0);

            Assert.DoesNotThrow(() => new AxisParentAggregate(rs, null));
            ClassicAssert.IsFalse(rs.HasNext());
        }
    }
}