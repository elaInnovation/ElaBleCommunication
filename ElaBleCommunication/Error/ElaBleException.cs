using System;
using System.Collections.Generic;
using System.Text;

/**
 * \namespace ElaBluetoothCommunication.Error
 * \brief namespace associated to the errors from the library
 */
namespace ElaBleCommunication.Error
{
    /**
     * \class ElaBleException
     * \brief Ela Bluetooth Exception
     */
    public class ElaBleException : Exception
    {
        /** \brief additionnal exception information */
        public string AdditionnalInformation { get; set; }

        /** \brief constructor */
        public ElaBleException(String message, Exception ex) : base(ex.Message, ex) {
            this.AdditionnalInformation = message;
        }
    }
}
