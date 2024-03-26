using System;
using System.Collections.Generic;
using System.Text;
using wclBluetooth;
using wclCommon;
//using Windows.Devices.Radios;

namespace ElaBleCommunication.Wcl
{
    public class WclBleController
    {
        public WclBLEScanner Scanner { get; private set; }
        public WclBLEConnector Connector { get; private set; }

        public WclBleController()
        {
            Init();
        }

        public WclBleController(string radioName)
        {
            Init(radioName);
        }

        public WclBleController(string radioName, bool forUIapp)
        {
            Init(radioName, forUIapp);
        }

        public WclBleController(bool forUIapp)
        {
            Init(null, forUIapp);
        }

        private void Init(string radioName = null, bool forUIapp = false)
        {
            if (forUIapp)
            {
                // absolutely necessary when this lib is used in a UI framework
                wclMessageBroadcaster.SetSyncMethod(wclMessageSynchronizationKind.skMessages);
            }
            else
            {
                // Apc = Asynchronous Procedure Call
                wclMessageBroadcaster.SetSyncMethod(wclMessageSynchronizationKind.skApc);
            }

            // Note:
            // There is also wclMessageSynchronizationKind.skThread which is not recommended. All events fire in a separate
            // thread. An application is responsible for the synchronization with UI thread.
            // Must be used carefully. Most of the time use skApc instead.

            wclBluetoothRadio radio = null;
            var manager = new wclBluetoothManager();
            var result = manager.Open();
            if (result != wclErrors.WCL_E_SUCCESS) throw new Exception($"Error opening ble manager: 0x{result:X8} {ErrorMessages.Get(result)}");

            if (string.IsNullOrEmpty(radioName))
            {
                result = manager.GetLeRadio(out radio);
                if (result != wclErrors.WCL_E_SUCCESS) throw new Exception($"Get working radio failed: 0x{result:X8} {ErrorMessages.Get(result)}");
            }
            else
            {
                if (manager.Count == 0) throw new Exception("No radio could be found");
                for (int i = 0; i < manager.Count; i++)
                {
                    if (manager[i].Available)
                    {
                        var currentRadio = manager[i];
                        currentRadio.GetName(out var name);
                        if (name == radioName)
                        {
                            radio = currentRadio;
                            break;
                        }
                    }
                }
                if (radio == null) throw new Exception($"No available radio wilth name \"{radioName}\" could be found");
            }          

            Scanner = new WclBLEScanner(radio);
            Connector = new WclBLEConnector(radio);
        }
    }
}
