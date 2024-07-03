using ElaBleCommunication.Wcl.Controllers;
using ElaBleCommunication.Wcl.Models;
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
        public static WclBleController BleController { get; private set; } 
#endif

        /** \brief constructor */
        public MainWindow()
        {
#if WCL
            BleController = new WclBleController(AppTypeEnum.UI);
#endif
            Loaded += MainWindow_Loaded;
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
