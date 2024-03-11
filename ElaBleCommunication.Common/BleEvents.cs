using ElaTagClassLibrary.ElaTags.Interoperability;


namespace ElaBleCommunication.Common
{
    public delegate void NotifyResponseReceived(byte[] response);
    public delegate void NewAdvertismentReceived(ElaBaseData device);
}
