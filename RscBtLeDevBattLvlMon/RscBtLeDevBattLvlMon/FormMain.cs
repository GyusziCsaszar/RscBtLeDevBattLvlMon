using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// SRC: https://software.intel.com/content/www/us/en/develop/articles/using-winrt-apis-from-desktop-applications.html
// ATTN: There are differences between WinRT 8.1 and WinRT 10!!!

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

// SRC: https://github.com/microsoft/BluetoothLEExplorer

using Windows.Foundation.Metadata;
using Windows.Devices.Enumeration;

// SRC: https://www.andreasjakl.com/read-battery-level-bluetooth-le-devices/

namespace RscBtLeDevBattLvlMon
{
    public partial class FormMain : Form
    {

        // SRC: https://stackoverflow.com/questions/43568096/frombluetoothaddressasync-never-returns-on-windows-10-creators-update-in-wpf-app

        /*
        BluetoothLEAdvertisementWatcher BleWatcher = null;
        */

        // SRC: https://github.com/microsoft/BluetoothLEExplorer

        private const string BTLEDeviceWatcherAQSString = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

        /*
        private const string BatteryLevelGUID = "{995EF0B0-7EB3-4A8B-B9CE-068BB3F4AF69} 10";
        private const string BluetoothDeviceAddress = "System.DeviceInterface.Bluetooth.DeviceAddress";
        */

        /*
        private const string BatteryServiceGUID = "0000180f-0000-1000-8000-00805f9b34fb";
        */
        public static readonly Guid UuidBatteryService = new Guid("0000180f-0000-1000-8000-00805f9b34fb");

        private DeviceWatcher deviceWatcher;

        public static FormMain s_MainForm = null;

        public FormMain()
        {
            InitializeComponent();

            s_MainForm = this;
        }

        private delegate void Delegate_LogMessage(string sMessage);
        public static void LogMessage(string sMessage)
        {
            if (s_MainForm.InvokeRequired)
            {
                Delegate_LogMessage d = new Delegate_LogMessage(LogMessage);
                s_MainForm.BeginInvoke(d, sMessage);
            }
            else
            {
                if (sMessage.Length == 0)
                {
                    s_MainForm.lbLog.Items.Clear();
                }
                else
                {
                    s_MainForm.lbLog.Items.Add(sMessage);
                }
            }
        }

        private delegate void Delegate_WatcherStatusChanged();
        public static void WatcherStatusChanged()
        {
            if (s_MainForm.InvokeRequired)
            {
                Delegate_WatcherStatusChanged d = new Delegate_WatcherStatusChanged(WatcherStatusChanged);
                s_MainForm.BeginInvoke(d);
            }
            else
            {
                switch(s_MainForm.deviceWatcher.Status)
                {

                    case DeviceWatcherStatus.Stopped:
                    {
                        s_MainForm.btnEnum.Enabled = true;
                        s_MainForm.btnStop.Enabled = false;
                        break;
                    }

                }
            }
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            // TODO...
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (deviceWatcher != null && 
                (deviceWatcher.Status == DeviceWatcherStatus.Started ||
                 deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted))
            {
                deviceWatcher.Stop();
            }
        }

        private void btnEnum_Click(object sender, EventArgs e)
        {
            LogMessage(""); // Clears LOG...

            //
            ////
            //

            //var localAdapter = await BluetoothAdapter.GetDefaultAsync();

            Windows.Foundation.IAsyncOperation<BluetoothAdapter> tsk;

            tsk = BluetoothAdapter.GetDefaultAsync();

            while (tsk.Status == Windows.Foundation.AsyncStatus.Started)
            {
                System.Threading.Thread.Sleep(100);
            }

            var localAdapter = tsk.GetResults();

            LogMessage("Low energy supported? -> " + localAdapter.IsLowEnergySupported);
            LogMessage("Central role supported? -> " + localAdapter.IsCentralRoleSupported);
            LogMessage("Pheripherial role supported? -> " + localAdapter.IsPeripheralRoleSupported);

            //
            ////
            //

            // SRC: https://stackoverflow.com/questions/43568096/frombluetoothaddressasync-never-returns-on-windows-10-creators-update-in-wpf-app

            /*
            // To enumerate available devices!!!

            BleWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            BleWatcher.Received += Watcher_Received;
            BleWatcher.Start();
            */

            //
            ////
            //

            // SRC: https://github.com/microsoft/BluetoothLEExplorer

            // Additional properties we would like about the device.
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
            {
                string[] requestedProperties =
                {
                    "System.Devices.GlyphIcon",
                    "System.Devices.Aep.Category",
                    "System.Devices.Aep.ContainerId",
                    "System.Devices.Aep.DeviceAddress",
                    "System.Devices.Aep.IsConnected",
                    "System.Devices.Aep.IsPaired",
                    "System.Devices.Aep.IsPresent",
                    "System.Devices.Aep.ProtocolId",
                    "System.Devices.Aep.Bluetooth.Le.IsConnectable",
                    "System.Devices.Aep.SignalStrength",
                    "System.Devices.Aep.Bluetooth.LastSeenTime",
                    "System.Devices.Aep.Bluetooth.Le.IsConnectable",
                };

                // BT_Code: Currently Bluetooth APIs don't provide a selector to get ALL devices that are both paired and non-paired.
                deviceWatcher = DeviceInformation.CreateWatcher(
                    BTLEDeviceWatcherAQSString,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint);
            }
            else
            {
                string[] requestedProperties =
                {
                    "System.Devices.GlyphIcon",
                    "System.Devices.Aep.Category",
                    "System.Devices.Aep.ContainerId",
                    "System.Devices.Aep.DeviceAddress",
                    "System.Devices.Aep.IsConnected",
                    "System.Devices.Aep.IsPaired",
                    "System.Devices.Aep.IsPresent",
                    "System.Devices.Aep.ProtocolId",
                    "System.Devices.Aep.Bluetooth.Le.IsConnectable",
                    "System.Devices.Aep.SignalStrength",
               };

                // BT_Code: Currently Bluetooth APIs don't provide a selector to get ALL devices that are both paired and non-paired.
                deviceWatcher = DeviceInformation.CreateWatcher(
                    BTLEDeviceWatcherAQSString,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint);
            }

            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            deviceWatcher.Start();

            btnEnum.Enabled = false;
            btnStop.Enabled = true;

        }

