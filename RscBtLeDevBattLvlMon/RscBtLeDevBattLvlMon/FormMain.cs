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
using System.Text.RegularExpressions;

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

        public static bool s_bCloseOnStop = false;

        public List<BtLeDevInfo> m_aDevices = new List<BtLeDevInfo>();

        public NotifyIcon m_NotifyIcon;

        public FormMain()
        {
            InitializeComponent();

            s_MainForm = this;
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {

            lvDevices.Columns.Add("Battery Level");
            lvDevices.Columns.Add("Name");
            lvDevices.Columns.Add("Status");
            lvDevices.Columns.Add("Service Count");
            lvDevices.Columns.Add("MAC Address");

            // TODO...
        }
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (deviceWatcher != null)
            {
                if (deviceWatcher.Status == DeviceWatcherStatus.Started ||
                 deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    s_bCloseOnStop = true;
                    deviceWatcher.Stop();

                    e.Cancel = true;
                }
                else if (deviceWatcher.Status == DeviceWatcherStatus.Stopping)
                {
                    e.Cancel = true;
                }
            }

            if (!e.Cancel)
            {
                // TODO...
            }
        }

        private void btnTogleIcon_Click(object sender, EventArgs e)
        {
            if (m_NotifyIcon == null)
            {
                
                m_NotifyIcon = new NotifyIcon();

                //m_NotifyIcon.Icon = SystemIcons.Exclamation;

                // SRC: https://stackoverflow.com/questions/25403169/get-application-icon-of-c-sharp-winforms-app
                //m_NotifyIcon.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);

                // SRC: https://stackoverflow.com/questions/34075264/i-want-to-display-numbers-on-the-system-tray-notification-icons-on-windows
                Brush brush = new SolidBrush(Color.White);
                Brush brushBk = new SolidBrush(Color.DodgerBlue);
                // Create a bitmap and draw text on it
                Bitmap bitmap = new Bitmap(24, 24); // 32, 32); // 16, 16);
                Graphics graphics = Graphics.FromImage(bitmap);
                //graphics.DrawRectangle(new Pen(Color.Red), new Rectangle(0, 0, 23, 23));
                graphics.FillEllipse(brushBk, new Rectangle(3, 0, 23 - 4, 23 - 12));
                graphics.DrawEllipse(new Pen(Color.DodgerBlue), new Rectangle(3, 0, 23 - 4, 23 - 12));
                Font font = new Font("Tahoma", 13); // 18);
                graphics.DrawString("99", font, brush, 2, 4);
                // Convert the bitmap with text to an Icon
                m_NotifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());

                m_NotifyIcon.Visible = true;
            }
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
                switch (s_MainForm.deviceWatcher.Status)
                {

                    case DeviceWatcherStatus.EnumerationCompleted:
                        {
                            if (s_MainForm.chbAutoStopOnEnumComp.Checked)
                            {
                                s_MainForm.deviceWatcher.Stop();
                            }
                            break;
                        }

                    case DeviceWatcherStatus.Stopped:
                        {
                            s_MainForm.btnEnum.Enabled = true;
                            s_MainForm.chbAutoStopOnEnumComp.Enabled = true;
                            s_MainForm.btnStop.Enabled = false;

                            if (s_bCloseOnStop)
                            {
                                s_MainForm.Close();
                            }

                            break;
                        }

                }
            }
        }

        private delegate void Delegate_UpdateDevice(BtLeDevInfo btLeDevInfo);
        public static void UpdateDevice(BtLeDevInfo btLeDevInfo)
        {
            if (s_MainForm.InvokeRequired)
            {
                Delegate_UpdateDevice d = new Delegate_UpdateDevice(UpdateDevice);
                s_MainForm.BeginInvoke(d, btLeDevInfo);
            }
            else
            {
                int iIdxItem = -1;

                bool bHit = false;
                int iIdx = -1;
                foreach (BtLeDevInfo btLeDevInfoItem in s_MainForm.m_aDevices)
                {
                    iIdx++;

                    if (btLeDevInfoItem.MacAddress == btLeDevInfo.MacAddress)
                    {
                        iIdxItem = iIdx;

                        bHit = true;

                        /*
                        // SRC: https://stackoverflow.com/questions/930433/apply-properties-values-from-one-object-to-another-of-the-same-type-automaticall
                        foreach (System.Reflection.PropertyInfo property in typeof(BtLeDevInfo).GetProperties().Where(p => p.CanWrite))
                        {
                            property.SetValue(btLeDevInfoItem, property.GetValue(btLeDevInfo, null), null);
                        }
                        */

                        btLeDevInfoItem.Reason          = btLeDevInfo.Reason;
                        btLeDevInfoItem.isConnectible   = btLeDevInfo.isConnectible;
                        btLeDevInfoItem.isConnected     = btLeDevInfo.isConnected;
                        btLeDevInfoItem.isPaired        = btLeDevInfo.isPaired;
                        btLeDevInfoItem.ServiceCount    = btLeDevInfo.ServiceCount;

                        // ATTN!!!
                        btLeDevInfo = btLeDevInfoItem;

                        string sBatteryLevel = "";
                        if (btLeDevInfo.BatteryLevel > 0)
                        {
                            sBatteryLevel = btLeDevInfo.BatteryLevel.ToString() + " %";
                        }

                        s_MainForm.lvDevices.Items[iIdxItem].Text = sBatteryLevel;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[1].Text = btLeDevInfo.Name;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[2].Text = btLeDevInfo.StatusText;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[3].Text = btLeDevInfo.ServiceCount.ToString();
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[4].Text = btLeDevInfo.MacAddress;

                        break;
                    }
                }

                if (!bHit)
                {
                    s_MainForm.m_aDevices.Add(btLeDevInfo);

                    string sBatteryLevel = "";
                    if (btLeDevInfo.BatteryLevel > 0)
                    {
                        sBatteryLevel = btLeDevInfo.BatteryLevel.ToString() + " %";
                    }

                    s_MainForm.lvDevices.Items.Add(sBatteryLevel);

                    iIdxItem = s_MainForm.lvDevices.Items.Count - 1;

                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.Name);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.StatusText);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.ServiceCount.ToString());
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.MacAddress);
                }

                if (iIdxItem >= 0)
                {
                    bool bColored = false;

                    if ((btLeDevInfo.Reason != BtLeDevInfo_Reason.REMOVED)
                        && (btLeDevInfo.isConnected))
                    {
                        bColored = true;

                        s_MainForm.lvDevices.Items[iIdxItem].BackColor = Color.Blue;
                        s_MainForm.lvDevices.Items[iIdxItem].ForeColor = Color.White;
                    }

                    if (!bColored)
                    {
                        switch (btLeDevInfo.Reason)
                        {

                            case BtLeDevInfo_Reason.ADDED:
                                s_MainForm.lvDevices.Items[iIdxItem].BackColor = SystemColors.Window;
                                s_MainForm.lvDevices.Items[iIdxItem].ForeColor = SystemColors.WindowText;
                                break;

                            case BtLeDevInfo_Reason.REMOVED:
                                s_MainForm.lvDevices.Items[iIdxItem].BackColor = SystemColors.Control;
                                s_MainForm.lvDevices.Items[iIdxItem].ForeColor = SystemColors.ControlText;
                                break;
                        }
                    }
                }

            }
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

            if (deviceWatcher == null)
            {
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

            }

            deviceWatcher.Start();

            btnEnum.Enabled = false;
            chbAutoStopOnEnumComp.Enabled = false;
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

        private void QueryBtLeDevice(BluetoothLEDevice bluetoothLeDevice, BtLeDevInfo btLeDevInfo)
        {

            if (bluetoothLeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                btLeDevInfo.isConnected = true;
            }
            else
            {
                btLeDevInfo.isConnected = false;
            }

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

            btLeDevInfo.ServiceCount = gattServices.Services.Count;

            btLeDevInfo.BatteryLevel = 0;

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

                            btLeDevInfo.BatteryLevel = (int)input[0];
                        }
                    }
                }
            }

        }

        private /* async */ void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    LogMessage("---------------------- Device Watcher - ADDED");

                    BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                    btLeDevInfo.Reason = BtLeDevInfo_Reason.ADDED;

                    //await AddDeviceToList(deviceInfo);

                    btLeDevInfo.isConnectible = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.Bluetooth.Le.IsConnectable") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"]));

                    btLeDevInfo.isConnected = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsConnected") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.IsConnected"]));

                    btLeDevInfo.isPaired = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsPaired") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.IsPaired"]));

                    // Let's make it connectable by default, we have error handles in case it doesn't work
                    bool shouldDisplay =
                        btLeDevInfo.isConnectible ||
                        btLeDevInfo.isConnected ||
                        btLeDevInfo.isPaired;

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

                        btLeDevInfo.Name = deviceInfo.Name;

                        LogMessage("Is Connectible: " + btLeDevInfo.isConnectible);
                        LogMessage("Is Connected: " + btLeDevInfo.isConnected);
                        LogMessage("Is Paired: " + btLeDevInfo.isPaired);

                        string sDeviceAddress = "";
                        btLeDevInfo.MacAddressUlong = 0;
                        if (deviceInfo.Properties.ContainsKey("System.Devices.Aep.DeviceAddress"))
                        {
                            sDeviceAddress = deviceInfo.Properties["System.Devices.Aep.DeviceAddress"].ToString();
                            btLeDevInfo.MacAddressUlong = Convert.ToUInt64(sDeviceAddress.Replace(":", String.Empty), 16);
                        }
                        LogMessage("Device Address: " + sDeviceAddress + " (ULONG: " + btLeDevInfo.MacAddressUlong + ")");

                        if (btLeDevInfo.MacAddressUlong != 0)
                        {
                            btLeDevInfo.MacAddress = sDeviceAddress;

                            Windows.Foundation.IAsyncOperation<BluetoothLEDevice> tsk1;

                            tsk1 = BluetoothLEDevice.FromBluetoothAddressAsync(btLeDevInfo.MacAddressUlong);

                            while (tsk1.Status == Windows.Foundation.AsyncStatus.Started)
                            {
                                System.Threading.Thread.Sleep(100);
                            }

                            BluetoothLEDevice bluetoothLeDevice = tsk1.GetResults();

                            QueryBtLeDevice(bluetoothLeDevice, btLeDevInfo);

                            bluetoothLeDevice.Dispose();
                        }

                        UpdateDevice(btLeDevInfo);
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

                    BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                    btLeDevInfo.Reason = BtLeDevInfo_Reason.REMOVED;

                    LogMessage("Device ID: " + deviceInfoUpdate.Id);

                    Windows.Foundation.IAsyncOperation<BluetoothLEDevice> tsk1;

                    tsk1 = BluetoothLEDevice.FromIdAsync(deviceInfoUpdate.Id);

                    while (tsk1.Status == Windows.Foundation.AsyncStatus.Started)
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    var bluetoothLeDevice = tsk1.GetResults();

                    ulong ulDeviceAddress = bluetoothLeDevice.BluetoothAddress;

                    // SRC: https://stackoverflow.com/questions/26775850/get-mac-address-of-device/26776285#26776285
                    var tempMac = ulDeviceAddress.ToString("x12"); // ("X");
                    var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
                    var replace = "$1:$2:$3:$4:$5:$6";
                    var sDeviceAddress = Regex.Replace(tempMac, regex, replace);

                    LogMessage("Device Address: " + sDeviceAddress + " (ULONG: " + ulDeviceAddress + ")");

                    if (ulDeviceAddress != 0)
                    {
                        btLeDevInfo.MacAddress = sDeviceAddress;

                        QueryBtLeDevice(bluetoothLeDevice, btLeDevInfo);
                    }

                    bluetoothLeDevice.Dispose();

                    UpdateDevice(btLeDevInfo);
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

                    WatcherStatusChanged();

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

    public enum BtLeDevInfo_Reason
    {
        NA = 0,
        ADDED = 1,
        REMOVED = 2
    }

    public class BtLeDevInfo
    {
        public BtLeDevInfo_Reason Reason
        {
            get;
            set;
        }

        public ulong MacAddressUlong
        {
            get;
            set;
        }

        public string MacAddress
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public bool isConnectible
        {
            get;
            set;
        }

        public bool isConnected
        {
            get;
            set;
        }

        public bool isPaired
        {
            get;
            set;
        }

        public string StatusText
        {
            get
            {
                string sStatus;

                if (isPaired)
                {
                    sStatus = "Paired";

                    if (isConnected)
                    {
                        sStatus += ", Connected";
                    }
                }
                else if (isConnected)
                {
                    sStatus = "Connected";
                }
                else
                {
                    if (isConnectible)
                    {
                        sStatus = "Ready to connect";
                    }
                    else
                    {
                        sStatus = "Inaccessible Device";
                    }
                }

                return sStatus;
            }
        }

        public int ServiceCount
        {
            get;
            set;
        }

        public int BatteryLevel
        {
            get;
            set;
        }
    }
}
