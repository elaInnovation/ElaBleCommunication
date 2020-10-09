using System;
using System.Collections.Generic;
using System.Text;

namespace ElaBluetoothCommunication.Error
{
    /**
     * \class ErrorHandler
     * \brief class to 
     */
    public class ErrorHandler
    {
        /** \brief ERR_OK no problems occurs */
        public static uint ERR_OK = 0;
        /** \brief ERR_KO an unhandled error occurs */
        public static uint ERR_KO = 1;
        /** \brief ERR_UNHANDLED_EXCEPT an unhandled exception occurs during the execution */
        public static uint ERR_UNHANDLED_EXCEPTION = 1;
        /** \brief ERR_SCANNER_ALREADY_STARTED : the scanner is in started state */
        public static uint ERR_SCANNER_ALREADY_STARTED = 2;
        /** \brief ERR_SCANNER_ALREADY_STOPPED : the scanner is in stopped state */
        public static uint ERR_SCANNER_ALREADY_STOPPED = 3;
        /** \brief mac address not found */
        public static uint ERR_CONNECT_MAC_NOT_FOUND = 4;
        /** \brief cannot send the specific command to the tag */
        public static uint ERR_SEND_COMMAND_ERROR = 5;
        /** \brief the nordic uart is unfound */
        public static uint ERR_UNFOUND_NORDIC_SERVICE = 6;
        /** \brief the characteristics from nordic Uart are unfound */
        public static uint ERR_UNFOUND_NORDIC_CHARACTERISTICS = 7;
        /** \brief cannot use nordic tx during send command */
        public static uint ERR_CANNOT_WRITE_ON_NORDIC_TX = 8;
        /** \brief cannot use nordic uart non initialized */
        public static uint ERR_NORDIC_UART_UNITIALIZED = 9;
        /** \brief the connection is not established in bluetooth discovery */
        public static uint ERR_NOT_CONNECTED = 10;
    }
}
