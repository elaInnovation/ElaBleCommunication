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
namespace ElaBluetoothCommunication.Model
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
        private const String EddystoneFrameCheck = "AA-FE";

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
            update(args);
        }

        /** 
         * \fn getiBeaconFromBytes
         * \brief function to get iBeacon from bytes array 
         */
        public void getiBeaconFromBytes(byte[] bytes)
        {
            if (bytes.Length >= 22)
            {
                //
                if (bytes[0] != 0x02 || bytes[1] != 0x15) return;
                this.Uuid = new Guid(
                            BitConverter.ToInt32(bytes.Skip(2).Take(4).Reverse().ToArray(), 0),
                            BitConverter.ToInt16(bytes.Skip(6).Take(2).Reverse().ToArray(), 0),
                            BitConverter.ToInt16(bytes.Skip(8).Take(2).Reverse().ToArray(), 0),
                            bytes.Skip(10).Take(8).ToArray());
                this.Major = BitConverter.ToUInt16(bytes.Skip(18).Take(2).Reverse().ToArray(), 0);
                this.Minor = BitConverter.ToUInt16(bytes.Skip(20).Take(2).Reverse().ToArray(), 0);
            }
        }


        /** 
         * \fn getFormatedNid
         * \brief getter on the hexa minor or major 
         * \param [in] ui : associated uint
         * \return hexa string
         */
        private String getFormatedNid(UInt64 lui, UInt64 mui)
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
        private String getFormatedBid(UInt64 ui)
        {
            if (0 != ui) return ui.ToString("X").PadLeft(12, '0');
            else return String.Empty;
        }

        /** 
         * \fn updateEddystoneUid
         * \brief function to get Eddystone uid based on datas 
         */
        public void updateEddystoneUid()
        {
            if (this.LDataSection.Count > 2
                && this.LDataSection[2].Length > 64)
            {
                if (this.LDataSection[2].Substring(0, 5) != EddystoneFrameCheck) return;
                this.MNid = UInt64.Parse(this.LDataSection[2].Substring(12, 14).Replace("-", ""), System.Globalization.NumberStyles.HexNumber);
                this.LNid = UInt64.Parse(this.LDataSection[2].Substring(27, 14).Replace("-", ""), System.Globalization.NumberStyles.HexNumber);
                this.Bid = UInt64.Parse(this.LDataSection[2].Substring(42, 17).Replace("-", ""), System.Globalization.NumberStyles.HexNumber);
                //this.StrNid = getFormatedNid(this.LNid, this.MNid);
                //this.StrBid = getFormatedBid(this.Bid);
            }
        }

        /**
         * \fn update
         * \brief update the ElaBleDevice configurtion from arguments from bluetooth advertiser
         * \param [in] args : argument from bluetooth advertiser
         */
        public void update(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            this.Rssi = args.RawSignalStrengthInDBm;
            this.LocalName = args.Advertisement.LocalName;
            this.BluetoothAddress = args.BluetoothAddress;
            this.Timestamp = args.Timestamp;

            // advertisement datas
            this.AdvertisementType = args.AdvertisementType.ToString();
            this.Advertisement_flags = args.Advertisement.Flags.ToString();
            this.LServiceUuids = new List<string>();
            this.LServiceUuids.Clear();
            foreach (Guid g in args.Advertisement.ServiceUuids)
            {
                this.LServiceUuids.Add(g.ToString());
            }

            // update with manufacturer data
            this.LManufacturerData = new List<string>();
            this.LManufacturerData.Clear();
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
                        this.LManufacturerData.Add(string.Format("0x{0}: {1}", mmanufacturerD.CompanyId.ToString("X"), BitConverter.ToString(data)));
                        if (mmanufacturerD.CompanyId.Equals(AppleCompanyId))
                        {
                            this.getiBeaconFromBytes(data);
                        }
                    }
                }
            }

            // update datasection parameters
            this.LDataSection = new List<string>();
            this.LDataSection.Clear();
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
                        this.LDataSection.Add(datasection);
                    }
                }
                if (3 == this.LDataSection.Count
                    && this.LDataSection[1].ToLower().Equals(EddystoneFrameCheck.ToLower()))
                {
                    this.updateEddystoneUid();
                }
            }
        }

        /**
         * \fn getFormattedData
         */
        public String getFormattedData()
        {
            return $"{this.Timestamp.ToString()};" +
                $"{MacAddress.macAdressLongToHexa(this.BluetoothAddress)};" + 
                $"{this.LocalName};" +
                $"{this.Rssi};";
        }
    }
}
