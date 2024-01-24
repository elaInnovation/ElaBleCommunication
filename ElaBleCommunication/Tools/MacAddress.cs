using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ElaBleCommunication.Tools
{
    /**
     * \class MacAddress
     * \brief class to manage mac address from ulong to string
     */
    public class MacAddress
    {
        public enum MacAddressType
        {
            Decimal,
            Hexadecimal
        }

        /** function to convert a mac adress to ulong */
        public static ulong macAdressHexaToULong(String input)
        {
            String mac = input.Replace(":", "");

            ulong ulmacadress = 0;
            ulmacadress = Convert.ToUInt64(mac, 16);
            return ulmacadress;
        }

        /** function to convert a mac adress to long */
        public static long macAdressHexaToLong(String input)
        {
            String mac = input.Replace(":", "");

            long ulmacadress = 0;
            ulmacadress = Convert.ToInt64(mac, 16);
            return ulmacadress;
        }

        /** getter on the hexa bluetooth adress 
         * @param [in] adress : associated mac adress
         * @return hexa string associated to mac adress
         */
        public static String macAdressLongToHexa(ulong adress)
        {
            return Regex.Replace(
                        String.Format("{0:X}", adress),
                        "([0-9A-F]{2})(?!$)",
                        "$1:");
        }

        /** getter on the hexa bluetooth adress 
         * @param [in] adress : associated mac adress
         * @return hexa string associated to mac adress
         */
        public static String macAdressLongToHexa(long adress)
        {
            return Regex.Replace(
                        String.Format("{0:X}", adress),
                        "([0-9A-F]{2})(?!$)",
                        "$1:");
        }

        /** getter on the mac address type, according to the input string 
         * @param [in] macAddress : input mac address
         * @return MacAddressType target type
         */
        public static MacAddressType getType(String macAddress)
        {
            if (macAddress.Contains(":"))
            {
                return MacAddressType.Hexadecimal;
            }
            else
            {

                return MacAddressType.Decimal;
            }
        }

        /** getter on the trusted ulong mac addres */
        public static String getULongMacAdress(String address)
        {
            if (getType(address).Equals(MacAddressType.Hexadecimal))
            {
                return macAdressHexaToULong(address).ToString();
            }
            else
            {
                return address;
            }
        }

        /** getter on the trusted ulong mac addres */
        public static String getHexaMacAdress(String address)
        {
            if (getType(address).Equals(MacAddressType.Decimal))
            {
                ulong ulAddress = 0;
                ulong.TryParse(address, out ulAddress);
                return macAdressLongToHexa(ulAddress);
            }
            else
            {
                return address;
            }
        }
    }
}
