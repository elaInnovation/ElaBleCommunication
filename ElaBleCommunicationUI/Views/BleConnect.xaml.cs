using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ElaSoftwareCommon.Error;
using ElaBleCommunication.Wcl;
using ElaBleCommunication.Legacy.Windows;


namespace ElaBleCommunicationUI.Views
{
    /// <summary>
    /// Logique d'interaction pour BleConnect.xaml
    /// </summary>
    public partial class BleConnect : UserControl
    {
        //
        // nordic uart service
        public const string nordicUartService = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
        public const string nordicUartTxCharacteristic = "6e400002-b5a3-f393-e0a9-e50e24dcca9e";
        public const string nordicUartRxCharacteristic = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";

        /** \brief bluetooth connector */
#if WCL
        private WclBLEConnector bleconnection = MainWindow.BleController.Connector;
#else
        private ElaBLEConnector bleconnection = new ElaBLEConnector();
#endif



        /** \brief constructor */
        public BleConnect()
        {
            InitializeComponent();
            //
            bleconnection.evResponseReceived += Bleconnection_evResponseReceived;
        }

        /**
         * \fn Bleconnection_evResponseReceived
         * \brief event when a new response has been received from the tag
         */
        private void Bleconnection_evResponseReceived(byte[] response)
        {
            DispatcherOperation op = Dispatcher.BeginInvoke((Action)(() =>
            {
                writeConsole($"<<<Received from tag : {Encoding.UTF8.GetString(response)}");
            }));
        }

        /**
         * \fn writeConsole
         * \brief write a message into the console
         * \param [in] message : target message to display into the console
         */
        private void writeConsole(String message)
        {
            this.lvTagExchange.Items.Add(message);
        }

        #region event declaration
        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            writeConsole($">>>Sending command : {this.tbMacCommandValue.Text}");
            try
            {
                uint errorSend = await bleconnection.SendCommandAsync(tbMacCommandValue.Text, this.tbMacPasswordValue.Text, this.tbMacargsValue.Text);
                if (errorSend != ErrorServiceHandlerBase.ERR_OK)
                {
                    writeConsole($"Send function return error code : {errorSend} ({ErrorServiceHandlerBase.getErrorMessage(errorSend)})");
                }
                else
                {
                    writeConsole("Send command SUCCESS !!!");
                }
            }
            catch (Exception ex)
            {
                writeConsole($"An exception occurs while sending command : {ex.Message}");
            }
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            writeConsole($"Try to connect to tag : {this.tbMacAddressValue.Text}");
            try
            {
                uint errorConnect = await bleconnection.ConnectDeviceAsync(this.tbMacAddressValue.Text);

                if (errorConnect != ErrorServiceHandlerBase.ERR_OK)
                {
                    writeConsole($"Connect function return error code : {errorConnect} ({ErrorServiceHandlerBase.getErrorMessage(errorConnect)})");
                }
                else
                {
                    writeConsole("Connection SUCCESS !!!");
                }
            }
            catch (Exception ex)
            {
                writeConsole($"An exception occurs in connection : {ex.Message}");
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                uint error = bleconnection.DisconnectDevice();
                if (error != ErrorServiceHandlerBase.ERR_OK)
                {
                    writeConsole($"Disconnect function return error code : {error} ({ErrorServiceHandlerBase.getErrorMessage(error)})");
                }
                else
                {
                    writeConsole("Disconnection SUCCESS !!!");
                }
            }
            catch (Exception ex)
            {
                writeConsole($"An exception occurs in disconnection : {ex.Message}");
            }
        }
#endregion
    }
}
