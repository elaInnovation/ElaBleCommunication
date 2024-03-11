using ElaBleCommunication.Wcl;
using ElaBleCommunicationUI.Views;
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

namespace ElaBleCommunicationUI
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if WCL
        public static WclBleController BleController { get; } = new WclBleController();
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
