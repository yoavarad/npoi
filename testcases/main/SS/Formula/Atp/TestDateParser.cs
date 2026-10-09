/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

namespace TestCases.SS.Formula.Atp
{
    using NPOI.SS.Formula.Atp;
    using NPOI.SS.Formula.Eval;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using System;

    /**
     * @author jfaenomoto@gmail.com
     */
    [TestFixture]
    public class TestDateParser
    {
        [Test]
        public void TestFailWhenNoDate()
        {
            try
            {
                DateParser.ParseDate("potato");
                Assert.Fail("Shouldn't parse potato!");
            }
            catch(EvaluationException e)
            {
                ClassicAssert.AreEqual(ErrorEval.VALUE_INVALID, e.GetErrorEval());
            }
        }

        [Test]
        public void TestFailWhenLooksLikeDateButItIsnt()
        {
            try
            {
                DateParser.ParseDate("potato/cucumber/banana");
                Assert.Fail("Shouldn't parse this thing!");
            }
            catch(EvaluationException e)
            {
                ClassicAssert.AreEqual(ErrorEval.VALUE_INVALID, e.GetErrorEval());
            }
        }

        [Test]
        public void TestFailWhenIsInvalidDate()
        {
            try
            {
                DateParser.ParseDate("13/13/13");
                Assert.Fail("Shouldn't parse this thing!");
            }
            catch(EvaluationException e)
            {
                ClassicAssert.AreEqual(ErrorEval.VALUE_INVALID, e.GetErrorEval());
            }
        }

        [Test]
        public void TestShouldParseValidDate()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

            ClassicAssert.AreEqual(new DateTime(1984, 10, 20), DateParser.ParseDate("1984/10/20"));
        }

        [TestCase("en-US", "10/20/1984", 1984, 10, 20)]
        [TestCase("en-US", "1/2/05", 2005, 1, 2)]
        [TestCase("en-US", "1/2/29", 2029, 1, 2)]
        [TestCase("en-US", "1/2/30", 1930, 1, 2)]
        [TestCase("en-US", "10-20-1984", 1984, 10, 20)]
        [TestCase("en-GB", "20/10/1984", 1984, 10, 20)]
        [TestCase("de-DE", "20-10-84", 1984, 10, 20)]
        [TestCase("ja-JP", "84/10/20", 1984, 10, 20)]
        public void TestShouldParseInCultureDateOrder(string culture, string text, int year, int month, int day)
        {
            System.Globalization.CultureInfo saved = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture(culture);
            try
            {
                ClassicAssert.AreEqual(new DateTime(year, month, day), DateParser.ParseDate(text));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = saved;
            }
        }

        [Test]
        public void TestInvalidMonthInCultureOrderIsValueError()
        {
            System.Globalization.CultureInfo saved = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
            try
            {
                DateParser.ParseDate("20/10/1984");
                Assert.Fail("month 20 is not valid in M/D/Y order");
            }
            catch(EvaluationException e)
            {
                ClassicAssert.AreEqual(ErrorEval.VALUE_INVALID, e.GetErrorEval());
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = saved;
            }
        }

        [Test]
        public void TestShouldIgnoreTimestamp()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
            ClassicAssert.AreEqual(new DateTime(1984, 10, 20), DateParser.ParseDate("1984/10/20 12:34:56"));
        }
    }
}