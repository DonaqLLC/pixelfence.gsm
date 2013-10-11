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
    public class ReadMessageModemResponse
        : ModemResponse
    {
        static ReadMessageModemResponse()
        {
            parseRegex = new System.Text.RegularExpressions.Regex("(?<status>[^,]*),(?<peer>[^,]*),(?<empty>[^,]*),(?<date>.*)", System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        private int _messageIndex = 0;
        public ReadMessageModemResponse(int messageIndex, ModemResponse responsePrototype)
            : base(responsePrototype.Command)
        {
            _messageIndex = messageIndex;
            ResultCode = responsePrototype.ResultCode;
            ProcessResponsePrototype(responsePrototype);  
        }

        public SMSMessageInfo Message
        {
            get;
            private set;
        }

        private static System.Text.RegularExpressions.Regex parseRegex;

        private void ProcessResponsePrototype(ModemResponse prototype)
        {
            if (prototype.ResultCode == CommandResult.OK)
            {                
                SMSMessageInfo lastSMS = null;

                for (int l = 0; l < prototype.ResponseLinesCount; l++)
                {
                    var respLine = prototype.GetResponseLine(l);
                    AddResponseLine(respLine);

                    if (respLine.StartsWith("+CMGR", true, System.Globalization.CultureInfo.CurrentCulture))
                    {
                        var Match = parseRegex.Match(respLine.Substring(7));

                        lastSMS = new SMSMessageInfo();
                        
                        lastSMS.Index = _messageIndex;

                        if (Match.Groups["status"] != null && Match.Groups["status"].Success)
                        {
                            var status = RemoveQuotes(Match.Groups["status"].Value.Trim().ToUpper());
                            if (status == "REC UNREAD")
                                lastSMS.Status = SMSMessageStatus.Unread;
                            else if (status == "REC READ")
                                lastSMS.Status = SMSMessageStatus.Read;
                            else if (status == "STO UNSENT")
                                lastSMS.Status = SMSMessageStatus.Unsent;
                            else if (status == "STO SENT")
                                lastSMS.Status = SMSMessageStatus.Sent;
                        }

                        if (Match.Groups["status"] != null && Match.Groups["status"].Success)
                        {
                            lastSMS.Peer = RemoveQuotes(Match.Groups["peer"].Value.Trim());
                        }

                        string ts = RemoveQuotes(Match.Groups["date"].Value.Trim());

                        lastSMS.Timestamp = ParseDateTime(ts);
                    }
                    else if (lastSMS != null)
                    {
                        lastSMS.Text += respLine;
                    }
                }

                Message = lastSMS;
            }
        }

        private DateTime ParseDateTime(string input)
        {
            string[] datetime = input.Split(',');

            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int min = 0;
            int sec = 0;

            if (datetime.Length > 0)
            {
                string[] dateParts = datetime[0].Split('/');
                if (dateParts.Length > 0)
                {
                    Int32.TryParse(dateParts[0], out year);
                    year += 2000;
                }
                if (dateParts.Length > 1)
                    Int32.TryParse(dateParts[1], out month);
                if (dateParts.Length > 2)
                    Int32.TryParse(dateParts[2], out day);
            }

            if (datetime.Length > 1)
            {
                string[] timeParts = datetime[1].Split(':');
                if (timeParts.Length > 0)
                    Int32.TryParse(timeParts[0], out hour);
                if (timeParts.Length > 1)
                    Int32.TryParse(timeParts[1], out min);
                if (timeParts.Length > 2)
                    Int32.TryParse(timeParts[2], out sec);
            }

            return new DateTime(year, month, day, hour, min, sec);
        }
    }
}
