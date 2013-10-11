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
    [DebuggerDisplay("{code,nq}: {Verbose}")]
    public class CommandResult
    {
        private string code = string.Empty;

        private CommandResult(string numericalCode, string verbose, string description, bool isPrimitive)
        {
            Code = numericalCode;
            Verbose = verbose;
            Description = description;
            IsPrimitive = isPrimitive;
            SpecificFailureCode = null;
        }

        private bool IsPrimitive
        {
            get;
            set;
        }

        public string Code
        {
            get { return code; }
            private set { code = value; }
        }

        public string Verbose
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public ISpecificFailureCode SpecificFailureCode
        {
            get;
            private set;
        }

        public static CommandResult TryParseResponseCode(string text)
        {
            string term = text.Trim().ToUpper();
            if (term == CommandResult.OK.Code || string.Compare(term, "OK", true) == 0)
                return CommandResult.OK;
            else if (term == CommandResult.Ring.Code || string.Compare(term, "RING", true) == 0)
                return CommandResult.Ring;
            else if (term == CommandResult.NoCarrier.Code || string.Compare(term, "NO CARRIER", true) == 0)
                return CommandResult.NoCarrier;
            else if (term == CommandResult.Error.Code || string.Compare(term, "ERROR", true) == 0)
                return CommandResult.Error;
            else if (term == CommandResult.NoAnswer.Code || string.Compare(term, "NO ANSWER", true) == 0)
                return CommandResult.NoAnswer;
            else if (term == CommandResult.Busy.Code || string.Compare(term, "BUSY", true) == 0)
                return CommandResult.Busy;
            else if (term.StartsWith("+CME ERROR", true, System.Globalization.CultureInfo.CurrentCulture))
            {
                CMEFailureCode failCode = CMEFailureCode.FromErrorCode(term.Substring(11).Trim());
                string resultCode = "+CME ERROR: " + failCode.FailureCode.ToString();
                CommandResult cr = new CommandResult(resultCode, resultCode, "CME Error: " + failCode.FailureDescription, true);
                return cr;
            }
            else if (term.StartsWith("+CMS ERROR", true, System.Globalization.CultureInfo.CurrentCulture))
            {
                CMSFailureCode failCode = CMSFailureCode.FromErrorCode(term.Substring(11).Trim());
                string resultCode = "+CMS ERROR: " + failCode.FailureCode.ToString();
                CommandResult cr = new CommandResult(resultCode, resultCode, "CMS Error: " + failCode.FailureDescription, true);
                return cr;                
            }

            return CommandResult.Unknown;
        }

        public override string ToString()
        {
            return Verbose;
        }

        public static IEnumerator<CommandResult> GetEnumerator()
        {
            yield return OK;
            yield return Ring;
            yield return NoCarrier;
            yield return Error;
            yield return Busy;
            yield return NoAnswer;
            yield return CMSError;
            yield return CMEError;
        }

        public static readonly CommandResult Unknown = new CommandResult(null, null, "Unknown or unset command result code", false);
        public static readonly CommandResult OK = new CommandResult("0", "OK", "Command excecuted correctly", true);
        public static readonly CommandResult Ring = new CommandResult("2", "RING", "Incoming call signal from the network", true);
        public static readonly CommandResult NoCarrier = new CommandResult("3", "NO CARRIER", "Command excecuted correctly", true);
        public static readonly CommandResult Error = new CommandResult("4", "ERROR", "Command excecuted correctly", true);
        public static readonly CommandResult Busy = new CommandResult("7", "BUSY", "Command excecuted correctly", true);
        public static readonly CommandResult NoAnswer = new CommandResult("8", "NO ANSWER", "Command excecuted correctly", true);
        public static readonly CommandResult CMSError = new CommandResult("+CMS ERROR", "+CMS ERROR", "Error from SMS command (07.07)", false);
        public static readonly CommandResult CMEError = new CommandResult("+CME ERROR", "+CME ERROR", "Error from GSM 07.05 command", false);
    }
}
