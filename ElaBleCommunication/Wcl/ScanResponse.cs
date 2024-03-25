using ElaTagClassLibrary.ElaTags.Interoperability;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace ElaBleCommunication.Wcl
{
    public delegate void ScanResponseTimedOut(long address);


    internal class ScanResponse : IDisposable
    {
        public event ScanResponseTimedOut ResponseTimeout;

        public long Address { get; }
        public sbyte Rssi { get; }
        public byte[] OriginalPayload { get; }
        public bool TimedOut { get; private set; }

        private Timer _timer;
        
        
        public ScanResponse(long address, byte[] originalPayload, sbyte rssi)
        {
            Address = address;
            Rssi = rssi;
            OriginalPayload = originalPayload;
            TimedOut = false;

            _timer = new Timer(100) // wait 100 ms for scan response
            {
                AutoReset = false,
                Enabled = true
            };

            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimedOut = true;
            ResponseTimeout?.Invoke(Address);
        }

        public void Dispose()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Dispose();
            _timer = null;
        }
    }
}
