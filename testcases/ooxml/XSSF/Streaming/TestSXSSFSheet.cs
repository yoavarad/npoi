/*
 *  ====================================================================
 *    Licensed to the Apache Software Foundation (ASF) under one or more
 *    contributor license agreements.  See the NOTICE file distributed with
 *    this work for Additional information regarding copyright ownership.
 *    The ASF licenses this file to You under the Apache License, Version 2.0
 *    (the "License"); you may not use this file except in compliance with
 *    the License.  You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 * ====================================================================
 */

namespace TestCases.XSSF.Streaming
{
    using NPOI.SS.UserModel;
    using NPOI.Util;
    using NPOI.XSSF;
    using NPOI.XSSF.Streaming;
    using NPOI.XSSF.UserModel;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;
    using TestCases.SS.UserModel;

    [TestFixture]
    public class TestSXSSFSheet : BaseTestXSheet
    {

        public TestSXSSFSheet()
            : base(SXSSFITestDataProvider.instance)
        {

        }


        [TearDown]
        public void TearDown()
        {
            //SXSSFITestDataProvider.instance.Cleanup();
        }

        protected override void TrackColumnsForAutoSizingIfSXSSF(ISheet sheet)
        {
            SXSSFSheet sxSheet = (SXSSFSheet)sheet;
            sxSheet.TrackAllColumnsForAutoSizing();
        }

        /**
         * cloning of sheets is not supported in SXSSF
         */

        [Test]
        public override void CloneSheet()
        {
            //thrown.Expect(typeof(Exception));
            //thrown.ExpectMessage("NotImplemented");
            Assert.Throws<RuntimeException>(() =>
            {
                base.CloneSheet();
            });

        }


        [Test]
        public override void CloneSheetMultipleTimes()
        {
            //thrown.Expect(typeof(Exception));
            //thrown.ExpectMessage("NotImplemented");
            Assert.Throws<RuntimeException>(() =>
            {
                base.CloneSheetMultipleTimes();
            });
        }

        /**
         * Shifting rows is not supported in SXSSF
         */

        [Test]
        public override void ShiftMerged()
        {
            //thrown.Expect(typeof(Exception));
            //thrown.ExpectMessage("NotImplemented");

            Assert.Throws<NotImplementedException>(() =>
            {
                base.ShiftMerged();
            });
        }

        /**
         *  Bug 35084: cloning cells with formula
         *
         *  The test is disabled because cloning of sheets is not supported in SXSSF
         */

        [Test]
        public override void Bug35084()
        {
            //thrown.Expect(typeof(Exception));
            //thrown.ExpectMessage("NotImplemented");

            Assert.Throws<RuntimeException>(() =>
            {
                base.Bug35084();
            });
        }

        [Test]
        public override void GetCellComment()
        {
            // TODO: Reading cell comments via Sheet does not work currently as it tries 
            // to access the underlying sheet for this, but comments are stored as
            // properties on Cells...
        }

        [Test]
        public void GroupRowDoesNotGroupPastToRow()
        {
            using var wb = new SXSSFWorkbook(100);
            var sheet = (SXSSFSheet) wb.CreateSheet();
            for(int i = 0; i < 6; i++)
            {
                sheet.CreateRow(i);
            }

            sheet.GroupRow(1, 3);

            ClassicAssert.AreEqual(0, sheet.GetRow(0).OutlineLevel);
            for(int i = 1; i <= 3; i++)
            {
                ClassicAssert.AreEqual(1, sheet.GetRow(i).OutlineLevel, "row " + i);
            }
            ClassicAssert.AreEqual(0, sheet.GetRow(4).OutlineLevel, "row past toRow must not be grouped");
            ClassicAssert.AreEqual(0, sheet.GetRow(5).OutlineLevel);
        }

        [Test]
        public void AutoSizeColumnTrackingSkipsMergedRegionWithMissingFirstCell()
        {
            using var wb = new SXSSFWorkbook(100);
            var sheet = (SXSSFSheet) wb.CreateSheet();
            sheet.TrackAllColumnsForAutoSizing();
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 1, 2));
            IRow row = sheet.CreateRow(0);
            row.CreateCell(2).SetCellValue("text in second column of merged region");

            Assert.DoesNotThrow(() => sheet.AutoSizeColumn(2, true));
        }

        [Test]
        public void OverrideFlushedRows()
        {
            IWorkbook wb = new SXSSFWorkbook(3);
            try
            {
                ISheet sheet = wb.CreateSheet();

                sheet.CreateRow(1);
                sheet.CreateRow(2);
                sheet.CreateRow(3);
                sheet.CreateRow(4);
                ((SXSSFSheet) sheet).FlushRows();

                Assert.Throws<ArgumentException>(() =>
                {
                    sheet.CreateRow(1);
                }, "Attempting to write a row[1] in the range [0,1] that is already written to disk.");
            }
            finally
            {
                wb.Close();
            }
        }

        [Test]
        public void OverrideRowsInTemplate()
        {
            XSSFWorkbook template = new XSSFWorkbook();
            template.CreateSheet().CreateRow(1);

            IWorkbook wb = new SXSSFWorkbook(template);
            try
            {
                ISheet sheet = wb.GetSheetAt(0);

                try
                {
                    sheet.CreateRow(1);
                    Assert.Fail("expected exception");
                }
                catch(Exception e)
                {
                    ClassicAssert.AreEqual("Attempting to write a row[1] in the range [0,1] that is already written to disk.", e.Message);
                }
                try
                {
                    sheet.CreateRow(0);
                    Assert.Fail("expected exception");
                }
                catch(Exception e)
                {
                    ClassicAssert.AreEqual("Attempting to write a row[0] in the range [0,1] that is already written to disk.", e.Message);
                }
                sheet.CreateRow(2);
            }
            finally
            {
                wb.Close();
                template.Close();
            }
        }

        [Test]
        public void ChangeRowNum()
        {
            SXSSFWorkbook wb = new SXSSFWorkbook(3);
            SXSSFSheet sheet = wb.CreateSheet() as SXSSFSheet;
            SXSSFRow row0 = sheet.CreateRow(0) as SXSSFRow;
            SXSSFRow row1 = sheet.CreateRow(1) as SXSSFRow;
            sheet.ChangeRowNum(row0, 2);

            ClassicAssert.AreEqual(1, row1.RowNum, "Row 1 knows its row number");
            ClassicAssert.AreEqual(2, row0.RowNum, "Row 2 knows its row number");
            ClassicAssert.AreEqual(1, sheet.GetRowNum(row1), "Sheet knows Row 1's row number");
            ClassicAssert.AreEqual(2, sheet.GetRowNum(row0), "Sheet knows Row 2's row number");
            var it = sheet.GetEnumerator();
            it.MoveNext();
            ClassicAssert.AreEqual(row1, it.Current, "Sheet row iteratation order should be ascending");

            wb.Close();
        }
    }
}