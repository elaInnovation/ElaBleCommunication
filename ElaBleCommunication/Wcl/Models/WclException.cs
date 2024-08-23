using System;

namespace ElaBleCommunication.Wcl.Models
{
    public class WclException : Exception
    {
        public int WclErrorCode { get; }

        public WclException(string message) : base(message) { }

        public WclException(int wclErrorCode) : base(ErrorMessages.Get(wclErrorCode))
        {
            WclErrorCode = wclErrorCode;
        }

        public WclException(int wclErrorCode, string message) : base($"{message}: 0x{wclErrorCode:X8} {ErrorMessages.Get(wclErrorCode)}")
        {
            WclErrorCode = wclErrorCode;
        }
    }
}
