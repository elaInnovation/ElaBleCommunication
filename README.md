# ElaBleCommunication

ELA Innovation provide this project for Visual Studion 2019 to help users to intergrate easily the Tags provide by ELA Innovation Company. You can directly use our Nuger Package **ElaBleCommunication** available on nuget.org or clone this project from Github. This project contains the **code of the Nuget Packag**e and a simple **User interface** to manage **Bluetooth scanner** for your windows project and use Gatt and Services to use **the connected mode** from our Bluetooth tag.

## Build

Before starting, please download [Visual Studio 2019 Community][here_VS_Community] and install it. Then, to build the application, open the solution file (.sln) using [Visual Studio 2019 Community][here_VS_Community] and Generate the solution. You can use the UI Application **ElaBleCommunicationUI** as a sample to know how to use the **ElaBleCommunication** package.

## Nuget

You can fing directly ElaBleCommunication package on [nuget.org][here_nuger_org]. Use the Package manager to install and use the package.

## Sample

You will find some sample for the integration in project **ElaBleCommunicationUI**. Use the two different Views to scan or connect to the tags provided by ELA Innovation.

### Scanner

To start the sanner, create a new instance of **ElaBLEAdvertisementWatcher** object, associate the event to get all informations of the Bluetooth device packaged in **ElaBleDevice** object and start it by calling method **startBluetoothScanner**.
```csharp
   ElaBLEAdvertisementWatcher scanner = new ElaBLEAdvertisementWatcher();
   scanner.evAdvertisementReceived += Scanner_evAdvertisementReceived;
   scanner.startBluetoothScanner();
```

Stop the scanner by calling the method **stopBluetoothScanner**.
```csharp
   scanner.stopBluetoothScanner();
```

### Connect

To connect to an ELA Innovation Tag, you need to know the target mac address (value seen in **ElaBleDevice** object during a scan). Then, you can create a new instance of **ElaBLEConnector** connectDeviceAsync function to connect to the tag. The format of the mac address is an hexadecimal string (example: C7:05:B6:F3:31:3A). You can associate the reception event if you want to display or share the feedback fro the tag.
```csharp
   ElaBLEConnector bleconnection = new ElaBLEConnector();
   bleconnection.evResponseReceived += Bleconnection_evResponseReceived;
   uint errorConnect = await bleconnection.connectDeviceAsync("C7:05:B6:F3:31:3A");
```

You can send as many commands as you want once you're connected. When all the operation are finished, call **disconnectDeviceAsync** function to disconnect from the tag.
```csharp
    uint error = bleconnection.disconnectDeviceAsync();
```

To send commandes, use the function **sendCommandAsync** to send the target command, the associated password configured for the tag, add arguments is requested. For more informations about the available commands, please refer to the documentation on our webite. The following sample provide an example to send the command "LED_ON" to light the LED on our tags.
```csharp
    uint errorSend = await bleconnection.sendCommandAsync("LED_ON");
```

[here_VS_Community]: https://visualstudio.microsoft.com/fr/downloads/

[here_nuger_org]: https://www.nuget.org/packages/ElaBleCommunication/1.0.0


