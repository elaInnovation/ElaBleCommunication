using System;
using System.Collections.Generic;
using ElaBleCommunication.Wcl.Models;
using wclBluetooth;
using wclCommon;

namespace ElaBleCommunication.Wcl.Controllers
{
    public class WclBleController
    {
        private string _radioName = null;
        private wclBluetoothManager _manager;
        private wclBluetoothRadio _radio;

        public AppTypeEnum AppType { get; private set; }

        private WclBLEScanner _scanner;
        public WclBLEScanner Scanner 
        { 
            get {
                if (_scanner == null) throw new WclException($"Wcl scanner not initialized : consider calling {nameof(SetRadio)}() method");
                return _scanner;  
            }
            private set { _scanner = value; } 
        }

        private WclBLEConnector _connector;
        public WclBLEConnector Connector
        {
            get
            {
                if (_connector == null) throw new WclException($"Wcl connector not initialized : consider calling {nameof(SetRadio)}() method");
                return _connector;
            }
            private set { _connector = value; }
        }

        public WclBleController()
        {
            Initialize(AppTypeEnum.Default);
            SetRadio();
        }

        public WclBleController(string radioName)
        {
            Initialize(AppTypeEnum.Default);
            SetRadio(radioName);
        }

        public WclBleController(string radioName, AppTypeEnum appType = AppTypeEnum.Default)
        {
            Initialize(appType);
            SetRadio(radioName);
        }

        public WclBleController(AppTypeEnum appType)
        {
            Initialize(appType);
            SetRadio();
        }

        private void Initialize(AppTypeEnum appType)
        {
            AppType = appType;

            if (appType == AppTypeEnum.UI)
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

            _manager = new wclBluetoothManager();
            var result = _manager.Open();
            if (result == ErrorMessages.WCL_E_BLUETOOTH_MANAGER_OPENED || result == ErrorMessages.WCL_E_BLUETOOTH_MANAGER_EXISTS)
            {
                result = _manager.Close();
                if (result != wclErrors.WCL_E_SUCCESS) throw new WclException(result, "Bluetooth manager was already opeend but unable to close it first");
                result = _manager.Open();
            }
            if (result != wclErrors.WCL_E_SUCCESS) throw new WclException(result, "Error while opening wcl bluetooth manager");
        }

        public void SetRadio(string radioName = null)
        {
            try
            {
                GetRadio(radioName);
                Scanner = new WclBLEScanner(_radio);
                Connector = new WclBLEConnector(_radio);
            }
            catch (Exception)
            {
                _manager.Close();
                throw;
            }
            
        }

        public List<string> GetAvailableRadios()
        {
            var radios = new List<string>();
            for (int i = 0; i < _manager.Count; i++)
            {
                var currentRadio = _manager[i];
                if (currentRadio.Available)
                {
                    currentRadio.GetName(out var name);
                    radios.Add(name);
                }
            }

            return radios;
        }

        private void GetRadio(string radioName = null)
        {
            _radio = null;

            if (string.IsNullOrEmpty(radioName))
            {
                var result = _manager.GetLeRadio(out _radio);
                if (result != wclErrors.WCL_E_SUCCESS) throw new WclException(result, "Get working radio failed");
                _radio.GetName(out _radioName);
            }
            else
            {
                if (_manager.Count == 0) throw new WclException("No radio could be found");
                for (int i = 0; i < _manager.Count; i++)
                {
                    if (_manager[i].Available)
                    {
                        var currentRadio = _manager[i];
                        currentRadio.GetName(out var name);
                        if (name == radioName)
                        {
                            _radio = currentRadio;
                            _radioName = radioName;
                            return;
                        }
                    }
                }
                if (_radio == null) throw new WclException($"No available radio with name \"{radioName}\" could be found");
            }
        }

        public bool CheckAvailable()
        {
            _manager.Close();
            _manager.Open();

            try
            {
                SetRadio(_radioName);
                return true;
            }
            catch 
            {
                return false;
            }
        }

        public void Close()
        {
            _scanner.Stop();
            _manager.Close();
            _manager = null;
        }
    }
}
