using System;
using System.Collections.Generic;
using System.Linq;
using ElaBleCommunication.Wcl.Models;
using wclBluetooth;
using wclCommon;

namespace ElaBleCommunication.Wcl.Controllers
{
    public class WclBleController
    {
        private wclBluetoothManager _manager;
        private wclBluetoothRadio _radio;

        public AppTypeEnum AppType { get; private set; }
        public wclBluetoothApi Api { get; private set; }
        public bool IsAvailable => _radio.Available;

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

        public WclBleController(AppTypeEnum appType)
        {
            Initialize(appType);
            SetRadio();
        }

        public WclBleController(AppTypeEnum appType, wclBluetoothApi api)
        {
            Initialize(appType);
            SetRadio(api);
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

        public void SetRadio(wclBluetoothApi api = wclBluetoothApi.baMicrosoft)
        {
            try
            {
                _radio = null;

                var availableRadios = GetAvailableRadios();
                foreach (var availableRadio in availableRadios)
                {
                    if (availableRadio.Api == api)
                    {
                        _radio = availableRadio;

                        if (_scanner != null) _scanner.Stop();
                        Scanner = new WclBLEScanner(_radio);
                        Connector = new WclBLEConnector(_radio);
                        Api = api;

                        return;
                    }
                }

                if (_radio == null) throw new WclException($"No radio with api {api} could be found");
            }
            catch (Exception)
            {
                _manager.Close();
                throw;
            }
        }

        private List<wclBluetoothRadio> GetAvailableRadios()
        {
            _manager.Close();
            _manager.Open();

            var radios = new List<wclBluetoothRadio>();
            for (int i = 0; i < _manager.Count; i++)
            {
                var currentRadio = _manager[i];
                if (currentRadio.Available)
                {
                    radios.Add(currentRadio);
                }
            }

            return radios;
        }

        public void Close()
        {
            _scanner.Stop();
            _manager.Close();
            _manager = null;
        }
    }
}
