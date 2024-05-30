using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ElaSoftwareCommon.Error;
using wclBluetooth;
using wclCommon;
using ElaBleCommunication.Common;
using ElaBleCommunication.Common.Tools;
using ElaBleCommunication.Wcl.Models;


namespace ElaBleCommunication.Wcl.Controllers
{
    public class WclGattConnection
    {
        private ManualResetEvent _connectedEvent;
        private ManualResetEvent _terminateEvent;
        private ManualResetEvent _initializedEvent;
        private int _initResult;
        private Thread _gattConnectionThread;
        private wclGattClient _client;
        private wclBluetoothRadio _bluetoothRadio;

        public event NotifyResponseReceived ResponseReceived = null;
        private wclGattCharacteristic? _txNordicCharacteristic = null;
        private wclGattCharacteristic? _rxNordicCharacteristic = null;

        private bool _debug = false;

        private string MacAddress { get => _client == null ? "" : MacAddressHelper.macAdressLongToHexa(_client.Address); }

        public WclGattConnection(bool debug)
        {
            _connectedEvent = null;
            _terminateEvent = null;
            _initializedEvent = null;
            _initResult = wclErrors.WCL_E_SUCCESS;
            _gattConnectionThread = null;
            _client = null;
            _bluetoothRadio = null;
            _debug = debug;
        }

        public bool Connect(wclBluetoothRadio radio, long address)
        {
            if (_gattConnectionThread != null) return false;

            bool hasInitializedCorrectly = false;

            try
            {
                _connectedEvent = new ManualResetEvent(false);
                _terminateEvent = new ManualResetEvent(false);
                _initializedEvent = new ManualResetEvent(false);
            }
            catch (Exception ex)
            {
                PrintDebug($"Initialize {nameof(ManualResetEvent)}s failed: {ex.Message}");
            }

            try
            {
                _bluetoothRadio = radio;

                _client = new wclGattClient();
                _client.Address = address;
                _client.OnConnect += Client_OnConnect;
                _client.OnDisconnect += Client_OnDisconnect;
                _client.OnCharacteristicChanged += Client_OnCharacteristicChanged;
                _client.OnMaxPduSizeChanged += Client_OnMaxPduSizeChanged;
                _client.OnConnectionParamsChanged += Client_OnConnectionParamsChanged;
                _client.OnConnectionPhyChanged += Client_OnConnectionPhyChanged;

                _gattConnectionThread = new Thread(ConnectionThread);
                _gattConnectionThread.Start();

                _initializedEvent.WaitOne();
                hasInitializedCorrectly = _initResult == wclErrors.WCL_E_SUCCESS;

                _initializedEvent.Close();
                _initializedEvent = null;
            }
            catch (Exception ex)
            {
                PrintDebug(ex.Message);
            }
            finally
            {
                if (!hasInitializedCorrectly)
                {
                    _connectedEvent.Close();
                    _connectedEvent = null;
                }
            }

            return hasInitializedCorrectly;
        }

        public void Disconnect()
        {
            if (_gattConnectionThread == null) return;

            _terminateEvent.Set();
            _gattConnectionThread.Join();

            _connectedEvent.Close();
            _connectedEvent = null;

            _terminateEvent.Close();
            _terminateEvent = null;

            _initResult = wclErrors.WCL_E_SUCCESS;
            _gattConnectionThread = null;
            _client = null;
            _bluetoothRadio = null;
        }

        private void ConnectionThread()
        {
            _initResult = _client.Connect(_bluetoothRadio);

            if (_initResult == wclErrors.WCL_E_SUCCESS)
            {
                wclMessageBroadcaster.Wait(_connectedEvent);
                ReadServices();
            }

            if (_txNordicCharacteristic == null)
            {
                PrintDebug($"Unable to find NORDIC Tx characteristic");
                _initResult = wclErrors.WCL_E_BASE; // better error code?
            }
            if (_rxNordicCharacteristic == null)
            {
                PrintDebug($"Unable to find NORDIC Rx characteristic");
                _initResult = wclErrors.WCL_E_BASE; // better error code?
            }

            _initializedEvent.Set();

            if (_initResult == wclErrors.WCL_E_SUCCESS)
            {
                wclMessageBroadcaster.Wait(_terminateEvent);
                _client.Disconnect();
            }
        }

