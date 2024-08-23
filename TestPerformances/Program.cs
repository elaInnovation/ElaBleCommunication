using ElaBleCommunication.Wcl.Controllers;
using ElaBleCommunication.Legacy.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using ElaBleCommunication.Spy;

namespace TestPerformances
{
    internal class Program
    {
        private const int TEST_DURATION_SECONDS = 10;
        private const int TAG_PERIOD_MS = 250;
        private static Stopwatch _chrono = new Stopwatch();
        private static int _devicesCount = 0;

        static async Task Main(string[] args)
        {
            //await TestDataflow();
            //Console.WriteLine();
            await TestWcl(29, 29);
            Console.WriteLine();
            await TestWindows();
            Console.WriteLine();
            await TestSpy();
            Console.ReadLine();
        }

        private static async Task TestWcl(ushort interval, ushort window)
        {
            _devicesCount = 0;
            var wclController = new WclBleController();
            wclController.SetRadio();
            var wclScanner = wclController.Scanner;
            wclScanner.evAdvertisementReceived += _scanner_evAdvertisementReceived;

            wclScanner.Start(false, interval, window);
            _chrono.Restart();
            Console.WriteLine($"WCL : scanning for devices for {TEST_DURATION_SECONDS} seconds...");
            await LaunchProgressBar();
            wclScanner.Stop();
            _chrono.Stop();
            wclController.Close();
            Console.WriteLine($"P TPROBE 000DFF seen {_devicesCount} times");

            if (_devicesCount != 0)
            {
                var taux = _devicesCount * 100 / (_chrono.ElapsedMilliseconds / TAG_PERIOD_MS);
                Console.WriteLine($"Taux de reception: {taux}%");
                await Save("WCL direct", taux);
            }
        }

        private static async Task TestWindows()
        {
            _devicesCount = 0;
            var windowsScanner = new ElaBLEAdvertisementWatcher();
            windowsScanner.evAdvertisementReceived += _scanner_evAdvertisementReceived;

            windowsScanner.StartBluetoothScanner();
            _chrono.Restart();
            Console.WriteLine($"WINDOWS : scanning for devices for {TEST_DURATION_SECONDS} seconds...");
            await LaunchProgressBar();
            windowsScanner.StopBluetoothScanner();
            _chrono.Stop();
            Console.WriteLine($"P TPROBE 000DFF seen {_devicesCount} times");
            if (_devicesCount != 0)
            {
                var taux = _devicesCount * 100 / (_chrono.ElapsedMilliseconds / TAG_PERIOD_MS);
                Console.WriteLine($"Taux de reception: {taux}%");
                await Save("WCL direct", taux);
            }
        }

        private static async Task TestDataflow()
        {
            Console.WriteLine("Launch dataflow test? y/n");
            var response = Console.ReadLine();
            if (response != "y") return;

            Console.WriteLine("hostname?");
            var hostname = Console.ReadLine();
            Console.WriteLine("port?");
            var port = Console.ReadLine();

            _devicesCount = 0;
            var dataflowClient = new elaMicroserviceClient.Core.DataFlow.DataFlowBase.ElaDataFlowBaseClient();
            dataflowClient.Connect(hostname, int.Parse(port));
            dataflowClient.Authenticate("admin", "admin");
            dataflowClient.evDataReceived += DataflowClient_evDataReceived;
            dataflowClient.StartDataStreaming();
            _chrono.Restart();
            Console.WriteLine($"Dataflow : scanning for devices for {TEST_DURATION_SECONDS} seconds...");
            await LaunchProgressBar();
            dataflowClient.StopDataStreaming();
            _chrono.Stop();
            dataflowClient.Unauthenticate();
            dataflowClient.Disconnect();
            Console.WriteLine($"P TPROBE 000DFF seen {_devicesCount} times");
            if (_devicesCount != 0) Console.WriteLine($"Taux de reception: {_devicesCount * 100 / (_chrono.ElapsedMilliseconds / TAG_PERIOD_MS)}%");
        }

        private static void DataflowClient_evDataReceived(object sender, elaMicroserviceClient.Core.DataFlow.DataReceivedEventArgs args)
        {
            _scanner_evAdvertisementReceived(args.Data);
        }

        private static async Task TestSpy()
        {
            _devicesCount = 0;
            var spyScanner = new SpyBleScanner();
            await spyScanner.InitializeAsync("COM3", 115000);
            spyScanner._evAdvertisementReceived += _scanner_evAdvertisementReceived;
            await spyScanner.Start();
            _chrono.Restart();
            Console.WriteLine($"SPY : scanning for devices for {TEST_DURATION_SECONDS} seconds...");
            await LaunchProgressBar();
            await spyScanner.Stop();
            _chrono.Stop();
            Console.WriteLine($"P TPROBE 000DFF seen {_devicesCount} times");
            if (_devicesCount != 0) Console.WriteLine($"Taux de reception: {_devicesCount * 100 / (_chrono.ElapsedMilliseconds / TAG_PERIOD_MS)}%");
        }

        private static void _scanner_evAdvertisementReceived(ElaTagClassLibrary.ElaTags.Interoperability.ElaBaseData device)
        {
            if (device.id == "D4:71:9B:80:E0:EF")
            {
                _devicesCount++;
            }
        }

        private static async Task LaunchProgressBar()
        {
            using (var progress = new ProgressBar())
            {
                for (int i = 0; i < TEST_DURATION_SECONDS; i++)
                {
                    progress.Report((double)i / TEST_DURATION_SECONDS);
                    await Task.Delay(1000);
                }
            }
        }

        private static string _savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "BleScanPerfs.csv");
        private static async Task Save(string source, long tauxReception, int? window = null, int? interval = null)
        {
            if (window.HasValue && interval.HasValue)
            {
                await File.AppendAllTextAsync(_savePath, $"{DateTime.Now};{source};{TEST_DURATION_SECONDS};{tauxReception};{window.Value};{interval.Value}\n");
            }
            else
            {
                await File.AppendAllTextAsync(_savePath, $"{DateTime.Now};{source};{TEST_DURATION_SECONDS};{tauxReception}\n");
            }
        }
    }
}
