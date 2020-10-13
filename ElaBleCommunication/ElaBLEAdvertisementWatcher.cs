using ElaBleCommunication.Error;
using ElaBleCommunication.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Bluetooth.Advertisement;

/**
 * \namespace ElaBleCommunication
 * \brief namespace associated to the Bluetooth configuration
 */
namespace ElaBleCommunication
{
    /** \brief delegate associated to the Bluetooth device */
    public delegate void NewAdvertismentreceived(ElaBleDevice device);
    
    /**
     * \class ElaBLEAdvertisementWatcher
     * \brief use ElaBLEAdvertisementWatcher to scan and get data from Bluetooth Advertising
     */
    public class ElaBLEAdvertisementWatcher
    {
        /** \brief event for new advertisement */
        public event NewAdvertismentreceived evAdvertisementReceived = null;

        /** \brief BLE Watcher declaration */
        private BluetoothLEAdvertisementWatcher m_watcher = new BluetoothLEAdvertisementWatcher();

        /** \brief status to handle advertisement scanner started */
        private bool m_bStarted = false;

        /**
         * \fn startBluetoothScanner
         * \brief start the bluetooth scanner
         * \return error code :
         *      + ERR_SCANNER_ALREADY_STARTED
         *      + ERR_OK
         */
        public uint startBluetoothScanner()
        {
            try
            {
                if(true == this.m_bStarted)
                {
                    return ErrorHandler.ERR_SCANNER_ALREADY_STARTED;
                }
                //
                this.m_watcher.Received += Watcher_Received;
                this.m_watcher.Start();
                //
                this.m_bStarted = true;
            }
            catch (Exception e)
            {
                throw new ElaBleException("An exception occurs while tryig to Start Bluetooth Scanner.", e);
            }
            return ErrorHandler.ERR_OK;
        }

        /**
         * \fn stopBluetoothScanner
         * \brief stop the bluetooth scanner
         * \return error code :
         *      + ERR_SCANNER_ALREADY_STOPPED
         *      + ERR_OK
         */
        public uint stopBluetoothScanner()
        {
            try
            {
                if (false == this.m_bStarted)
                {
                    return ErrorHandler.ERR_SCANNER_ALREADY_STOPPED;
                }
                //
                this.m_watcher.Stop();
                this.m_watcher.Received -= Watcher_Received;
                //
                this.m_bStarted = false;
            }
            catch (Exception e)
            {
                throw new ElaBleException("An exception occurs while tryig to Stop Bluetooth Scanner.", e);
            }

            return ErrorHandler.ERR_OK;
        }

        /**
         * \fn Watcher_Received
         * \brief a new event from BluetoothLEAdvertisementWatcher has been received 
         * \param [in] sender : BluetoothLEAdvertisementWatcher associated to the event 
         * \param [in] args : argument assoiated to the event of advertising
         */
        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            ElaBleDevice device = new ElaBleDevice(args);
            evAdvertisementReceived?.Invoke(device);
        }
    }
}
