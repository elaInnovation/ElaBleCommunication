using ElaTagClassLibrary.ElaTags.Interoperability;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElaBleCommunication
{
    public delegate void NotifyResponseReceived(byte[] response);
    public delegate void NewAdvertismentReceived(ElaBaseData device);
}