        private void ReadServices()
        {
            wclGattService[] Services;
            int res = _client.ReadServices(wclGattOperationFlag.goNone, out Services);

            if (res != wclErrors.WCL_E_SUCCESS)
            {
                PrintDebug($"read services error: 0x{res:X8}", res);
                return;
            }

            if (Services == null || Services.Length == 0)
            {
                PrintDebug($"no services found");
                return;
            }

            PrintDebug($"found {Services.Length} services");

            foreach (wclGattService Service in Services)
            {
                PrintDebug($"\tservice: {UuidToString(Service.Uuid)}");
                ReadCharacteristics(Service);
            }
        }

        private void ReadCharacteristics(wclGattService service)
        {
            wclGattCharacteristic[] characteristics;
            int res = _client.ReadCharacteristics(service, wclGattOperationFlag.goNone, out characteristics);

            if (res != wclErrors.WCL_E_SUCCESS)
            {
                PrintDebug($"\t\tread characteristics error: 0x{res:X8}", res);
                return;
            }

            if (characteristics == null || characteristics.Length == 0)
            {
                PrintDebug($"\t\tno characteristics found");
                return;
            }

            foreach (wclGattCharacteristic characteristic in characteristics)
            {
                PrintDebug($"\t\tcharacteristic: {UuidToString(characteristic.Uuid)}");

                if (_debug)
                {
                    if (characteristic.IsReadable)
                    {
                        PrintDebug("\t\t\treadable");
                        byte[] value;
                        res = _client.ReadCharacteristicValue(characteristic, wclGattOperationFlag.goNone, out value);
                        if (value == null || value.Length == 0)
                            PrintDebug("\t\t\tvalue is empty");
                        else
                            DumpValue(value);
                    }
                }

                if (UuidToString(characteristic.Uuid) == ElaCharacteristics.NORDIC_UART_RX_CHAR)
                {
                    PrintDebug("\t\t\tfound NORDIC Rx characteristic");

                    res = _client.Subscribe(characteristic);
                    if (res != wclErrors.WCL_E_SUCCESS)
                    {
                        PrintDebug($"\t\t\tsubscribe error: 0x{res:X8}", res);
                    }
                    else
                    {
                        PrintDebug("\t\t\tsubscribed");
                        res = _client.WriteClientConfiguration(characteristic, Subscribe: true, wclGattOperationFlag.goNone);
                        if (res != wclErrors.WCL_E_SUCCESS)
                        {
                            PrintDebug($"\t\t\twrite configuration error: 0x{res:X8}", res);
                        }
                        else
                        {
                            PrintDebug("\t\t\twrite configuration completed");
                            _rxNordicCharacteristic = characteristic;
                        }
                    }
                }

                if (UuidToString(characteristic.Uuid) == ElaCharacteristics.NORDIC_UART_TX_CHAR)
                {
                    PrintDebug("\t\t\tfound NORDIC Tx characteristic");
                    _txNordicCharacteristic = characteristic;
                }
            }
        }


        public uint SendCommand(byte[] command)
        {
            if (_client == null) return ErrorServiceHandlerBase.ERR_BLUETOOTH_SEND_COMMAND_ERROR;
            if (!_txNordicCharacteristic.HasValue) return ErrorServiceHandlerBase.ERR_BLUETOOTH_NO_NORDIC_TX_CHAR;

            var res = _client.WriteCharacteristicValue(_txNordicCharacteristic.Value, command);

            if (res == wclErrors.WCL_E_SUCCESS) return ErrorServiceHandlerBase.ERR_OK;

            throw new Exception($"Send error command: {ErrorMessages.Get(res)}");
        }

        private void Client_OnCharacteristicChanged(object sender, ushort handle, byte[] value)
        {
            string valueHexa = "";
            foreach (byte b in value) valueHexa += b.ToString("X2");

            PrintDebug($"value {handle:X4} changed: (ASCII){Encoding.ASCII.GetString(value)} (Hexa){valueHexa}");

            ResponseReceived?.Invoke(value);
        }

