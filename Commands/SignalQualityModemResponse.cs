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

namespace Pixelfence.GSM.Commands
{
    public class SignalQualityModemResponse
        : ModemResponse
    {
        public SignalQualityModemResponse(ModemResponse responsePrototype)
            : base(responsePrototype.Command)
        {
            ResultCode = responsePrototype.ResultCode;
            ProcessResponsePrototype(responsePrototype);
        }

        public SignalQualityEventArgs SignalQuality { get; private set; }

        private void ProcessResponsePrototype(ModemResponse prototype)
        {
            if (prototype.ResultCode == CommandResult.OK)
            {
                for (int l = 0; l < prototype.ResponseLinesCount; l++)
                {
                    var respLine = prototype.GetResponseLine(l);
                    AddResponseLine(respLine);

                    if (respLine.StartsWith("+CSQ", true, System.Globalization.CultureInfo.CurrentCulture))
                    {
                        string[] tokens = respLine.Substring(5).Split(',');

                        int sigQuality = ParseTokenToInt32(tokens, 0);
                        int bitErrRate = ParseTokenToInt32(tokens, 1);

                        SignalQuality = new SignalQualityEventArgs(sigQuality, bitErrRate);
                    }
                }
            }
        }
    }
}
