using ElaBleCommunication.Common.Error;
using ElaBleCommunication.Common;
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
using System.Linq;


namespace ElaBleCommunication.Wcl
{
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
        private AutoResetEvent _initializedFlag;
        private AutoResetEvent _stopScanFlag;

        private object _ScanResponsesLock = new object();
        private Dictionary<long, ScanResponse> _scanResponses = new Dictionary<long, ScanResponse>();

        public bool IsScanning { get => _isStarted; }

        public WclBLEScanner(wclBluetoothRadio radio)
        {
            _radio = radio;
            _wclBluetoothLeBeaconWatcher = new wclBluetoothLeBeaconWatcher();
            _wclBluetoothLeBeaconWatcher.OnAdvertisementReceived += M_WclBluetoothLeBeaconWatcher_OnAdvertisementReceived;
            _wclBluetoothLeBeaconWatcher.OnAdvertisementRawFrame += M_WclBluetoothLeBeaconWatcher_OnAdvertisementRawFrame;
        }

        public uint Start(bool withScanResponse = false, ushort interval = 189, ushort window = 29)
        {
            if (_isStarted)
            {
                if (interval == _interval && window == _window && ((withScanResponse && _mode == wclBluetoothLeScanningMode.smActive) || (!withScanResponse && _mode == wclBluetoothLeScanningMode.smPassive)))
                {
                    return ErrorServiceHandlerBase.ERR_OK;
                }
                else
                {
                    Stop();
                }
            }

            if (withScanResponse)
            {
                _mode = wclBluetoothLeScanningMode.smActive;
                ClearResponses();
            }
            else
            {
                _mode = wclBluetoothLeScanningMode.smPassive;
            }
                
            _interval = interval;
            _window = window;

            _isStarted = false;
            _stopScanFlag = new AutoResetEvent(false);
            _initializedFlag = new AutoResetEvent(false);

            _scanningThread = new Thread(ScanBle);
            _scanningThread.Start();

            wclMessageBroadcaster.Wait(_initializedFlag);
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
                _initializedFlag.Set();
                wclMessageBroadcaster.Wait(_stopScanFlag);
                _wclBluetoothLeBeaconWatcher.Stop();
                return;
            }

            _initializedFlag.Set();
        }

        public uint Stop()
        {
            try
            {
                if (!_isStarted) return ErrorServiceHandlerBase.ERR_OK;

                _stopScanFlag.Set();
                _scanningThread.Join();
                _scanningThread = null;

                ClearResponses();

                _isStarted = false;

                return ErrorServiceHandlerBase.ERR_OK;
            }
            catch (Exception e)
            {
                throw new ElaBleException("Exception while trying to stop Bluetooth scanner.", e);
            }
        }

        private void ClearResponses()
        {
            lock (_ScanResponsesLock)
            {
                foreach (var response in _scanResponses.Values)
                {
                    response.ResponseTimeout -= ScanResponse_ResponseTimeout;
                    response.Dispose();
                }
                _scanResponses.Clear();
            }
        }

        private void M_WclBluetoothLeBeaconWatcher_OnAdvertisementRawFrame(object Sender, long Address, long Timestamp, sbyte Rssi, byte DataType, byte[] Data)
        {
            if (DataType == (byte)wclBluetoothLeAdvertisementType.atScanResponse)
            {
                Debug(Address, $"Received scan response for {Address}");
                lock (_ScanResponsesLock)
                {
                    if (_scanResponses.ContainsKey(Address))
                    {
                        var scanResponse = _scanResponses[Address];
                        ParseAdvertisement(Address, Rssi, scanResponse.OriginalPayload.Concat(Data).ToArray());
                        _scanResponses[Address].ResponseTimeout -= ScanResponse_ResponseTimeout;
                        _scanResponses[Address].Dispose();
                        _scanResponses.Remove(Address);
                        Debug(Address, $"Concatenate payloads for {Address}");
                    }
                    else
                    {
                        Debug(Address, $"No ScanResponse for {Address}: already timed out");
                    }
                }
            }
        }

        private void M_WclBluetoothLeBeaconWatcher_OnAdvertisementReceived(object Sender, long Address, long Timestamp, sbyte Rssi, byte[] Data)
        {
            if (_mode == wclBluetoothLeScanningMode.smActive)
            {
                lock (_ScanResponsesLock)
                {
                    if (!_scanResponses.ContainsKey(Address))
                    {
                        var scanResponse = new ScanResponse(Address, Data, Rssi);
                        _scanResponses.Add(Address, scanResponse);
                        scanResponse.ResponseTimeout += ScanResponse_ResponseTimeout;
                        Debug(Address, $"Start waiting for scan response from {Address}");
                    }
                    else
                    {
                        ParseAdvertisement(Address, Rssi, Data);
                    }
                }
            }
            else
            {
                ParseAdvertisement(Address, Rssi, Data);
            }
        }

        private void ScanResponse_ResponseTimeout(long address)
        {
            lock (_ScanResponsesLock)
            {
                if (_scanResponses.ContainsKey(address))
                {
                    var scanResponse = _scanResponses[address];

                    ParseAdvertisement(scanResponse.Address, scanResponse.Rssi, scanResponse.OriginalPayload);

                    scanResponse.ResponseTimeout -= ScanResponse_ResponseTimeout;
                    scanResponse.Dispose();
                    _scanResponses.Remove(address);

                    Debug(address, $"Timed out while waiting for scan response from {address}");
                }
                else
                {
                    Debug(address, $"ScanResponse {address} timed out but object not in dict: scan response correctly concatenated");
                }
            }
        }

        private const string _regexMac = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
        private const string _regexReplaceMac = "$1:$2:$3:$4:$5:$6";

        private void ParseAdvertisement(long Address, sbyte Rssi, byte[] Data)
        {
            try
            {
                string macAddress = Regex.Replace(string.Format("{0:X}", Address), _regexMac, _regexReplaceMac);

                string payload = "";
                foreach (byte b in Data) payload += b.ToString("X2");
                var data = InteroperableDeviceFactory.getInstance().get(ElaTagTechno.Bluetooth, payload);
                
                data.id = macAddress;
                data.rssi = Rssi;
                if (data.identification == null) data.identification = new ElaIdenficationObject();
                data.identification.macaddress = macAddress;
                data.version = ElaModelVersion.get();
                data.payload = payload;

                evAdvertisementReceived?.Invoke(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{nameof(WclBLEScanner)}][{nameof(ParseAdvertisement)}] Error while parsing received advertisement frame: {ex.Message}");
            }
        }

        private void Debug(long address, string message) 
        {
#if DEBUG
            Console.WriteLine(message);
#endif
        }
    }
}
