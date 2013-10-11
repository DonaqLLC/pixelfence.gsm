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
using System.IO;
using System.Threading;

namespace Pixelfence.GSM
{
    public class Modem
    {
        public event EventHandler ModemCommunicationLost;
        public event EventHandler<IndicatorsEventArgs> IndicatorsChanged;
        public event EventHandler<SignalQualityEventArgs> SignalQualityChanged;
        public event EventHandler<ModemResponseEventArgs> ResponseReceived;
        public event EventHandler<UnsolicitedNotificationEventArgs> UnsolicitedNotification;
        public event EventHandler Connected;

        private System.IO.Ports.SerialPort _serialPort;
        private ModemState _modemState = ModemState.Unknown;
        private IndicatorsEventArgs _indicators = new IndicatorsEventArgs(-1, -1, false, false, false, false, -1);
        private ManualResetEvent _cmdReadyEvent = new ManualResetEvent(false);
        private object _modemInternalLock = new object();

        private System.Threading.Timer _tmrPeriodicalCheckIndicators = null;
        private System.Threading.Timer _tmrDequeuCommandDelay = null;

        private DateTime _lastTimeIndicatorsUpdated = DateTime.Now;

        public Modem(string comPort = null)
        {
            _modemState = ModemState.Unknown;

            _serialPort = new System.IO.Ports.SerialPort(comPort ?? "COM1", 38400, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            _serialPort.RtsEnable = true;
            _serialPort.DtrEnable = true;
            _serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(DataReceived);
            _serialPort.Open();
        }

        #region Public Properties and Methods

        public ModemState ModemState
        {
            get { return _modemState; }
        }

        public string IMEI
        {
            get;
            private set;
        }

        public string IMSI
        {
            get;
            private set;
        }

        public bool Debug
        {
            get;
            set;
        }

        public void Connect()
        {
            if (_serialPort.IsOpen)
            {
                _modemState = ModemState.Connected;
                lastResponse = null;

                ScheduleCommand(new Commands.EscapeTerminalCommand(null));
                ScheduleCommand(new Commands.CustomCommand("ATE1", null));        // enable command echo
                ScheduleCommand(new Commands.CustomCommand("AT+CMEE=1", null));   // set CMEE
                ScheduleCommand(new Commands.CustomCommand("ATV1", null));        // enable verbose responses
                ScheduleCommand(new Commands.QuerySignalQualityCommand(GetSignalQualityResponseHandler));      // query signal quality
                ScheduleCommand(new Commands.GetTimeCommand(ModemTimeReponseHandler));                         // query modem clock
                ScheduleCommand(new Commands.SetPreferredMessageStorageCommand("ME", "ME", "ME", null));
                ScheduleCommand(new Commands.GetModemIMEICommand(GetIMEIResponseHandler));
                
                if (_tmrPeriodicalCheckIndicators != null)
                    _tmrPeriodicalCheckIndicators.Dispose();
                _tmrPeriodicalCheckIndicators = new System.Threading.Timer((System.Threading.TimerCallback)CheckIndicatorsTimer, null, 100, 5000);
            }
        }

        private List<Command> commandsWaiting = new List<Command>();
        public void ScheduleCommand(Command command)
        {
            lock (_modemInternalLock)
            {
                if (commandsWaiting.Count == 0)
                {
                    lastResponse = null;
                    InternalSendCommand(command);
                }

                commandsWaiting.Add(command);
            }
        }
        
        #endregion

        #region Internal Private Methods

        private void InternalSendCommand(Command cmd)
        {
            if (cmd is IMultipartCommand)
            {
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object usrState)
                {
                    IMultipartCommand multiPartCmd = usrState as IMultipartCommand;
                    int partIndex = 0;
                    while (partIndex < multiPartCmd.PartsCount - 1)
                    {
                        _cmdReadyEvent.Reset();
                        _modemState = Pixelfence.GSM.ModemState.MultipartCmdWait;
                        _serialPort.WriteLine(multiPartCmd.GetPart(partIndex) + multiPartCmd.PartTerminator);
                        _cmdReadyEvent.WaitOne();
                        partIndex++;
                    }
                    // finally, just send the last part without waiting any more..
                    
                    Command currentCommand = null;
                    lock (_modemInternalLock)
                    {
                        if (commandsWaiting.Count > 0)
                        {
                            currentCommand = commandsWaiting[0];
                            if (currentCommand != null && string.Compare(cmd.CommandText, currentCommand.CommandText, true) != 0)
                                currentCommand = null;
                        }
                    }

                    if (currentCommand != null)
                        lastResponse = new ModemResponse(currentCommand);
                    else
                        lastResponse = new ModemResponse(new UnsolicitedModemCommand(multiPartCmd.GetPart(0)));
                    
                    _modemState = Pixelfence.GSM.ModemState.Idle;
                    _serialPort.WriteLine(multiPartCmd.GetPart(partIndex) + multiPartCmd.FinalPartTerminator);
                }, cmd);
            }else
                _serialPort.WriteLine(cmd.CommandText + "\r");
        }
        
