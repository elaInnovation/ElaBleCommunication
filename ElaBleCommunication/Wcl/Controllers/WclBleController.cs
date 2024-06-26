﻿using System;
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
                if (_scanner == null) throw new Exception($"Controller not initialized : consider calling {nameof(SetRadio)}() method");
                return _scanner;  
            }
            private set { _scanner = value; } 
        }

        private WclBLEConnector _connector;
        public WclBLEConnector Connector
        {
            get
            {
                if (_connector == null) throw new Exception($"Controller not initialized : consider calling {nameof(SetRadio)}() method");
                return _connector;
            }
            private set { _connector = value; }
        }

        public WclBleController()
        {
            Initialize(AppTypeEnum.Default);
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
        }

        public void SetRadio(string radioName = null)
        {
            GetRadio(radioName);

            Scanner = new WclBLEScanner(_radio);
            Connector = new WclBLEConnector(_radio);
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
                if (result != wclErrors.WCL_E_SUCCESS) throw new Exception($"Get working radio failed: 0x{result:X8} {ErrorMessages.Get(result)}");
                _radio.GetName(out _radioName);
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
                            _radioName = radioName;
                            return;
                        }
                    }
                }
                if (_radio == null) throw new Exception($"No available radio wilth name \"{radioName}\" could be found");
            }
        }

        public bool CheckAvailable(string radioName = null)
        {
            if (string.IsNullOrEmpty(radioName)) return GetAvailableRadios().Count > 0;
            return GetAvailableRadios().Contains(radioName);
        }

        private void OpenManager()
        {
            var result = _manager.Open();
            if (result != wclErrors.WCL_E_SUCCESS || result != 0x00050001) throw new Exception($"Error opening ble manager: 0x{result:X8} {ErrorMessages.Get(result)}");
        }

        private void CloseManager()
        {
            var result = _manager.Close();
            if (result != wclErrors.WCL_E_SUCCESS || result != 0x00050000) throw new Exception($"Error closing ble manager: 0x{result:X8} {ErrorMessages.Get(result)}");
        }

        public void Close()
        {
            _scanner.Stop();
            _manager.Close();
            _manager = null;
        }
    }
}
