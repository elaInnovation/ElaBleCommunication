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
        private wclBluetoothRadio _radio;
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

        private void Init(string radioName = null)
        {
            // absolutely necessary when this lib is not used in a UI framework
            // Apc = Asynchronous Procedure Call
            wclMessageBroadcaster.SetSyncMethod(wclMessageSynchronizationKind.skApc);

            var manager = new wclBluetoothManager();
            var result = manager.Open();
            if (result != wclErrors.WCL_E_SUCCESS) throw new Exception($"Error opening ble manager: 0x{result:X8} {ErrorMessages.Get(result)}");

            if (string.IsNullOrEmpty(radioName))
            {
                result = manager.GetLeRadio(out _radio);
                if (result != wclErrors.WCL_E_SUCCESS) throw new Exception($"Get working radio failed: 0x{result:X8} {ErrorMessages.Get(result)}");
            }
            else
            {
                if (manager.Count == 0) throw new Exception("No radio could be found");
                for (int i = 0; i < manager.Count; i++)
                {
                    if (manager[i].Available)
                    {
                        var radio = manager[i];
                        radio.GetName(out var name);
                        if (name == radioName)
                        {
                            _radio = radio;
                            break;
                        }
                    }
                }
                if (_radio == null) throw new Exception("No radio could be found");
            }          

            Scanner = new WclBLEScanner(_radio);
            Connector = new WclBLEConnector(_radio);
        }
    }
}
