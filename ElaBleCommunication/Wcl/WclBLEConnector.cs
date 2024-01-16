using ElaBleCommunication.Error;
using ElaBleCommunication.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElaSoftwareCommon.Error;
using System.Threading;
using wclBluetooth;

/**
 * \namespace ElaBluetoothCommunication
 * \brief namespace associated to the Bluetooth configuration
 */
namespace ElaBleCommunication.Wcl
{
    /**
     * \class WclBLEConnector
     * \brief use nordic UART to connect to ELA Bluetooth tags
     */
    public class WclBLEConnector
    {
        /** \brief event for a new response received */
        public event NotifyResponseReceived evResponseReceived = null;

        private readonly wclBluetoothRadio _radio;

        /** \brief current connected device*/
        private string m_ConnectedDeviceMacAddress = null;
        private WclGattConnection m_GattConnection = null;


        /** \brief state connection */
        private bool m_IsConnected = false;
        private readonly SemaphoreSlim m_ConnectLock = new SemaphoreSlim(1, 1);

        /** \brief constructor */
        public WclBLEConnector(wclBluetoothRadio radio)
        {
            _radio = radio;
        }

        /**
         * \fn connectDeviceAsync
         */
        public async Task<uint> ConnectDeviceAsync(string macAddress)
        {
            await m_ConnectLock.WaitAsync();
            try
            {
                long lMacAddress = MacAddress.macAdressHexaToLong(macAddress);
                m_GattConnection = new WclGattConnection();
                var connected = m_GattConnection.Connect(_radio, lMacAddress);

                if (connected)
                {
                    m_ConnectedDeviceMacAddress = macAddress;
                    m_GattConnection.ResponseReceived += M_GattConnection_ResponseReceived;
                    return ErrorServiceHandlerBase.ERR_OK;
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

        private void M_GattConnection_ResponseReceived(byte[] response)
        {
            evResponseReceived?.Invoke(response);
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
                if (m_GattConnection == null) return ErrorServiceHandlerBase.ERR_OK;

                m_GattConnection.Disconnect();
                m_GattConnection = null;

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

        private Task<uint> SendCommandAsync_MustBeUnderLock(byte[] command)
        {
            if (m_GattConnection == null) return Task.FromResult(ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_NOT_CONNECTED);

            m_GattConnection.SendCommand(command);

            return Task.FromResult(ErrorServiceHandlerBase.ERR_OK);

        }
    }
}