        private void Client_OnDisconnect(object Sender, int reason)
        {
            PrintDebug($"disconnected with reason: 0x{reason:X8}", reason);
        }

        private void Client_OnConnect(object Sender, int error)
        {
            _initResult = error;
            if (error == wclErrors.WCL_E_SUCCESS)
            {
                PrintDebug("connected");
                GetMaxPduSize();
                GetConnectionParams();
                GetConnectionPhy();
            }
            else
            {
                PrintDebug($"connect error: 0x{error:X8}", error);
            }

            _connectedEvent.Set();
        }

        #region Debug functions

        private void PrintDebug(string message, int errorCode = wclErrors.WCL_E_SUCCESS)
        {
            if (_debug)
            {
                var macAddress = _client == null ? "" : MacAddressHelper.macAdressLongToHexa(_client.Address);
                var errorMessage = errorCode == wclErrors.WCL_E_SUCCESS ? string.Empty : ErrorMessages.Get(errorCode);
                Console.WriteLine($"[{nameof(WclGattConnection)}][{macAddress}]: {message}. {errorMessage}");
            }
        }

        private void GetMaxPduSize()
        {
            if (_debug)
            {
                int res = _client.GetMaxPduSize(out ushort size);

                if (res != wclErrors.WCL_E_SUCCESS)
                    PrintDebug($"get max PDU size error: 0x{res:X8}", res);
                else
                    PrintDebug($"max PDU size: {size}");
            }
        }

        private void GetConnectionParams()
        {
#if DEBUG
            if (_debug)
            {
                wclBluetoothLeConnectionParameters Params;
                int Res = _client.GetConnectionParams(out Params);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    PrintDebug($"get connection params error: 0x{Res:X8}", Res);
                else
                {
                    PrintDebug("connection params");
                    Console.WriteLine($"\tInterval\t: {Params.Interval}");
                    Console.WriteLine($"\tLatency\t: {Params.Latency}");
                    Console.WriteLine($"\tLink timeout\t: {Params.LinkTimeout}");
                }
            }
#endif
        }

        private void GetConnectionPhy()
        {
#if DEBUG
            if (_debug)
            {
                wclBluetoothLeConnectionPhy Phy;
                int Res = _client.GetConnectionPhyInfo(out Phy);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    PrintDebug($"get connection PHY error: 0x{Res:X8}", Res);
                else
                {
                    PrintDebug("connection PHY");
                    Console.WriteLine("\tReceive");
                    Console.WriteLine("\t\tIsCoded\t: " + Phy.Receive.IsCoded.ToString());
                    Console.WriteLine("\t\tIsUncoded1MPhy\t: " + Phy.Receive.IsUncoded1MPhy.ToString());
                    Console.WriteLine("\t\tIsUncoded2MPhy\t: " + Phy.Receive.IsUncoded2MPhy.ToString());
                    Console.WriteLine("\tTransmit");
                    Console.WriteLine("\t\tIsCoded\t: " + Phy.Transmit.IsCoded.ToString());
                    Console.WriteLine("\t\tIsUncoded1MPhy\t: " + Phy.Transmit.IsUncoded1MPhy.ToString());
                    Console.WriteLine("\t\tIsUncoded2MPhy\t: " + Phy.Transmit.IsUncoded2MPhy.ToString());
                }
            }
#endif
        }

        private void Client_OnMaxPduSizeChanged(object sender, EventArgs e)
        {
            GetMaxPduSize();
        }

        private void Client_OnConnectionParamsChanged(object sender, EventArgs e)
        {
            GetConnectionParams();
        }

        private void Client_OnConnectionPhyChanged(object sender, EventArgs e)
        {
            GetConnectionPhy();
        }
        #endregion

        #region Utils

        private string UuidToString(wclGattUuid Uuid)
        {
            if (Uuid.IsShortUuid)
                return Uuid.ShortUuid.ToString("X4");
            return Uuid.LongUuid.ToString();
        }

        private void DumpValue(byte[] Value)
        {
            if (Value != null && Value.Length > 0)
            {
                string s = "";
                foreach (byte b in Value)
                    s = s + b.ToString("X1");
                PrintDebug("\t\t\tvalue: " + s);
            }
        }

        #endregion
    }
}
