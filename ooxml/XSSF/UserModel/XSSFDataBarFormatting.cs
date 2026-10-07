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
namespace NPOI.XSSF.UserModel
{
    using NPOI.OOXML.XSSF.UserModel;
    using NPOI.OpenXmlFormats.Spreadsheet;
    using NPOI.SS.UserModel;
    using System;

    /**
     * High level representation for DataBar / Data Bar Formatting 
     *  component of Conditional Formatting Settings
     */
    public class XSSFDataBarFormatting : IDataBarFormatting
    {
        IIndexedColorMap _colorMap;
        readonly CT_DataBar _databar;

        /*package*/
        public XSSFDataBarFormatting(CT_DataBar databar, IIndexedColorMap colorMap)
        {
            _databar = databar;
            _colorMap = colorMap;
        }

        public bool IsIconOnly
        {
            get
            {
                if(_databar.IsSetShowValue())
                    return !_databar.showValue;
                return false;
            }
            set
            {
                _databar.showValue = value;
            }
        }

        public bool IsLeftToRight
        {
            get
            {
                return true;
            }
            set
            {
                // won't fix: stored only in the x14 dataBar extension, which is not modelled; value is not persisted
            }
        }


        public int WidthMin
        {
            get
            {
                return 0;
            }
            set
            {
                // won't fix: stored only in the x14 dataBar extension, which is not modelled; value is not persisted
            }
        }

        public int WidthMax
        {
            get
            {
                return 100;
            }
            set
            {
                // won't fix: stored only in the x14 dataBar extension, which is not modelled; value is not persisted
            }
        }


        public IColor Color
        {
            get
            {
                return XSSFColor.From(_databar.color, _colorMap);
            }
            set
            {
                _databar.color = ((XSSFColor) value).GetCTColor();
            }
        }

        public IConditionalFormattingThreshold MinThreshold
        {
            get
            {
                return new XSSFConditionalFormattingThreshold(_databar.cfvo[0]);
            }
        }
        public IConditionalFormattingThreshold MaxThreshold
        {
            get
            {
                return new XSSFConditionalFormattingThreshold(_databar.cfvo[1]);
            }
        }

        public XSSFConditionalFormattingThreshold CreateThreshold()
        {
            return new XSSFConditionalFormattingThreshold(_databar.AddNewCfvo());
        }
    }

}