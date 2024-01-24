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
        private WclGattConnection _gattConnection = null;
        private bool _isConnected = false;
        private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);

        /** \brief constructor */
        public WclBLEConnector(wclBluetoothRadio radio)
        {
            _radio = radio;
        }

        /**
         * \fn connectDeviceAsync
         */
        public async Task<uint> ConnectDeviceAsync(string macAddress, bool debug = false)
        {
            await _connectLock.WaitAsync();
            try
            {
                long lMacAddress = MacAddress.macAdressHexaToLong(macAddress);
                _gattConnection = new WclGattConnection(debug);
                var connected = _gattConnection.Connect(_radio, lMacAddress);

                if (connected)
                {
                    _isConnected = true;
                    _gattConnection.ResponseReceived += M_GattConnection_ResponseReceived;
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
                _connectLock.Release();
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
            _connectLock.Wait();
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
                _connectLock.Release();
            }
        }

        private uint DisconnectDevice_MustBeUnderLock()
        {
            try
            {
                if (_gattConnection == null) return ErrorServiceHandlerBase.ERR_OK;

                _gattConnection.Disconnect();
                _gattConnection = null;
                _isConnected = false;

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
            await _connectLock.WaitAsync();
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
                _connectLock.Release();
            }
        }

        public async Task<uint> SendCommandAsync(byte[] command)
        {
            await _connectLock.WaitAsync();
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
                _connectLock.Release();
            }
        }

        private Task<uint> SendCommandAsync_MustBeUnderLock(byte[] command)
        {
            if (_gattConnection == null) return Task.FromResult(ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_NOT_CONNECTED);

            _gattConnection.SendCommand(command);

            return Task.FromResult(ErrorServiceHandlerBase.ERR_OK);

        }
    }
}
