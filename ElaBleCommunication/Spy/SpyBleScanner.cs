using ElaTagClassLibrary.ElaTags.Interoperability.Model;
using ElaTagClassLibrary.ElaTags.Interoperability;
using ElaTagClassLibrary.ElaTags;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ElaSoftwareCommon.Error;
using ElaBleCommunication.Common;

namespace ElaBleCommunication.Spy
{
    public class SpyBleScanner
    {
        private const string KEY_SEP = "\r\n";
        private string _bufferStr = string.Empty;
        private Regex _regexData = new Regex("(?<payload>([0-9a-fA-F]{2})+);(?<macAddress>[0-9a-fA-F]{12});(?<name>[^;]*);(?<rssi>-[0-9]+);(?<size>[0-9]+);");
        private const string _regexMac = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
        private const string _regexReplaceMac = "$1:$2:$3:$4:$5:$6";

        private SerialPort _serialPort;

        public event NewAdvertismentReceived _evAdvertisementReceived = null;

        public Task InitializeAsync(string comPort, int baudRate)
        {
            var detectedPorts = SerialPort.GetPortNames();
            if (!detectedPorts.Contains(comPort)) throw new Exception($"Could not find port {comPort}");

            _serialPort = new SerialPort(comPort);
            _serialPort.BaudRate = baudRate;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.DataBits = 8;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Encoding = Encoding.ASCII;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            return Task.CompletedTask;
        }

        public Task<uint> Start()
        {
            _bufferStr = string.Empty;
            _serialPort.Open();
            return Task.FromResult(ErrorServiceHandlerBase.ERR_OK);
        }

        public Task<uint> Stop()
        {
            _serialPort.Close();
            _bufferStr = string.Empty;
            return Task.FromResult(ErrorServiceHandlerBase.ERR_OK);
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer;

            try
            {
                if (!_serialPort.IsOpen) return;
                buffer = new byte[_serialPort.BytesToRead];
                _serialPort.Read(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            _bufferStr += Encoding.ASCII.GetString(buffer);
            var frames = _bufferStr.Split(new string[] { KEY_SEP }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var frame in frames)
            {
                Match regexMatch = _regexData.Match(frame);
                if (regexMatch.Success)
                {
                    string macAddress = Regex.Replace(regexMatch.Groups["macAddress"].Value, _regexMac, _regexReplaceMac);
                    string payload = regexMatch.Groups["payload"].Value;

                    var data = InteroperableDeviceFactory.getInstance().get(ElaTagTechno.Bluetooth, payload);
                    data.id = macAddress;
                    if (int.TryParse(regexMatch.Groups["rssi"].Value, out int rssi)) data.rssi = rssi;
                    if (data.identification is null) data.identification = new ElaIdenficationObject();
                    data.identification.macaddress = macAddress;
                    data.version = ElaModelVersion.get();
                    data.payload = payload;

                    _evAdvertisementReceived?.Invoke(data);
                }
            }

            if (_bufferStr.Contains(KEY_SEP))
                _bufferStr = _bufferStr.Substring(_bufferStr.LastIndexOf(KEY_SEP));
            else
                _bufferStr = string.Empty;
        }
    }
}
