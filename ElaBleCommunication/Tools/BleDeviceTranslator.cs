using ElaTagClassLibrary.ElaTags;
using ElaTagClassLibrary.ElaTags.Interoperability;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace ElaBleCommunication.Tools
{
    public class BleDeviceTranslator
    {
        /**
         * \fn ToInteroperableObject
         * \brief translates an object generated in the Windows implementation to an interoperable object
         * \param [in] windowsObject : device in the windows context
         * \return interoperable object
         */
        public static ElaBaseData ToInteroperableObject(BluetoothLEAdvertisementReceivedEventArgs windowsObject)
        {
            try
            {
                if (windowsObject.Advertisement.DataSections.Count == 0) return null;

                var payload = new List<byte>();

                foreach (BluetoothLEAdvertisementDataSection section in windowsObject.Advertisement.DataSections)
                {
                    payload.Add((byte)(section.Data.Length + 1));
                    payload.Add(section.DataType);

                    var data = new byte[section.Data.Length];
                    using (DataReader reader = DataReader.FromBuffer(section.Data))
                    {
                        reader.ReadBytes(data);
                        payload.AddRange(data);
                    }
                }

                var payloadStr = ElaSoftwareCommon.Tools.ConversionTools.ByteArrayToString(payload.ToArray());

                var interoperableObject = InteroperableDeviceFactory.getInstance().get(ElaTagTechno.Bluetooth, payloadStr);
                if (string.IsNullOrEmpty(interoperableObject.localname)) interoperableObject.localname = windowsObject.Advertisement.LocalName;
                interoperableObject.rssi = windowsObject.RawSignalStrengthInDBm;
                interoperableObject.macaddress = Regex.Replace(string.Format("{0:X}", windowsObject.BluetoothAddress), "([0-9A-F]{2})(?!$)", "$1:");
                interoperableObject.id = interoperableObject.macaddress;
                interoperableObject.timestamp = DateTime.Now.ToString(ElaBaseData.timestampformat, System.Globalization.CultureInfo.InvariantCulture);
                if (interoperableObject.techno == ElaTagTechno.Unknown) interoperableObject.techno = ElaTagTechno.Bluetooth;
                if (interoperableObject.localname.StartsWith("P T")) System.Diagnostics.Debug.WriteLine("coucou");
                return interoperableObject;



                //elaCommonMicroservice.Model.Bluetooth.BleScannedDevice device = new BleScannedDevice();

                //device.rssi = windowsObject.RawSignalStrengthInDBm;
                //device.localname = windowsObject.Advertisement.LocalName;
                //device.macaddress = Regex.Replace(String.Format("{0:X}", windowsObject.BluetoothAddress), "([0-9A-F]{2})(?!$)", "$1:");
                //device.timestamp = DateTime.Now;

                //// update with manufacturer data
                //device.manufacturerData = new List<String>();
                //device.hasManufacturerData = false;
                //if (windowsObject.Advertisement.ManufacturerData.Count > 0)
                //{
                //    device.hasManufacturerData = true;
                //    foreach (BluetoothLEManufacturerData manufacturerD in windowsObject.Advertisement.ManufacturerData)
                //    {
                //        var data = new byte[manufacturerD.Data.Length];
                //        using (var reader = DataReader.FromBuffer(manufacturerD.Data))
                //        {
                //            // Print the company ID + the raw data in hex format
                //            reader.ReadBytes(data);
                //            device.manufacturerData.Add(string.Format("0x{0}: {1}", manufacturerD.CompanyId.ToString("X"), BitConverter.ToString(data)));
                //        }
                //    }
                //}      

                //return device;
            }
            catch             
            {
                return null;
            }
        }
    }
}
