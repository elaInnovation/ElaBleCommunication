using ElaBleCommunication.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

/**
 * \namespace ElaBluetoothCommunication.Model
 * \brief namespace associated to all the model used in library
 */
namespace ElaBleCommunication.Model
{
    /**
     * \class ElaBleDevice
     * \brief map all advertisment information from a Ble device
     */
    public class ElaBleDevice
    {
        /** \brief Apple company id to detect iBeacon */
        private const UInt16 AppleCompanyId = 0x004c;

        /** \brief Eddystone frame identification */
        private const string EddystoneFrameCheck = "AA-FE";

        #region accessors
        /** \brief rssi value */
        public short Rssi { get; set; }
        /** \brief LocalName associated to the tag */
        public string LocalName { get; set; }
        /** \brief bluetooth mac address as a ulong */
        public ulong BluetoothAddress { get; set; }
        /** \brief iBeacon : UUID value */
        public Guid Uuid { get; set; }
        /** \brief iBeacon : Minor value */
        public UInt32 Minor { get; set; }
        /** \brief iBeacon : Major value */
        public UInt32 Major { get; set; }
        public UInt64 LNid { get; set; }
        public UInt64 MNid { get; set; }
        public UInt64 Bid { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string AdvertisementType { get; set; }
        public string Advertisement_flags { get; set; }
        public List<string> LServiceUuids { get; set; }
        public List<String> LManufacturerData { get; set; }
        public List<String> LDataSection { get;  set; }
        #endregion

        /**
         * \brief constructor
         * \param [in] args : argument from bluetooth advertiser
         */
        public ElaBleDevice(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Update(args);
        }

        /** 
         * \fn getiBeaconFromBytes
         * \brief function to get iBeacon from bytes array 
         */
        public void GetIBeaconFromBytes(byte[] bytes)
        {
            if (bytes.Length >= 22)
            {
                //
                if (bytes[0] != 0x02 || bytes[1] != 0x15) return;
                Uuid = new Guid(
                            BitConverter.ToInt32(bytes.Skip(2).Take(4).Reverse().ToArray(), 0),
                            BitConverter.ToInt16(bytes.Skip(6).Take(2).Reverse().ToArray(), 0),
                            BitConverter.ToInt16(bytes.Skip(8).Take(2).Reverse().ToArray(), 0),
                            bytes.Skip(10).Take(8).ToArray());
                Major = BitConverter.ToUInt16(bytes.Skip(18).Take(2).Reverse().ToArray(), 0);
                Minor = BitConverter.ToUInt16(bytes.Skip(20).Take(2).Reverse().ToArray(), 0);
            }
        }


        /** 
         * \fn getFormatedNid
         * \brief getter on the hexa minor or major 
         * \param [in] ui : associated uint
         * \return hexa string
         */
        private String GetFormatedNid(UInt64 lui, UInt64 mui)
        {
            if (0 == lui && 0 == mui) return String.Empty;
            return mui.ToString("X").PadLeft(10, '0') + lui.ToString("X").PadLeft(10, '0');
        }

        /** 
         * \fn getFormatedBid
         * \brief getter on the hexa minor or major 
         * \param [in] ui : associated uint
         * \return hexa string
         */
        private String GetFormatedBid(UInt64 ui)
        {
            if (0 != ui) return ui.ToString("X").PadLeft(12, '0');
            else return String.Empty;
        }

        /** 
         * \fn updateEddystoneUid
         * \brief function to get Eddystone uid based on datas 
         */
        public void UpdateEddystoneUid()
        {
            if (LDataSection.Count > 2
                && LDataSection[2].Length > 64)
            {
                if (LDataSection[2].Substring(0, 5) != EddystoneFrameCheck) return;
                MNid = UInt64.Parse(LDataSection[2].Substring(12, 14).Replace("-", ""), System.Globalization.NumberStyles.HexNumber);
                LNid = UInt64.Parse(LDataSection[2].Substring(27, 14).Replace("-", ""), System.Globalization.NumberStyles.HexNumber);
                Bid = UInt64.Parse(LDataSection[2].Substring(42, 17).Replace("-", ""), System.Globalization.NumberStyles.HexNumber);
                //StrNid = getFormatedNid(LNid, MNid);
                //StrBid = getFormatedBid(Bid);
            }
        }

        /**
         * \fn update
         * \brief update the ElaBleDevice configurtion from arguments from bluetooth advertiser
         * \param [in] args : argument from bluetooth advertiser
         */
        public void Update(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Rssi = args.RawSignalStrengthInDBm;
            LocalName = args.Advertisement.LocalName;
            BluetoothAddress = args.BluetoothAddress;
            Timestamp = args.Timestamp;

            // advertisement datas
            AdvertisementType = args.AdvertisementType.ToString();
            Advertisement_flags = args.Advertisement.Flags.ToString();
            LServiceUuids = new List<string>();
            LServiceUuids.Clear();
            foreach (Guid g in args.Advertisement.ServiceUuids)
            {
                LServiceUuids.Add(g.ToString());
            }

            // update with manufacturer data
            LManufacturerData = new List<string>();
            LManufacturerData.Clear();
            if (args.Advertisement.ManufacturerData.Count > 0)
            {
                string manufacturerDataString = String.Empty;
                foreach (BluetoothLEManufacturerData mmanufacturerD in args.Advertisement.ManufacturerData)
                {
                    var data = new byte[mmanufacturerD.Data.Length];
                    using (var reader = DataReader.FromBuffer(mmanufacturerD.Data))
                    {
                        // Print the company ID + the raw data in hex format
                        reader.ReadBytes(data);
                        LManufacturerData.Add(string.Format("0x{0}: {1}", mmanufacturerD.CompanyId.ToString("X"), BitConverter.ToString(data)));
                        if (mmanufacturerD.CompanyId.Equals(AppleCompanyId))
                        {
                            GetIBeaconFromBytes(data);
                        }
                    }
                }
            }

            // update datasection parameters
            LDataSection = new List<string>();
            LDataSection.Clear();
            if (args.Advertisement.DataSections.Count > 0)
            {
                string datasection = String.Empty;
                foreach (BluetoothLEAdvertisementDataSection section in args.Advertisement.DataSections)
                {
                    var data = new byte[section.Data.Length];
                    using (var reader = DataReader.FromBuffer(section.Data))
                    {
                        reader.ReadBytes(data);
                        datasection = string.Format("{0}", BitConverter.ToString(data));
                        LDataSection.Add(datasection);
                    }
                }
                if (3 == LDataSection.Count
                    && LDataSection[1].ToLower().Equals(EddystoneFrameCheck.ToLower()))
                {
                    UpdateEddystoneUid();
                }
            }
        }

        /**
         * \fn getFormattedData
         */
        public String GetFormattedData()
        {
            return $"{Timestamp.ToString()};" +
                $"{MacAddress.macAdressLongToHexa(BluetoothAddress)};" + 
                $"{LocalName};" +
                $"{Rssi};";
        }
    }
}
