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
        public const string nordicUartService = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
        public const string nordicUartTxCharacteristic = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
        public const string nordicUartRxCharacteristic = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

        /** \brief event for a new response received */
        public event NotifyResponseReceived evResponseReceived = null;

        /** \brief current connected device*/
        private BluetoothLEDevice connectedDevice = null;

        /** \brief gatt result */
        private GattDeviceServicesResult gatt = null;

        /** \brief internal characteristics result */
        private GattCharacteristicsResult characs = null;

        /** target characteristic */
        private GattCharacteristic txNordicCharacteristic= null;
        private GattCharacteristic rxNordicCharacteristic = null;

        /** \brief state connection */
        private bool bIsConnected = false;

        /** \brief constructor */
        public ElaBLEConnector() { }

        /**
         * \fn connectDeviceAsync
         */
        public async Task<uint> connectDeviceAsync(String macAddress)
        {
            uint uiErrorCode = ErrorHandler.ERR_NOT_CONNECTED;
            try
            {
                ulong ulMacAddress = MacAddress.macAdressHexaToLong(macAddress);
                // try to get data
                this.connectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(ulMacAddress);
                if (null != this.connectedDevice)
                {
                    this.gatt = await this.connectedDevice.GetGattServicesAsync(BluetoothCacheMode.Cached);
                    if (null != gatt)
                    {
                        foreach (GattDeviceService service in gatt.Services)
                        {
                            if(service.Uuid.ToString().Equals(nordicUartService))
                            {
                                bool bFoundRx = false;
                                bool bFoundTx = false;
                                this.characs = await service.GetCharacteristicsAsync();
                                foreach (var charac in characs.Characteristics)
                                {
                                    if(charac.Uuid.ToString().Equals(nordicUartTxCharacteristic))
                                    {
                                        this.txNordicCharacteristic = charac;
                                        bFoundTx = true;
                                    }
                                    if (charac.Uuid.ToString().Equals(nordicUartRxCharacteristic))
                                    {
                                        this.rxNordicCharacteristic = charac;
                                        var result = await this.rxNordicCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                        if (result == GattCommunicationStatus.Success)
                                        {
                                            this.rxNordicCharacteristic.ValueChanged += AssociatedCharacteristic_ValueChanged;
                                        }
                                        bFoundRx = true;
                                    }
                                    //
                                    if(true == bFoundRx && true == bFoundTx)
                                    {
                                        this.bIsConnected = true;
                                        uiErrorCode = ErrorHandler.ERR_OK;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.bIsConnected = false;
                throw new ElaBleException("An exception occurs while tryig to disconnect from device.", ex);
            }
            //
            return uiErrorCode;
        }

        /**
         * \fn disconnectDeviceAsync
         * \brief disconnect from bluetooth device
         */
        public uint disconnectDeviceAsync()
        {
            try
            {
                if(null != this.txNordicCharacteristic)
                {
                    this.txNordicCharacteristic = null;
                }
                if (null != this.rxNordicCharacteristic)
                {
                    this.rxNordicCharacteristic.ValueChanged -= AssociatedCharacteristic_ValueChanged;
                    this.rxNordicCharacteristic = null;
                }
                // dispose service dictionnary
                if (null != this.gatt)
                {
                    foreach (GattDeviceService service in this.gatt.Services)
                    {
                        try { service.Dispose(); } catch { }
                        GC.Collect();
                    }
                }
                GC.SuppressFinalize(this.gatt);
                this.gatt = null;
                // disconnect device
                try
                {
                    if (null != this.connectedDevice)
                    {
                        this.connectedDevice.Dispose(); // according to docs, this one line should be enough to disconnect
                        GC.SuppressFinalize(this.connectedDevice);
                        GC.Collect();
                    }
                }
                catch { }
                //Following two lines are undocumented but necessary
                this.connectedDevice = null;
                GC.SuppressFinalize(this);
                GC.Collect();

                // default connect
                this.bIsConnected = false;
            }
            catch (Exception ex)
            {
                throw new ElaBleException("An exception occurs while tryig to disconnect from device.", ex);
            }
            return ErrorHandler.ERR_OK;
        }

        /**
         * \fn sendCommandAsync
         * \brief function to write through an uart nordic service if this one exist 
         */
        public async Task<uint> sendCommandAsync(String command, String password = "", String arguments = "")
        {
            uint uiErrorCode = ErrorHandler.ERR_OK;
            try
            {
                if(true == this.bIsConnected &&
                    null != this.txNordicCharacteristic &&
                    null != this.rxNordicCharacteristic)
                {
                    String fullCommand = command;
                    if(false == password.Equals(String.Empty)) fullCommand += $" {password}";
                    if (false == arguments.Equals(String.Empty)) fullCommand += $" {arguments}";
                    //
                    DataWriter writer = new DataWriter();
                    writer.WriteString(fullCommand);
                    GattCommunicationStatus status = await this.txNordicCharacteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
                    if (status != GattCommunicationStatus.Success)
                    {
                        uiErrorCode = ErrorHandler.ERR_CANNOT_WRITE_ON_NORDIC_TX;
                    }
                }
                else
                {
                    uiErrorCode = ErrorHandler.ERR_NORDIC_UART_UNITIALIZED;
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
            Windows.Storage.Streams.DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            String value = Encoding.UTF8.GetString(data, 0, data.Length);
            evResponseReceived?.Invoke(value);
        }
    }
}