        // SRC: https://stackoverflow.com/questions/43568096/frombluetoothaddressasync-never-returns-on-windows-10-creators-update-in-wpf-app

        /*
        private /*async* void Watcher_Received(BluetoothLEAdvertisementWatcher sender,
                                           BluetoothLEAdvertisementReceivedEventArgs args)
        {
            LogMessage("Bluetooth LE Device Advertisement Local Name -> " + args.Advertisement.LocalName);
  
            Windows.Foundation.IAsyncOperation<BluetoothLEDevice> tsk1;

            tsk1 = BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

            while (tsk1.Status == Windows.Foundation.AsyncStatus.Started)
            {
                System.Threading.Thread.Sleep(100);
            }

            var bluetoothLeDevice = tsk1.GetResults();

            LogMessage("                    Address -> " + args.BluetoothAddress);
            LogMessage("          Connection Status -> " + bluetoothLeDevice.ConnectionStatus);
            LogMessage("                       Name -> " + bluetoothLeDevice.Name);
            LogMessage("                  Device ID -> " + bluetoothLeDevice.DeviceId);

            //
            ////
            //

            Windows.Foundation.IAsyncOperation<Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceServicesResult> tsk2;

            tsk2 = bluetoothLeDevice.GetGattServicesAsync();

            while (tsk2.Status == Windows.Foundation.AsyncStatus.Started)
            {
                System.Threading.Thread.Sleep(100);
            }

            var gattServices = tsk2.GetResults();

            foreach (var curService in gattServices.Services)
            {
                LogMessage("  Service: " + curService.Uuid);
            }

            // TODO...

            bluetoothLeDevice.Dispose();
        }
        */

        private /* async */ void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    LogMessage("---------------------- Device Watcher - ADDED");

                    //await AddDeviceToList(deviceInfo);

