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
using System.Threading;

namespace Pixelfence.GSM
{

    /// <summary>
    /// The base class to all modem commands, this class provides the common basic commands functionality 
    /// such as the ability to query whether the command has finished executing (IsComplete); to get the
    /// ModemResponse object returned from the modem (Response property); and to asynchronously wait for 
    /// the command to finish executing using the WaitOne method, or the WaitHandle property. The 
    /// CommandText property provides the original command text that was sent to the modem.
    /// </summary>
    /// <remarks>
    /// The NextCommand property may be used to chain multiple commands together so that their execution
    /// is guaranteed to be sequential. This behavior ensures that the "next command" specified by the 
    /// property will be executed as soon as this command completes execution, after the Response object
    /// has been set and the responseHandler delegate has been invoked (if set). It is important to note 
    /// that a very long "cycle" of commands that have their NextCommand property set will cause all other
    /// command scheduled with ScheduleCommand to be delayed untill the last command in the cycle (which has
    /// it's NextCommand property set to null) is completed.
    /// </remarks>
    public abstract class Command
    {
        #region Private and Protected members of the Command class

        private Delegate _responseHandler;
        private Lazy<ManualResetEvent> _cmdCompleteMRE;

        protected Command(string command, Delegate responseHandler)
        {
            _responseHandler = responseHandler;
            _cmdCompleteMRE = new Lazy<ManualResetEvent>(InitializeCmdCompleteMRE, LazyThreadSafetyMode.PublicationOnly);
            CommandText = command;
        }

        private ManualResetEvent InitializeCmdCompleteMRE()
        {
            return new ManualResetEvent(IsComplete);
        }

        /// <summary>
        /// Derived classes may choose to override this method to provide a more specialized implementation for
        /// the method to produce a more specialized (i.e. derived) ModemResponse class specific to the 
        /// derived command class.
        /// </summary>
        /// <param name="responsePrototype">The raw modem response received from the modem. This object may
        /// be further parsed and interpreted in the overriden implementaion to produce a specialized response
        /// or could be returned directly if no more processing is required.</param>
        /// <returns>A ModemResponse (or derived) class based on the response received from the modem.</returns>
        protected virtual ModemResponse CreateResponse(ModemResponse responsePrototype)
        {
            return responsePrototype;
        }

        /// <summary>
        /// This method should not be invoked by any client code. It is called from the Modem class once a 
        /// response has been received for this command from the hardware modem.
        /// </summary>
        /// <param name="responsePrototype">A ModemResponse object that represents the raw response data 
        /// received from the modem.</param>
        internal void SetCompete(ModemResponse responsePrototype)
        {
            IsComplete = true;
            Response = CreateResponse(responsePrototype);

            if (_responseHandler != null)
            {
                try
                {
                    _responseHandler.DynamicInvoke(Response);
                }
                catch { }
            }

            if (_cmdCompleteMRE.IsValueCreated)
            {
                _cmdCompleteMRE.Value.Set();
            }
        }

        #endregion


        #region Public members

        /// <summary>
        /// The actual text of the command that was sent to the modem. This may be used by more advanced users
        /// but otherwise should be set by derived Command classes in their constructor. This property needs to
        /// be set before the Command object is sent to the Modem class to be executed.
        /// </summary>
        public string CommandText
        {
            get;
            protected set;
        }

        /// <summary>
        /// A boolean flag which indicates whether the command has been fully sent to the modem, and 
        /// whether the modem responded to the command. When the modem sends back a response to the
        /// command, the falg is automatically set to TRUE by the Modem class.
        /// </summary>
        public bool IsComplete
        {
            get;
            private set;
        }        

        /// <summary>
        /// After the command is fully sent to the modem, the Modem class receives the modem response
        /// and parses it into an appropriate ModemResponse class which is stored in this property.
        /// The property may be "shadowed" in derived Command classes to provide specialized responses.
        /// </summary>
        public ModemResponse Response
        {
            get;
            private set;
        }        

        /// <summary>
        /// The NextCommand property may be used to chain multiple commands together so that their execution
        /// is guaranteed to be sequential. This behavior ensures that the "next command" specified by the 
        /// property will be executed as soon as this command completes execution, after the Response object
        /// has been set and the responseHandler delegate has been invoked (if set).
        /// </summary>
        /// <remarks>
        /// Note that a very long "cycle" of commands that have their NextCommand property set will cause all 
        /// other commands scheduled with ScheduleCommand to be delayed untill the last command in the 
        /// cycle (which has it's NextCommand property set to null) is completed.
        /// </remarks>        
        public Command NextCommand
        {
            get;
            set;
        }

        /// <summary>
        /// The WaitHandle of the command may be used to asynchronously wait for the command to be completed.
        /// The handle will automactically be signaled once the command was sent to the modem, and the modem
        /// has responded to the command.
        /// </summary>
        public WaitHandle WaitHandle
        {
            get
            {
                return (WaitHandle)_cmdCompleteMRE.Value;
            }
        }

        /// <summary>
        /// This method may be used to block the current thread's execution until the command is fully executed
        /// and a response is readily available from the modem.
        /// </summary>
        public void WaitOne()
        {
            _cmdCompleteMRE.Value.WaitOne();
        }

        /// <summary>
        /// This method may be used to block the current thread's execution until the command is fully executed
        /// and a response is readily available from the modem, or the number of milliseconds specified have 
        /// passed.
        /// </summary>
        /// /// <param name="millisecondsTimeout">The amout of time in milliseconds to wait for the command to complete.</param>
        public void WaitOne(int millisecondsTimeout)
        {
            _cmdCompleteMRE.Value.WaitOne(millisecondsTimeout);
        }

        /// <summary>
        /// This method may be used to block the current thread's execution until the command is fully executed
        /// and a response is readily available from the modem, or the amout of time specified by <paramref name="timeout"/>
        /// has passed.
        /// </summary>
        /// <param name="timeout">The amout of time to wait for the command to complete.</param>
        public void WaitOne(TimeSpan timeout)
        {
            _cmdCompleteMRE.Value.WaitOne(timeout);
        }

        #endregion
    }
}
