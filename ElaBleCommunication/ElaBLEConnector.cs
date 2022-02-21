using ElaBleCommunication.Error;
using ElaBleCommunication.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using ElaSoftwareCommon.Error;


/**
 * \namespace ElaBluetoothCommunication
 * \brief namespace associated to the Bluetooth configuration
 */
namespace ElaBleCommunication
{
    /** \brief notify that a new message has been received from tag */
    public delegate void NotifyResponseReceived(String response);

    /**
     * \class ElaBLEConnector
     * \brief use nordic UART to connect to ELA Bluetooth tags
     */
    public class ElaBLEConnector
    {
        // nordic uart service
        public const string NORDIC_UART_SERVICE = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
        public const string NORDIC_UART_TX_CHAR = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
        public const string NORDIC_UART_RX_CHAR = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

        /** \brief event for a new response received */
        public event NotifyResponseReceived evResponseReceived = null;

        /** \brief current connected device*/
        private BluetoothLEDevice m_ConnectedDevice = null;

        /** \brief gatt result */
        private GattDeviceServicesResult m_Gatt = null;

        /** \brief internal characteristics result */
        private GattCharacteristicsResult m_Characteristics = null;

        /** target characteristic */
        private GattCharacteristic m_TxNordicCharacteristic= null;
        private GattCharacteristic m_RxNordicCharacteristic = null;

        /** \brief state connection */
        private bool m_IsConnected = false;

        /** \brief constructor */
        public ElaBLEConnector() { }

        /**
         * \fn connectDeviceAsync
         */
        public async Task<uint> ConnectDeviceAsync(String macAddress)
        {
            uint uiErrorCode = ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_NOT_CONNECTED;
            try
            {
                ulong ulMacAddress = MacAddress.macAdressHexaToLong(macAddress);
                // try to get data
                m_ConnectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(ulMacAddress);
                if (null != m_ConnectedDevice)
                {
                    m_Gatt = await m_ConnectedDevice.GetGattServicesAsync(BluetoothCacheMode.Cached);
                    if (null != m_Gatt)
                    {
                        foreach (GattDeviceService service in m_Gatt.Services)
                        {
                            if(service.Uuid.ToString().Equals(NORDIC_UART_SERVICE))
                            {
                                bool bFoundRx = false;
                                bool bFoundTx = false;
                                m_Characteristics = await service.GetCharacteristicsAsync();
                                foreach (var charac in m_Characteristics.Characteristics)
                                {
                                    if(charac.Uuid.ToString().Equals(NORDIC_UART_TX_CHAR))
                                    {
                                        m_TxNordicCharacteristic = charac;
                                        bFoundTx = true;
                                    }
                                    if (charac.Uuid.ToString().Equals(NORDIC_UART_RX_CHAR))
                                    {
                                        m_RxNordicCharacteristic = charac;
                                        var result = await m_RxNordicCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                        if (result == GattCommunicationStatus.Success)
                                        {
                                            m_RxNordicCharacteristic.ValueChanged += AssociatedCharacteristic_ValueChanged;
                                        }
                                        bFoundRx = true;
                                    }
                                    //
                                    if(true == bFoundRx && true == bFoundTx)
                                    {
                                        m_IsConnected = true;
                                        uiErrorCode = ErrorServiceHandlerBase.ERR_OK;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_IsConnected = false;
                //return ErrorServiceHandlerBase.ERR_CONNECT_ERROR;
                throw new ElaBleException($"Exception while trying to connect to device {macAddress}.", ex);
            }
            //
            return uiErrorCode;
        }

        /**
         * \fn disconnectDeviceAsync
         * \brief disconnect from bluetooth device
         */
        public uint DisconnectDevice()
        {
            try
            {
                if(null != m_TxNordicCharacteristic)
                {
                    m_TxNordicCharacteristic = null;
                }
                if (null != m_RxNordicCharacteristic)
                {
                    m_RxNordicCharacteristic.ValueChanged -= AssociatedCharacteristic_ValueChanged;
                    m_RxNordicCharacteristic = null;
                }
                // dispose service dictionnary
                if (null != m_Gatt)
                {
                    foreach (GattDeviceService service in m_Gatt.Services)
                    {
                        try { service.Dispose(); } catch { }
                        GC.Collect();
                    }
                }
                GC.SuppressFinalize(m_Gatt);
                m_Gatt = null;
                // disconnect device
                try
                {
                    if (null != m_ConnectedDevice)
                    {
                        m_ConnectedDevice.Dispose(); // according to docs, this one line should be enough to disconnect
                        GC.SuppressFinalize(m_ConnectedDevice);
                        GC.Collect();
                    }
                }
                catch { }
                //Following two lines are undocumented but necessary
                m_ConnectedDevice = null;
                GC.SuppressFinalize(this);
                GC.Collect();

                // default connect
                m_IsConnected = false;
            }
            catch (Exception ex)
            {
                throw new ElaBleException("Exception while tryig to disconnect from device.", ex);
            }
            return ErrorServiceHandlerBase.ERR_OK;
        }

        /**
         * \fn sendCommandAsync
         * \brief function to write through an uart nordic service if this one exist 
         */
        public async Task<uint> SendCommandAsync(String command, String password = "", String arguments = "")
        {
            uint uiErrorCode = ErrorServiceHandlerBase.ERR_OK;
            try
            {
                if(true == m_IsConnected &&
                    null != m_TxNordicCharacteristic &&
                    null != m_RxNordicCharacteristic)
                {
                    String fullCommand = command;
                    if(false == password.Equals(String.Empty)) fullCommand += $" {password}";
                    if (false == arguments.Equals(String.Empty)) fullCommand += $" {arguments}";
                    //
                    DataWriter writer = new DataWriter();
                    writer.WriteString(fullCommand);
                    GattCommunicationStatus status = await m_TxNordicCharacteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
                    if (status != GattCommunicationStatus.Success)
                    {
                        uiErrorCode = ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_CANNOT_WRITE_ON_NORDIC_TX;
                    }
                }
                else
                {
                    uiErrorCode = ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_NORDIC_UART_UNITIALIZED;
                }
            }
            catch (Exception ex)
            {
                throw new ElaBleException("An exception occurs while tryig to sending command from device.", ex);
            }
            return uiErrorCode;
        }

        /** associated event for a value changed*/
        private void AssociatedCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            String value = Encoding.UTF8.GetString(data, 0, data.Length);
            evResponseReceived?.Invoke(value);
        }
    }
}