                    bool isConnectible = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.Bluetooth.Le.IsConnectable") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"]));

                    bool isConnected = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsConnected") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.IsConnected"]));

                    bool isPaired = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsPaired") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.IsPaired"]));

                    // Let's make it connectable by default, we have error handles in case it doesn't work
                    bool shouldDisplay =
                        isConnectible ||
                        isConnected ||
                        isPaired;

                    if (true) //shouldDisplay)
                    {
                        // HAS NOT!!!
                        /*
                        bool batteryLevelGUID = (deviceInfo.Properties.Keys.Contains(BatteryLevelGUID) &&
                                    deviceInfo.Properties[BatteryLevelGUID] != null);

                        LogMessage("  Has Battery Level GUID: " + batteryLevelGUID);

                        bool bluetoothDeviceAddress = (deviceInfo.Properties.Keys.Contains(BluetoothDeviceAddress) &&
                                    deviceInfo.Properties[BluetoothDeviceAddress] != null);

                        LogMessage("  Has Bluetooth Device Address: " + bluetoothDeviceAddress);
                        */

                        LogMessage("Device Name: " + deviceInfo.Name);

                        LogMessage("Is Connectible: " + isConnectible);
                        LogMessage("Is Connected: " + isConnected);
                        LogMessage("Is Paired: " + isPaired);

                        string sDeviceAddress = "";
                        ulong ulDeviceAddress = 0;
                        if (deviceInfo.Properties.ContainsKey("System.Devices.Aep.DeviceAddress"))
                        {
                            sDeviceAddress = deviceInfo.Properties["System.Devices.Aep.DeviceAddress"].ToString();
                            ulDeviceAddress = Convert.ToUInt64(sDeviceAddress.Replace(":", String.Empty), 16);
                        }
                        LogMessage("Device Address: " + sDeviceAddress + " (ULONG: " + ulDeviceAddress + ")");

                        if (ulDeviceAddress != 0)
                        {

                            Windows.Foundation.IAsyncOperation<BluetoothLEDevice> tsk1;

                            tsk1 = BluetoothLEDevice.FromBluetoothAddressAsync(ulDeviceAddress);

                            while (tsk1.Status == Windows.Foundation.AsyncStatus.Started)
                            {
                                System.Threading.Thread.Sleep(100);
                            }

                            var bluetoothLeDevice = tsk1.GetResults();

                            LogMessage("Connection Status: " + bluetoothLeDevice.ConnectionStatus);
                            LogMessage("Name: " + bluetoothLeDevice.Name);
                            LogMessage("Device ID: " + bluetoothLeDevice.DeviceId);

                            //
                            ////
                            //

                            Windows.Foundation.IAsyncOperation<Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceServicesResult> tsk2;

                            tsk2 = bluetoothLeDevice.GetGattServicesAsync();

                            while (tsk2.Status == Windows.Foundation.AsyncStatus.Started)
                            {
                                System.Threading.Thread.Sleep(100);
                            }

                            var gattServices = tsk2.GetResults();

                            LogMessage("Service Count: " + gattServices.Services.Count);

                            foreach (var curService in gattServices.Services)
                            {
                                LogMessage(" - Service GUID: " + curService.Uuid);

                                /*
                                if (curService.Uuid.ToString().ToLower() == BatteryServiceGUID)
                                */
                                if (curService.Uuid.Equals(UuidBatteryService))
                                {
                                    LogMessage("   Battery Service!");

                                    Windows.Foundation.IAsyncOperation<Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicsResult> tsk3;

                                    tsk3 = curService.GetCharacteristicsAsync();

                                    while (tsk3.Status == Windows.Foundation.AsyncStatus.Started)
                                    {
                                        System.Threading.Thread.Sleep(100);
                                    }

                                    var gattCharacteristics = tsk3.GetResults();

                                    LogMessage("   Characteristic Count: " + gattCharacteristics.Characteristics.Count);

                                    foreach (var curCharacteristic in gattCharacteristics.Characteristics)
                                    {
                                        LogMessage("   Characteristic Handle: " + curCharacteristic.AttributeHandle);
                                        LogMessage("   Characteristic GUID: " + curCharacteristic.Uuid);

                                        if (curCharacteristic.CharacteristicProperties.HasFlag(
                                            Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Read))
                                        {
                                            Windows.Foundation.IAsyncOperation<Windows.Devices.Bluetooth.GenericAttributeProfile.GattReadResult> tsk4;
                                                
                                            tsk4 = curCharacteristic.ReadValueAsync();

                                            while (tsk4.Status == Windows.Foundation.AsyncStatus.Started)
                                            {
                                                System.Threading.Thread.Sleep(100);
                                            }

                                            var result = tsk4.GetResults();

                                            var reader = Windows.Storage.Streams.DataReader.FromBuffer(result.Value);
                                            var input = new byte[reader.UnconsumedBufferLength];
                                            reader.ReadBytes(input);

                                            LogMessage("   Characteristic Value (HEX): 0x" + BitConverter.ToString(input));

                                            LogMessage("   Characteristic Value (Dec):   " + input[0].ToString());
                                        }
                                    }
                                }
                            }

                            //
                            ////
                            //

                            bluetoothLeDevice.Dispose();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                LogMessage("DeviceWatcher_Added - ERROR: " + ex.Message);
            }
        }

        private /* async */ void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {

                    // NOTE: Too many calls!
                    /*
                    LogMessage("---------------------- Device Watcher - UPDATED");
                    LogMessage("  DeviceWatcher_Updated - Device Id: " + deviceInfoUpdate.Id);
                    */

                }
            }
            catch (Exception ex)
            {
                LogMessage("DeviceWatcher_Updated - ERROR: " + ex.Message);
            }
        }

        private /* async */ void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {

                    LogMessage("---------------------- Device Watcher - REMOVED");
                    LogMessage("Device ID: " + deviceInfoUpdate.Id);

                    string sDeviceAddress = "";
                    ulong ulDeviceAddress = 0;
                    if (deviceInfoUpdate.Properties.ContainsKey("System.Devices.Aep.DeviceAddress"))
                    {
                        sDeviceAddress = deviceInfoUpdate.Properties["System.Devices.Aep.DeviceAddress"].ToString();
                        ulDeviceAddress = Convert.ToUInt64(sDeviceAddress.Replace(":", String.Empty), 16);
                    }
                    LogMessage("Device Address: " + sDeviceAddress + " (ULONG: " + ulDeviceAddress + ")");

                }
            }
            catch (Exception ex)
            {
                LogMessage("DeviceWatcher_Removed - ERROR: " + ex.Message);
            }
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {

                    LogMessage("---------------------- Device Watcher - ENUMERATION COMPLETED");

                }
            }
            catch (Exception ex)
            {
                LogMessage("DeviceWatcher_EnumerationCompleted - ERROR: " + ex.Message);
            }
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {

                    LogMessage("---------------------- Device Watcher - STOPPED");

                    WatcherStatusChanged();

                }
            }
            catch (Exception ex)
            {
                LogMessage("DeviceWatcher_Removed - ERROR: " + ex.Message);
            }
        }
    }
}
