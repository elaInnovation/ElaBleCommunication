using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ElaSoftwareCommon.Error;
using wclBluetooth;
using wclCommon;
//using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace ElaBleCommunication.Wcl
{
    public class WclGattConnection
    {
        private ManualResetEvent ConnectEvent;
        private ManualResetEvent TerminateEvent;
        private ManualResetEvent InitEvent;
        private int InitResult;
        private Thread GattThread;
        private wclGattClient Client;
        private wclBluetoothRadio BtRadio;

        public event NotifyResponseReceived ResponseReceived = null;
        private wclGattCharacteristic? m_TxNordicCharacteristic = null;
        private wclGattCharacteristic? m_RxNordicCharacteristic = null;

        public WclGattConnection()
        {
            ConnectEvent = null;
            TerminateEvent = null;
            InitEvent = null;
            InitResult = wclErrors.WCL_E_SUCCESS;
            GattThread = null;
            Client = null;
            BtRadio = null;
        }

        public bool Connect(wclBluetoothRadio Radio, long Address)
        {
            if (GattThread != null)
                return false;

            bool result = false;

            try { ConnectEvent = new ManualResetEvent(false); } catch { ConnectEvent = null; }
            if (ConnectEvent == null)
                Console.WriteLine("[" + Address.ToString("X12") + "]: Create connection event failed");
            else
            {
                try { TerminateEvent = new ManualResetEvent(false); } catch { TerminateEvent = null; }
                if (TerminateEvent == null)
                    Console.WriteLine("[" + Address.ToString("X12") + "]: Create termination event failed");
                else
                {
                    try { InitEvent = new ManualResetEvent(false); } catch { InitEvent = null; }
                    if (InitEvent == null)
                        Console.WriteLine("[" + Address.ToString("X12") + "]: Create initialization event failed");
                    else
                    {
                        try { GattThread = new Thread(ConnectionThread); } catch { GattThread = null; }
                        if (GattThread == null)
                            Console.WriteLine("[" + Address.ToString("X12") + "]: Create communication thread failed");
                        else
                        {
                            BtRadio = Radio;

                            Client = new wclGattClient();
                            Client.Address = Address;
                            Client.OnConnect += Client_OnConnect;
                            Client.OnDisconnect += Client_OnDisconnect;
                            Client.OnCharacteristicChanged += Client_OnCharacteristicChanged;
                            Client.OnMaxPduSizeChanged += Client_OnMaxPduSizeChanged;
                            Client.OnConnectionParamsChanged += Client_OnConnectionParamsChanged;
                            Client.OnConnectionPhyChanged += Client_OnConnectionPhyChanged;

                            InitResult = wclErrors.WCL_E_SUCCESS;
                            GattThread.Start();

                            InitEvent.WaitOne();
                            result = InitResult == wclErrors.WCL_E_SUCCESS;
                            //if (!Result)
                            //{
                            //    GattThread.Join();
                            //    GattThread = null;
                            //    Client = null;
                            //    BtRadio = null;
                            //}
                        }

                        InitEvent.Close();
                        InitEvent = null;
                    }

                    //if (!Result)
                    //{
                    //    TerminateEvent.Close();
                    //    TerminateEvent = null;
                    //}
                }

                if (!result)
                {
                    ConnectEvent.Close();
                    ConnectEvent = null;
                }
            }

            return result;
        }

        public void Disconnect()
        {
            if (GattThread != null)
            {
                TerminateEvent.Set();
                GattThread.Join();

                ConnectEvent.Close();
                ConnectEvent = null;

                TerminateEvent.Close();
                TerminateEvent = null;

                InitResult = wclErrors.WCL_E_SUCCESS;
                GattThread = null;
                Client = null;
                BtRadio = null;
            }
        }

        private void ConnectionThread()
        {
            InitResult = Client.Connect(BtRadio);
            if (InitResult == wclErrors.WCL_E_SUCCESS)
                wclMessageBroadcaster.Wait(ConnectEvent);

            if (InitResult == wclErrors.WCL_E_SUCCESS)
                ReadServices();

            InitEvent.Set();

            if (InitResult == wclErrors.WCL_E_SUCCESS)
            {
                wclMessageBroadcaster.Wait(TerminateEvent);
                Client.Disconnect();
            }
        }

        private void ReadCharacteristics(wclGattService Service)
        {
            wclGattCharacteristic? writeChar = null;

            wclGattCharacteristic[] Characteristics;
            int Res = Client.ReadCharacteristics(Service, wclGattOperationFlag.goNone, out Characteristics);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: read characteristics error: 0x" + Res.ToString("X8"));
            else
            {
                if (Characteristics == null || Characteristics.Length == 0)
                    Console.WriteLine("[" + Client.Address.ToString("X12") + "]: no characteristics found");
                else
                {
                    foreach (wclGattCharacteristic Characteristic in Characteristics)
                    {
                        Console.WriteLine("[" + Client.Address.ToString("X12") + "]: characteristic: " + UuidToString(Characteristic.Uuid));
                        if (Characteristic.IsReadable)
                        {
                            Console.WriteLine("[" + Client.Address.ToString("X12") + "]: readable");
                            byte[] Value;
                            Res = Client.ReadCharacteristicValue(Characteristic, wclGattOperationFlag.goNone, out Value);
                            if (Value == null || Value.Length == 0)
                                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: value is empty");
                            else
                                DumpValue(Value);
                        }

                        if (UuidToString(Characteristic.Uuid) == ElaCharacteristics.NORDIC_UART_RX_CHAR)
                        {
                            if (Characteristic.IsIndicatable || Characteristic.IsNotifiable)
                            {
                                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: notifiable (or indicatable)");
                            }

                            Res = Client.Subscribe(Characteristic);
                            if (Res != wclErrors.WCL_E_SUCCESS)
                                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: subscribe error: 0x" + Res.ToString("X8"));
                            else
                            {
                                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: subscribed");
                                Res = Client.WriteClientConfiguration(Characteristic, true, wclGattOperationFlag.goNone);
                                if (Res != wclErrors.WCL_E_SUCCESS)
                                {
                                    Console.WriteLine("[" + Client.Address.ToString("X12") + "]: write configuration error: 0x" + Res.ToString("X8"));
                                    m_RxNordicCharacteristic = Characteristic;
                                }
                                else
                                {
                                    Console.WriteLine("[" + Client.Address.ToString("X12") + "]: write configuration completed");
                                }
                            }
                        }

                        if (Characteristic.IsWritableWithoutResponse)
                        {
                            Console.WriteLine($"Found writable char {UuidToString(Characteristic.Uuid)}");
                        }

                        if (UuidToString(Characteristic.Uuid) == ElaCharacteristics.NORDIC_UART_TX_CHAR) m_TxNordicCharacteristic = Characteristic;
                    }
                }
            }
        }

        private void ReadServices()
        {
            wclGattService[] Services;
            int Res = Client.ReadServices(wclGattOperationFlag.goNone, out Services);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: read services error: 0x" + Res.ToString("X8"));
            else
            {
                if (Services == null || Services.Length == 0)
                    Console.WriteLine("[" + Client.Address.ToString("X12") + "]: no services found");
                else
                {
                    Console.WriteLine("[" + Client.Address.ToString("X12") + "]: found " + Services.Length + " services");
                    foreach (wclGattService Service in Services)
                    {
                        Console.WriteLine("[" + Client.Address.ToString("X12") + "]: service: " + UuidToString(Service.Uuid));
                        ReadCharacteristics(Service);
                    }
                }
            }
        }

        public uint SendCommand(byte[] command)
        {
            if (Client == null) return ErrorServiceHandlerBase.ERR_BLUETOOTH_SEND_COMMAND_ERROR;
            if (m_TxNordicCharacteristic == null) return ErrorServiceHandlerBase.ERR_BLUETOOTH_NO_NORDIC_TX_CHAR;

            var res = Client.WriteCharacteristicValue(m_TxNordicCharacteristic.Value, command);

            return res == wclErrors.WCL_E_SUCCESS ? ErrorServiceHandlerBase.ERR_OK : ErrorServiceHandlerBase.ERR_BLUETOOTH_SEND_COMMAND_ERROR;
        }

        private void GetMaxPduSize()
        {
            ushort Size;
            int Res = Client.GetMaxPduSize(out Size);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: Get max PDU size error: 0x" + Res.ToString("X8"));
            else
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: Max PDU size: " + Size.ToString());
        }

        private void GetConnectionParams()
        {
            wclBluetoothLeConnectionParameters Params;
            int Res = Client.GetConnectionParams(out Params);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: Get connection params error: 0x" + Res.ToString("X8"));
            else
            {
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: connection params");
                Console.WriteLine("  Interval     : " + Params.Interval.ToString());
                Console.WriteLine("  Latency      : " + Params.Latency.ToString());
                Console.WriteLine("  Link timeout : " + Params.LinkTimeout.ToString());
            }
        }

        private void GetConnectionPhy()
        {
            wclBluetoothLeConnectionPhy Phy;
            int Res = Client.GetConnectionPhyInfo(out Phy);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: Get connection PHY error: 0x" + Res.ToString("X8"));
            else
            {
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: connection PHY");
                Console.WriteLine("  Receive");
                Console.WriteLine("    IsCoded        : " + Phy.Receive.IsCoded.ToString());
                Console.WriteLine("    IsUncoded1MPhy : " + Phy.Receive.IsUncoded1MPhy.ToString());
                Console.WriteLine("    IsUncoded2MPhy : " + Phy.Receive.IsUncoded2MPhy.ToString());
                Console.WriteLine("  Transmit");
                Console.WriteLine("    IsCoded        : " + Phy.Transmit.IsCoded.ToString());
                Console.WriteLine("    IsUncoded1MPhy : " + Phy.Transmit.IsUncoded1MPhy.ToString());
                Console.WriteLine("    IsUncoded2MPhy : " + Phy.Transmit.IsUncoded2MPhy.ToString());
            }
        }

        private void Client_OnCharacteristicChanged(object Sender, ushort Handle, byte[] Value)
        {
            string value = "";
            foreach (byte b in Value) value += b.ToString("X2");
            value = Encoding.ASCII.GetString(Value);
            Console.WriteLine("[" + Client.Address.ToString("X12") + "]: value " + Handle.ToString("X4") + " changed. Value: " + value);
            ResponseReceived?.Invoke(Value);
        }

        private void Client_OnDisconnect(object Sender, int Reason)
        {
            Console.WriteLine("[" + Client.Address.ToString("X12") + "]: disconnected with reason: 0x" + Reason.ToString("X8"));
        }

        private void Client_OnConnect(object Sender, int Error)
        {
            InitResult = Error;
            if (Error == wclErrors.WCL_E_SUCCESS)
            {
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: connected");
                GetMaxPduSize();
                GetConnectionParams();
                GetConnectionPhy();
            }
            else
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: connect error: 0x" + Error.ToString("X8"));

            ConnectEvent.Set();
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
                Console.WriteLine("[" + Client.Address.ToString("X12") + "]: value: " + s);
            }
        }
    }
}
