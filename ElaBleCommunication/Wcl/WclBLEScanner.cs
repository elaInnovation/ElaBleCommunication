using ElaBleCommunication.Error;
//using ElaBleCommunication.Model;
using ElaBleCommunication.Tools;
using ElaSoftwareCommon.Error;
using ElaTagClassLibrary.ElaTags;
using ElaTagClassLibrary.ElaTags.Interoperability;
using ElaTagClassLibrary.ElaTags.Interoperability.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using wclBluetooth;
using wclCommon;

/**
 * \namespace ElaBleCommunication
 * \brief namespace associated to the Bluetooth configuration
 */
namespace ElaBleCommunication.Wcl
{

    /**
     * \class WclBLEAdvertisementWatcher
     * \brief use ElaBLEAdvertisementWatcher to scan and get data from Bluetooth Advertising
     */
    public class WclBLEScanner
    {
        /** \brief event for new advertisement */
        public event NewAdvertismentReceived evAdvertisementReceived = null;

        private readonly wclBluetoothRadio _radio;

        private wclBluetoothLeBeaconWatcher _wclBluetoothLeBeaconWatcher;
        private bool _isStarted = false;

        private wclBluetoothLeScanningMode _mode;
        private ushort _interval = 189;
        private ushort _window = 29;

        private Thread _scanningThread;
        private AutoResetEvent _initFlag;
        private AutoResetEvent _stopScanFlag;

        public WclBLEScanner(wclBluetoothRadio radio)
        {
            _radio = radio;
            _wclBluetoothLeBeaconWatcher = new wclBluetoothLeBeaconWatcher();
            _wclBluetoothLeBeaconWatcher.OnAdvertisementReceived += M_WclBluetoothLeBeaconWatcher_OnAdvertisementReceived;
            _wclBluetoothLeBeaconWatcher.OnAdvertisementRawFrame += M_WclBluetoothLeBeaconWatcher_OnAdvertisementRawFrame;
        }

        public uint Start(bool withScanResponse = false, ushort interval = 189, ushort window = 29)
        {
            _mode = withScanResponse ? wclBluetoothLeScanningMode.smActive : wclBluetoothLeScanningMode.smPassive;
            _interval = interval;
            _window = window;

            _isStarted = false;
            _stopScanFlag = new AutoResetEvent(false);
            _initFlag = new AutoResetEvent(false);

            _scanningThread = new Thread(ScanBle);
            _scanningThread.Start();

            wclMessageBroadcaster.Wait(_initFlag);
            if (_isStarted)
            {
                return ErrorServiceHandlerBase.ERR_OK;
            }
            else
            {
                _scanningThread.Join();
                _scanningThread = null;
                return ErrorServiceHandlerBase.ERR_BLUETOOTH_START_SCANNER_FAILED;
            }
        }

        private void ScanBle()
        {
            var result = _wclBluetoothLeBeaconWatcher.Start(_radio, _mode, _interval, _window);
            if (result == wclErrors.WCL_E_SUCCESS)
            {
                _isStarted = true;
                _initFlag.Set();
                wclMessageBroadcaster.Wait(_stopScanFlag);
                _wclBluetoothLeBeaconWatcher.Stop();
            }

            _initFlag.Set();
        }

        /**
         * \fn stopBluetoothScanner
         * \brief stop the bluetooth scanner
         * \return error code :
         *      + ERR_SCANNER_ALREADY_STOPPED
         *      + ERR_OK
         */
        public uint Stop()
        {
            try
            {
                if (!_isStarted) return ErrorServiceHandlerBase.ERR_OK;

                _stopScanFlag.Set();
                _scanningThread.Join();
                _scanningThread = null;

                _isStarted = false;

                return ErrorServiceHandlerBase.ERR_OK;
            }
            catch (Exception e)
            {
                throw new ElaBleException("Exception while trying to stop Bluetooth scanner.", e);
            }
        }

        private void M_WclBluetoothLeBeaconWatcher_OnAdvertisementRawFrame(object Sender, long Address, long Timestamp, sbyte Rssi, byte DataType, byte[] Data)
        {
            ParseAdvertisement(Address, Rssi, Data);
        }

        private void M_WclBluetoothLeBeaconWatcher_OnAdvertisementReceived(object Sender, long Address, long Timestamp, sbyte Rssi, byte[] Data)
        {
            ParseAdvertisement(Address, Rssi, Data);
        }

        private const string _regexMac = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
        private const string _regexReplaceMac = "$1:$2:$3:$4:$5:$6";

        private void ParseAdvertisement(long Address, sbyte Rssi, byte[] Data)
        {         
            string macAddress = Regex.Replace(string.Format("{0:X}", Address), _regexMac, _regexReplaceMac);

            string payload = "";
            foreach (byte b in Data) payload += b.ToString("X2");
            var data = InteroperableDeviceFactory.getInstance().get(ElaTagTechno.Bluetooth, payload);

            data.id = macAddress;
            data.rssi = Rssi;
            if (data.identification is null) data.identification = new ElaIdenficationObject();
            data.identification.macaddress = data.id;
            data.version = ElaModelVersion.get();
            data.payload = payload;

            evAdvertisementReceived?.Invoke(data);
        }
    }
}
