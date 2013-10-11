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

namespace Pixelfence.GSM
{
    public class ModemResponse
    {
        private List<string> _responseLines = new List<string>();
        public ModemResponse(Command command)
        {
            Command = command;
            ResultCode = CommandResult.Unknown;            
        }            

        public Command Command { get; private set; }
        
        public CommandResult ResultCode { get; set; }

        public void AddResponseLine(string line)
        {
            _responseLines.Add(line);
        }
        public int ResponseLinesCount
        {
            get{
                return _responseLines.Count;
            }
        }

        public string GetResponseLine(int index)
        {
            return _responseLines[index];
        }        

        #region Static Command Parse-Helper Functions

        public static int ParseTokenToInt32(string[] tokens, int index)
        {
            if (tokens.Length <= index)
                return -1;

            int value = -1;
            if (!Int32.TryParse(tokens[index].Trim(), out value))
                value = -1;

            return value;
        }

        public static string RemoveQuotes(string text)
        {
            int startIndex = 0;
            int length = text.Length;

            if (text.StartsWith("\"") || text.StartsWith("'"))
            {
                startIndex = 1; 
                length--;
            }

            if (text.EndsWith("\"") || text.EndsWith("'"))
                length--;

            return text.Substring(startIndex, Math.Max(length, 0));
        }

        public static string ParseTokenToString(string[] tokens, int index)
        {
            if (tokens.Length <= index)
                return string.Empty;

            return tokens[index];
        }

        #endregion
    }
}
