using System;
using System.Collections.Generic;
using System.Text;
using ElaBleCommunication.Wcl.Models;
using wclBluetooth;
using wclCommon;
using static wclCommon.Api.Win.Rt;
//using Windows.Devices.Radios;

namespace ElaBleCommunication.Wcl.Controllers
{
    public class WclBleController
    {
        private string _radioName = null;
        private wclBluetoothManager _manager;
        private wclBluetoothRadio _radio;

        public bool IsAvailable => CheckAvailable();
        public AppTypeEnum AppType { get; private set; }
        public WclBLEScanner Scanner { get; private set; }
        public WclBLEConnector Connector { get; private set; }

        public WclBleController()
        {
            Initialize();
        }

        public WclBleController(string radioName)
        {
            _radioName = radioName;
            Initialize(radioName);
        }

        public WclBleController(string radioName, bool forUIapp)
        {
            _radioName = radioName;
            Initialize(radioName, forUIapp);
        }

        public WclBleController(bool forUIapp)
        {
            Initialize(forUIapp: forUIapp);
        }

        private void Initialize(string radioName = null, bool forUIapp = false)
        {
            if (forUIapp)
            {
                // absolutely necessary when this lib is used in a UI framework
                wclMessageBroadcaster.SetSyncMethod(wclMessageSynchronizationKind.skMessages);
                AppType = AppTypeEnum.UI;
            }
            else
            {
                // Apc = Asynchronous Procedure Call
                wclMessageBroadcaster.SetSyncMethod(wclMessageSynchronizationKind.skApc);
                AppType = AppTypeEnum.Default;
            }

            // Note:
            // There is also wclMessageSynchronizationKind.skThread which is not recommended. All events fire in a separate
            // thread. An application is responsible for the synchronization with UI thread.
            // Must be used carefully. Most of the time use skApc instead.

            _manager = new wclBluetoothManager();
            var result = _manager.Open();
            if (result != wclErrors.WCL_E_SUCCESS) throw new Exception($"Error opening ble manager: 0x{result:X8} {ErrorMessages.Get(result)}");

            GetRadio(radioName);

            Scanner = new WclBLEScanner(_radio);
            Connector = new WclBLEConnector(_radio);
        }

        private void GetRadio(string radioName = null)
        {
            _radio = null;

            if (string.IsNullOrEmpty(radioName))
            {
                var result = _manager.GetLeRadio(out _radio);
                if (result != wclErrors.WCL_E_SUCCESS) throw new Exception($"Get working radio failed: 0x{result:X8} {ErrorMessages.Get(result)}");
            }
            else
            {
                if (_manager.Count == 0) throw new Exception("No radio could be found");
                for (int i = 0; i < _manager.Count; i++)
                {
                    if (_manager[i].Available)
                    {
                        var currentRadio = _manager[i];
                        currentRadio.GetName(out var name);
                        if (name == radioName)
                        {
                            _radio = currentRadio;
                            break;
                        }
                    }
                }
                if (_radio == null) throw new Exception($"No available radio wilth name \"{radioName}\" could be found");
            }
        }

        private bool CheckAvailable()
        {
            try
            {
                GetRadio(_radioName);
                return _radio != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
