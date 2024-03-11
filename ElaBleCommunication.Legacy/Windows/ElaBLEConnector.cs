using ElaBleCommunication.Common.Error;
using ElaBleCommunication.Common.Tools;
using ElaBleCommunication.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using ElaSoftwareCommon.Error;
using System.Threading;

/**
 * \namespace ElaBluetoothCommunication
 * \brief namespace associated to the Bluetooth configuration
 */
namespace ElaBleCommunication.Legacy.Windows
{
    /** \brief notify that a new message has been received from tag */
    public delegate void NotifyResponseReceived(byte[] response);

    /**
     * \class ElaBLEConnector
     * \brief use nordic UART to connect to ELA Bluetooth tags
     */
    public class ElaBLEConnector
    {
        /** \brief event for a new response received */
        public event NotifyResponseReceived evResponseReceived = null;

        /** \brief current connected device*/
        private BluetoothLEDevice m_ConnectedDevice = null;
        private string m_ConnectedDeviceMacAddress = null;

        /** \brief gatt result */
        private GattDeviceServicesResult m_Gatt = null;

        /** target characteristic */
        private GattCharacteristic m_TxNordicCharacteristic = null;
        private GattCharacteristic m_RxNordicCharacteristic = null;

        /** \brief state connection */
        private bool m_IsConnected = false;
        private readonly SemaphoreSlim m_ConnectLock = new SemaphoreSlim(1, 1);

        /** \brief constructor */
        public ElaBLEConnector() { }

        /**
         * \fn connectDeviceAsync
         */
        public async Task<uint> ConnectDeviceAsync(string macAddress)
        {
            await m_ConnectLock.WaitAsync();
            try
            {
                if (m_IsConnected)
                {
                    if (m_ConnectedDeviceMacAddress == macAddress)
                        return ErrorServiceHandlerBase.ERR_OK;
                    else
                        DisconnectDevice_MustBeUnderLock();
                }

                ulong ulMacAddress = MacAddressHelper.macAdressHexaToULong(macAddress);

                m_ConnectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(ulMacAddress);
                if (null == m_ConnectedDevice) return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_CONNECT_ERROR;

                m_Gatt = await m_ConnectedDevice.GetGattServicesAsync(BluetoothCacheMode.Cached);
                if (null == m_Gatt) return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_CONNECT_ERROR;

                foreach (GattDeviceService service in m_Gatt.Services)
                {
                    if (service.Uuid.ToString() == ElaCharacteristics.NORDIC_UART_SERVICE)
                    {
                        bool bFoundRx = false;
                        bool bFoundTx = false;

                        var characteristics = await service.GetCharacteristicsAsync();
                        if (characteristics.Status != GattCommunicationStatus.Success) return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_CONNECT_ERROR;

                        foreach (var charac in characteristics.Characteristics)
                        {
                            if (charac.Uuid.ToString() == ElaCharacteristics.NORDIC_UART_TX_CHAR)
                            {
                                m_TxNordicCharacteristic = charac;
                                bFoundTx = true;
                            }
                            if (charac.Uuid.ToString() == ElaCharacteristics.NORDIC_UART_RX_CHAR)
                            {
                                m_RxNordicCharacteristic = charac;
                                var result = await m_RxNordicCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (result == GattCommunicationStatus.Success)
                                {
                                    m_RxNordicCharacteristic.ValueChanged += AssociatedCharacteristic_ValueChanged;
                                    bFoundRx = true;
                                }
                            }
                            //
                            if (true == bFoundRx && true == bFoundTx)
                            {
                                m_IsConnected = true;
                                m_ConnectedDeviceMacAddress = macAddress;
                                return ErrorServiceHandlerBase.ERR_OK;
                            }
                        }
                    }
                }

                return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_CONNECT_ERROR;
            }
            catch (Exception ex)
            {
                throw new ElaBleException($"Exception while trying to connect to device {macAddress}.", ex);
            }
            finally
            {
                m_ConnectLock.Release();
            }
        }

        /**
         * \fn disconnectDeviceAsync
         * \brief disconnect from bluetooth device
         */
        public uint DisconnectDevice()
        {
            m_ConnectLock.Wait();
            try
            {
                return DisconnectDevice_MustBeUnderLock();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                m_ConnectLock.Release();
            }
        }

        private uint DisconnectDevice_MustBeUnderLock()
        {
            try
            {
                if (!m_IsConnected) return ErrorServiceHandlerBase.ERR_OK;

                if (null != m_TxNordicCharacteristic)
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
                if (null != m_ConnectedDevice)
                {
                    m_ConnectedDevice.Dispose(); // according to docs, this one line should be enough to disconnect
                    GC.SuppressFinalize(m_ConnectedDevice);
                    GC.Collect();
                }
                //Following two lines are undocumented but necessary
                m_ConnectedDevice = null;
                GC.SuppressFinalize(this);
                GC.Collect();

                m_IsConnected = false;
                m_ConnectedDeviceMacAddress = null;

                return ErrorServiceHandlerBase.ERR_OK;
            }
            catch (Exception ex)
            {
                throw new ElaBleException("Exception while trying to disconnect from device.", ex);
            }
        }

        /**
         * \fn sendCommandAsync
         * \brief function to write through an uart nordic service if this one exist 
         */
        public async Task<uint> SendCommandAsync(string command, string password = "", string arguments = "")
        {
            await m_ConnectLock.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(password)) command += $" {password}";
                if (!string.IsNullOrEmpty(arguments)) command += $" {arguments}";
                //
                var commandBytes = Encoding.UTF8.GetBytes(command);
                return await SendCommandAsync_MustBeUnderLock(commandBytes);
            }
            catch (Exception ex)
            {
                throw new ElaBleException("An exception occurs while tryig to sending command from device.", ex);
            }
            finally
            {
                m_ConnectLock.Release();
            }
        }

        public async Task<uint> SendCommandAsync(byte[] command)
        {
            await m_ConnectLock.WaitAsync();
            try
            {
                return await SendCommandAsync_MustBeUnderLock(command);
            }
            catch (Exception ex)
            {
                throw new ElaBleException("An exception occurs while tryig to sending command from device.", ex);
            }
            finally
            {
                m_ConnectLock.Release();
            }
        }

        private async Task<uint> SendCommandAsync_MustBeUnderLock(byte[] command)
        {
            if (!m_IsConnected) return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_NOT_CONNECTED;
            if (m_TxNordicCharacteristic == null || m_RxNordicCharacteristic == null) return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_NORDIC_UART_UNITIALIZED;

            //
            using (DataWriter writer = new DataWriter())
            {
                writer.WriteBytes(command);
                GattCommunicationStatus status = await m_TxNordicCharacteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
                if (status != GattCommunicationStatus.Success) return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_CANNOT_WRITE_ON_NORDIC_TX;
                return ErrorServiceHandlerBase.ERR_OK;
            }
        }

        /** associated event for a value changed*/
        private void AssociatedCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);
            evResponseReceived?.Invoke(data);
        }
    }
}
