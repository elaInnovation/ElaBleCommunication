﻿using ElaBleCommunication.Error;
using ElaBleCommunication.Model;
using ElaBleCommunication.Tools;
using ElaSoftwareCommon.Error;
using ElaTagClassLibrary.ElaTags.Interoperability;
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
    public delegate void NewAdvertismentreceived(ElaBaseData device);
    
    /**
     * \class ElaBLEAdvertisementWatcher
     * \brief use ElaBLEAdvertisementWatcher to scan and get data from Bluetooth Advertising
     */
    public class ElaBLEAdvertisementWatcher
    {
        /** \brief event for new advertisement */
        public event NewAdvertismentreceived evAdvertisementReceived = null;

        /** \brief BLE Watcher declaration */
        private BluetoothLEAdvertisementWatcher m_Watcher = new BluetoothLEAdvertisementWatcher();

        /** \brief status to handle advertisement scanner started */
        private bool m_IsStarted = false;

        /**
         * \fn startBluetoothScanner
         * \brief start the bluetooth scanner
         * \return error code :
         *      + ERR_SCANNER_ALREADY_STARTED
         *      + ERR_OK
         */
        public uint StartBluetoothScanner()
        {
            try
            {
                if(true == m_IsStarted)
                {
                    return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_SCANNER_ALREADY_STARTED;
                }
                //
                m_Watcher.Received += Watcher_Received;
                m_Watcher.Start();
                //
                m_IsStarted = true;
            }
            catch (Exception e)
            {
                throw new ElaBleException("An exception occurs while tryig to Start Bluetooth Scanner.", e);
            }
            return ErrorServiceHandlerBase.ERR_OK;
        }

        /**
         * \fn stopBluetoothScanner
         * \brief stop the bluetooth scanner
         * \return error code :
         *      + ERR_SCANNER_ALREADY_STOPPED
         *      + ERR_OK
         */
        public uint StopBluetoothScanner()
        {
            try
            {
                if (false == m_IsStarted)
                {
                    return ErrorServiceHandlerBase.ERR_ELA_BLE_COMMUNICATION_SCANNER_ALREADY_STOPPED;
                }
                //
                m_Watcher.Stop();
                m_Watcher.Received -= Watcher_Received;
                //
                m_IsStarted = false;
            }
            catch (Exception e)
            {
                throw new ElaBleException("Exception while trying to stop Bluetooth scanner.", e);
            }

            return ErrorServiceHandlerBase.ERR_OK;
        }

        /**
         * \fn Watcher_Received
         * \brief a new event from BluetoothLEAdvertisementWatcher has been received 
         * \param [in] sender : BluetoothLEAdvertisementWatcher associated to the event 
         * \param [in] args : argument assoiated to the event of advertising
         */
        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            var device = BleDeviceTranslator.ToInteroperableObject(args);
            if (device != null) evAdvertisementReceived?.Invoke(device);
        }
    }
}
