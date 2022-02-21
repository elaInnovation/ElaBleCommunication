using ElaBleCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ElaBleCommunicationUI.Views
{
    /// <summary>
    /// Logique d'interaction pour BleScanner.xaml
    /// </summary>
    public partial class BleScanner : UserControl
    {
        /** \brief ble scanner declaration */
        private ElaBLEAdvertisementWatcher scanner = new ElaBLEAdvertisementWatcher();

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
            this.scanner.evAdvertisementReceived += Scanner_evAdvertisementReceived; ;
        }

        #region event controller
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            this.lvBleDevice.Items.Clear();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            this.scanner.StartBluetoothScanner();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            this.scanner.StopBluetoothScanner();
        }
        #endregion

        #region received event
        private void Scanner_evAdvertisementReceived(ElaTagClassLibrary.ElaTags.Interoperability.ElaBaseData device)
        {
            var formattedData = $"{device.timestamp};" +
                $"{device.macaddress};" +
                $"{device.localname};" +
                $"{device.rssi};";

            DispatcherOperation op = Dispatcher.BeginInvoke((Action)(() =>
            {
                this.lvBleDevice.Items.Add(formattedData);
            }));
        }
        #endregion
    }
}