        private MemoryStream receiveStream = new MemoryStream();
        private ModemResponse lastResponse = null;
        
        private void DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[_serialPort.BytesToRead];
            _serialPort.Read(buffer, 0, buffer.Length);

            for (int k = 0; k < buffer.Length; k++)
            {
                if (_modemState == Pixelfence.GSM.ModemState.MultipartCmdWait)
                {
                    if (buffer[k] == (byte)'>')
                    {
                        _modemState = Pixelfence.GSM.ModemState.MultipartCmdReady;
                        _cmdReadyEvent.Set();
                    }
                }else
                {
                    if ((buffer[k] != (byte)'\n') && (buffer[k] != (byte)'\r'))
                    {
                        receiveStream.WriteByte(buffer[k]);
                    }
                    else if (buffer[k] == (byte)'\r')
                    {
                        var txt = System.Text.Encoding.ASCII.GetString(receiveStream.ToArray(), 0, (int)receiveStream.Length);
                        receiveStream.Seek(0, SeekOrigin.Begin);
                        receiveStream.SetLength(0);

                        if (txt != string.Empty)
                        {
                            if(Debug)
                                Console.WriteLine(txt);

                            if (lastResponse == null)
                            {
                                if (txt.StartsWith("+++") || txt.StartsWith("AT", true, System.Globalization.CultureInfo.CurrentCulture) || txt.StartsWith("A/", true, System.Globalization.CultureInfo.CurrentCulture))
                                {                                    
                                    Command currentCommand = null;
                                    lock (_modemInternalLock)
                                    {
                                        if (commandsWaiting.Count > 0)
                                        {
                                            currentCommand = commandsWaiting[0];
                                            if (currentCommand != null && string.Compare(txt, currentCommand.CommandText, true) != 0)
                                                currentCommand = null;
                                        }


                                    }
                                    
                                    if (currentCommand != null)
                                        lastResponse = new ModemResponse(currentCommand);
                                    else
                                        lastResponse = new ModemResponse(new UnsolicitedModemCommand(txt));
                                }
                                else
                                {
                                    if (UnsolicitedNotification != null)
                                        UnsolicitedNotification(this, new UnsolicitedNotificationEventArgs(txt));
                                }
                            }
                            else
                            {
                                CommandResult cr = CommandResult.TryParseResponseCode(txt);
                                if (cr != CommandResult.Unknown)
                                    lastResponse.ResultCode = cr;
                                else
                                    lastResponse.AddResponseLine(txt);

                                if (lastResponse.ResultCode != CommandResult.Unknown)
                                {
                                    if (_modemState == ModemState.Connected)
                                    {
                                        _modemState = ModemState.Idle;
                                        if (this.Connected != null)
                                            Connected(this, EventArgs.Empty);
                                    }

                                    lastResponse.Command.SetCompete(lastResponse);

                                    if (lastResponse.Command.NextCommand != null)
                                    {
                                        var priorityCommand = lastResponse.Command.NextCommand;
                                        lock (_modemInternalLock)
                                        {
                                            if (priorityCommand != null)
                                                commandsWaiting.Insert(1, priorityCommand);
                                        }

                                    }
                                    // if the command that was just completed was an 'unsolicited command' try to fire the unsolicited command event
                                    if (lastResponse.Command is UnsolicitedModemCommand && this.ResponseReceived != null)
                                        ResponseReceived(this, new ModemResponseEventArgs(lastResponse));

                                    lastResponse = null;

                                    // Introduce a 100 ms delay before dequeuing and running next command
                                    if (_tmrDequeuCommandDelay != null)
                                        _tmrDequeuCommandDelay.Dispose();
                                    _tmrDequeuCommandDelay = new System.Threading.Timer((System.Threading.TimerCallback)delegate
                                    {
                                        lock (_modemInternalLock)
                                        {
                                            commandsWaiting.RemoveAt(0);
                                                                                        
                                            if (commandsWaiting.Count > 0)
                                            {
                                                lastResponse = null;
                                                Command cmd = commandsWaiting[0];
                                                InternalSendCommand(cmd);                                                
                                            }
                                        }
                                    }, null, 100, 0);
                                }
                            }
                        }
                    }
                }
            }
        }

        private Commands.GetIndicatorsCommand _getIndicatorsCmd = null;
        private int _missedIndicatorsCmdCount = 0;
        private void CheckIndicatorsTimer(object state)
        {
            if (_getIndicatorsCmd == null)
            {
                _getIndicatorsCmd = new Commands.GetIndicatorsCommand(GetIndicatorsResponseHandler);
                ScheduleCommand(_getIndicatorsCmd);
                ScheduleCommand(new Commands.QuerySignalQualityCommand(GetSignalQualityResponseHandler));
            }
            else
            {
                _missedIndicatorsCmdCount++;
                if (_missedIndicatorsCmdCount == 5)
                {
                    if (ModemCommunicationLost != null)
                        ModemCommunicationLost(this, EventArgs.Empty);
                }
            }
        }

        private void GetIMEIResponseHandler(Commands.IMEIModemResponse response)
        {
            IMEI = response.IMEI;
        }

        private void GetIMSIResponseHandler(Commands.SIMIdentityModemResponse response)
        {
            IMSI = response.IMSI;
        }
        
        private void GetIndicatorsResponseHandler(Commands.IndicatorsModemResponse response)
        {
            if (response.Indicators.IsDifferent(_indicators))
            {
                _indicators = response.Indicators;
                try
                {
                    if (IndicatorsChanged != null)
                        IndicatorsChanged(this, _indicators);
                }
                catch { }
                _getIndicatorsCmd = null;
            }
            else
            {                
                if((DateTime.Now - _lastTimeIndicatorsUpdated).TotalMinutes > 5)
                {
                    _lastTimeIndicatorsUpdated = DateTime.Now;
                    try
                    {
                        if (IndicatorsChanged != null)
                            IndicatorsChanged(this, _indicators);
                    }
                    catch { }
                }
                _getIndicatorsCmd = null;
            }

            if (IMSI == null && _indicators.InService)
            {
                ScheduleCommand(new Commands.GetSIMIdentityCommand(GetIMSIResponseHandler));
            }
        }

        private SignalQualityEventArgs _lastSignalQuality = null;
        private void GetSignalQualityResponseHandler(Commands.SignalQualityModemResponse response)
        {
            if (_lastSignalQuality == null || (_lastSignalQuality.SignalStrength != response.SignalQuality.SignalStrength || _lastSignalQuality.BitErrorRate != response.SignalQuality.BitErrorRate))
            {
                _lastSignalQuality = response.SignalQuality;

                try
                {
                    if (SignalQualityChanged != null)
                        SignalQualityChanged(this, response.SignalQuality);
                }
                catch { }
            }

            if (IMSI == null)
            {
                ScheduleCommand(new Commands.GetSIMIdentityCommand(GetIMSIResponseHandler));
            }
        }

        private void ModemTimeReponseHandler(Commands.DateTimeModemResponse response)
        {
            TimeSpan diff = DateTime.Now - response.Time;

            if (diff.TotalSeconds > 10)
            {
                Console.WriteLine("Time difference between modem and host is too great..  Setting modem clock to host clock!");
            }
        }

        public class UnsolicitedModemCommand : Command
        {
            internal UnsolicitedModemCommand(string command)
                : base(command, null)
            {
            }
        }

        #endregion
    }
}
