using ElaBleCommunication.Wcl;
using ElaBleCommunicationUI.Views;
using System.Windows;
using System.Windows.Controls;

namespace ElaBleCommunicationUI
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if WCL
        public static WclBleController BleController { get; } = new WclBleController(forUIapp: true);
#endif

        /** \brief constructor */
        public MainWindow()
        {
            InitializeComponent();
            initializeInternalComponents();
        }

        /**
         * \fn initializeInternalComponents
         * \brief initialize internal UI components 
         */
        private void initializeInternalComponents()
        {
            TabItem tabScan = new TabItem();
            tabScan.Header = "BLE Scan";
            tabScan.Content = new BleScanner();
            //
            TabItem tabConnect = new TabItem();
            tabConnect.Header = "BLE Connect";
            tabConnect.Content = new BleConnect();
            //
            this.tabsBluetoothOptions.Items.Add(tabScan);
            this.tabsBluetoothOptions.Items.Add(tabConnect);
            this.tabsBluetoothOptions.SelectedIndex = 0;
        }
    }
}
