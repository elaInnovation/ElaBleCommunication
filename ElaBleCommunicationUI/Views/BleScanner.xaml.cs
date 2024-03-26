using ElaBleCommunication.Wcl;
using ElaBleCommunication.Legacy.Windows;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ElaBleCommunicationUI.Views
{
    /// <summary>
    /// Logique d'interaction pour BleScanner.xaml
    /// </summary>
    public partial class BleScanner : UserControl
    {
        /** \brief ble scanner declaration */
#if WCL
        private readonly WclBLEScanner _scanner = MainWindow.BleController.Scanner;
#else
        private readonly ElaBLEAdvertisementWatcher _scanner = new ElaBLEAdvertisementWatcher();
#endif

        /** \brief constructor */
        public BleScanner()
        {
            InitializeComponent();
            initializeInternalComponent();
        }

        /**
         * \fn initializeInternalComponent
         * \brief initialize internal component
         */
        private void initializeInternalComponent()
        {
            _scanner.evAdvertisementReceived += Scanner_evAdvertisementReceived;
        }

        #region event controller
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            this.lvBleDevice.Items.Clear();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
#if WCL
            _scanner.Start(true, 29, 29);
#else
            _scanner.StartBluetoothScanner();
#endif
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
#if WCL
            _scanner.Stop();
#else
            _scanner.StopBluetoothScanner();
#endif
        }
        #endregion

        #region received event
        private void Scanner_evAdvertisementReceived(ElaTagClassLibrary.ElaTags.Interoperability.ElaBaseData device)
        {
            var formattedData = $"{device.timestamp};" +
                $"{device.identification?.macaddress};" +
                $"{device.identification?.localname};" +
                $"{device.rssi};";

            DispatcherOperation op = Dispatcher.BeginInvoke((Action)(() =>
            {
                this.lvBleDevice.Items.Add(formattedData);
            }));
        }
        #endregion
    }
}
