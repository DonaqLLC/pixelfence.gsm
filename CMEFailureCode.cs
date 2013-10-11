#region Copyright notice and license
// The MIT License (MIT)
// 
// Copyright (c) 2013 Donaq LLC
// 
// Pixelfence.GSM - A control Interface for Moxa OnCell GSM Modems
// http://www.donaq.com
// 
// Original Author(s): Miky Dinescu
//  
// This software was entirely developed by Donaq LLC, and contributors,
// and it was in no way endorsed or comissioned by Moxa.
//  
// Information about the Moxa OnCell 2100 Series Modems can be found at:
//   http://www.moxa.com/product/OnCell_G2111.htm
//  
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Pixelfence.GSM
{
    [DebuggerDisplay("{FailureCode,nq}: {FailureDescription}")]
    public class CMEFailureCode
        : ISpecificFailureCode
    {
        private static List<CMEFailureCode> _items = new List<CMEFailureCode>();
        private CMEFailureCode(int failureCode, string failureDescription)
        {
            foreach(var item in _items)
            {
                if(item.FailureCode == failureCode)
                    return;
            }
                
            this.FailureCode = failureCode;
            this.FailureDescription = failureDescription;
            _items.Add(this);
        }

        public int FailureCode
        {
            get;
            private set;
        }

        public string FailureDescription
        {
            get;
            private set;
        }

        public static CMEFailureCode FromErrorCode(string failureCodeText)
        {
            int failureCode = 0;
            if (Int32.TryParse(failureCodeText.Trim(), out failureCode))
            {
                foreach (var item in _items)
                {
                    if (item.FailureCode == failureCode)
                        return item;
                }
                CMEFailureCode newCode = new CMEFailureCode(failureCode, "Unknown Error " + failureCode.ToString());
                _items.Add(newCode);
                return newCode;
            }
            return Unknown;
        }

        public override string ToString()
        {
            return FailureDescription;
        }

        public static readonly CMEFailureCode Unknown = new CMEFailureCode(-1, string.Empty);
        public static readonly CMEFailureCode OperationNotAllowed = new CMEFailureCode(3, "Operation Not Allowed");
        public static readonly CMEFailureCode OperationNotSupported = new CMEFailureCode(4, "Operation Not Supported");
        public static readonly CMEFailureCode PHSIMPinRequired = new CMEFailureCode(5, "PH-SIM Pin Required (SIM Lock)");
        public static readonly CMEFailureCode SIMNotInserted = new CMEFailureCode(10, "SIM Not Inserted");
        public static readonly CMEFailureCode SIMPINRequired = new CMEFailureCode(11, "SIM PIN Required");
        public static readonly CMEFailureCode SIMPUKRequired = new CMEFailureCode(12, "SIM PUK Required");
        public static readonly CMEFailureCode SIMFailure = new CMEFailureCode(13, "SIM Failure");
    }
}
