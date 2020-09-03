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
using System.Runtime.InteropServices;
using System.Threading;

// SRC: https://www.andreasjakl.com/read-battery-level-bluetooth-le-devices/

namespace RscBtLeDevBattLvlMon
{
    public partial class FormMain : Form
    {
        // SRC: https://stackoverflow.com/questions/12026664/a-generic-error-occurred-in-gdi-when-calling-bitmap-gethicon
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        // SRC: https://stackoverflow.com/questions/43568096/frombluetoothaddressasync-never-returns-on-windows-10-creators-update-in-wpf-app

        /*
        BluetoothLEAdvertisementWatcher BleWatcher = null;
        */

        // SRC: https://github.com/microsoft/BluetoothLEExplorer

        private SemaphoreSlim BluetoothLEDevicesLock = new SemaphoreSlim(1, 1);

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

        public int m_iAlertLevel = 30;

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
            lvDevices.Columns.Add("Device ID");
            lvDevices.Columns.Add("Last Error");

            tbAlertLevel.Text = m_iAlertLevel.ToString();

            // TODO...

            tmrUpdate.Enabled = true;
        }
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (deviceWatcher != null)
            {
                if (deviceWatcher.Status == DeviceWatcherStatus.Started ||
                 deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    NewMsg(false /*bError*/, "Stopping Device discovery before closing application.");

                    s_bCloseOnStop = true;
                    deviceWatcher.Stop();

                    e.Cancel = true;
                }
                else if (deviceWatcher.Status == DeviceWatcherStatus.Stopping)
                {
                    NewMsg(false /*bError*/, "Stopping Device discovery before closing application.");

                    e.Cancel = true;
                }
            }

            if (!e.Cancel)
            {
                foreach (BtLeDevInfo btLeDevInfoItem in s_MainForm.m_aDevices)
                {
                    if (btLeDevInfoItem.notifyIcon != null)
                    {
                        IntPtr hIcon = IntPtr.Zero;
                        if (btLeDevInfoItem.notifyIcon.Icon != null)
                        {
                            hIcon = btLeDevInfoItem.notifyIcon.Icon.Handle;

                            btLeDevInfoItem.notifyIcon.Icon = null;

                            // SRC: https://stackoverflow.com/questions/12026664/a-generic-error-occurred-in-gdi-when-calling-bitmap-gethicon
                            if (hIcon != IntPtr.Zero)
                            {
                                DestroyIcon(hIcon);
                            }
                        }
                    }
                }
            }

            // TODO...
        }

        public void NewMsg(bool bError, string sMsg)
        {
            if (bError)
            {
                btnInfoBar.Text = "ERROR: " + sMsg + " (press to hide)";

                btnInfoBar.BackColor = Color.DarkRed;
                btnInfoBar.ForeColor = Color.White;
            }
            else
            {
                btnInfoBar.Text = "INFO: " + sMsg + " (press to hide)";

                btnInfoBar.BackColor = SystemColors.Info;
                btnInfoBar.ForeColor = SystemColors.InfoText;
            }
            btnInfoBar.Visible = true;
        }

        private void btnInfoBar_Click(object sender, EventArgs e)
        {
            btnInfoBar.Visible = false;
        }

        private void tmrUpdate_Tick(object sender, EventArgs e)
        {
            tmrUpdate.Enabled = false;

            foreach (BtLeDevInfo btLeDevInfoItem in s_MainForm.m_aDevices)
            {
                if (btLeDevInfoItem.BatteryLevel != 0)
                {
                    bool bGo = true;

                    if (btLeDevInfoItem.tskUpdate != null)
                    {
                        if ((btLeDevInfoItem.tskUpdate.Status != TaskStatus.Canceled) &&
                            (btLeDevInfoItem.tskUpdate.Status != TaskStatus.Faulted) &&
                            (btLeDevInfoItem.tskUpdate.Status != TaskStatus.RanToCompletion))
                        {
                            bGo = false;

                            NewMsg(true /*bError*/, "Device update is in progress!");
                        }
                    }

                    if (bGo)
                    {
                        btLeDevInfoItem.tskUpdate = QueryBtLeDevice_Known_Async(btLeDevInfoItem);
                    }
                }
            }

            tmrUpdate.Enabled = true;
        }

        // SRC: https://stackoverflow.com/questions/17119075/do-you-have-to-put-task-run-in-a-method-to-make-it-async
        public async Task QueryBtLeDevice_Known_Async(BtLeDevInfo btLeDevInfoWhat)
        {
            await Task.Run(() => QueryBtLeDevice_Known(btLeDevInfoWhat));
        }

        public async void QueryBtLeDevice_Known(BtLeDevInfo btLeDevInfoWhat)
        {
            try
            {
                await BluetoothLEDevicesLock.WaitAsync();

                LogMessage("---------------------- QueryBtLeDevice_Known");

                BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                btLeDevInfo.Reason = BtLeDevInfo_Reason.QUERY;

                LogMessage("Device ID: " + btLeDevInfoWhat.DeviceID);

                btLeDevInfo.DeviceID = btLeDevInfoWhat.DeviceID;

                Windows.Foundation.IAsyncOperation<BluetoothLEDevice> tsk1;

                tsk1 = BluetoothLEDevice.FromIdAsync(btLeDevInfoWhat.DeviceID);

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
            catch (Exception ex)
            {
                LogMessage("QueryBtLeDevice_Known - ERROR: " + ex.Message);
            }
            finally
            {
                BluetoothLEDevicesLock.Release();
            }
        }

        private void btnTogleIcon_Click(object sender, EventArgs e)
        {
            //TODO...
        }

        private void RefreshNotifyIcon(BtLeDevInfo btLeDevInfo)
        {
            bool bJustCreated = false;

            if (btLeDevInfo.notifyIcon == null)
            {
                bJustCreated = true;

                btLeDevInfo.notifyIcon = new NotifyIcon();

            }

            if (bJustCreated || btLeDevInfo.NotifyIcon_UpdateRequired)
            {
                btLeDevInfo.NotifyIcon_UpdateRequired = false;

                string sBattLevel;
                if (btLeDevInfo.BatteryLevel > 99)
                {
                    sBattLevel = "1d";
                }
                else if (btLeDevInfo.BatteryLevel < 0)
                {
                    sBattLevel = "?";
                }
                else
                {
                    sBattLevel = btLeDevInfo.BatteryLevel.ToString();
                }

                string sInfo = "";
                if (btLeDevInfo.Name.Length > 0)
                {
                    sInfo += btLeDevInfo.Name;
                }
                else
                {
                    sInfo += btLeDevInfo.MacAddress;
                }
                sInfo +=  " (" + sBattLevel + "%)";
                btLeDevInfo.notifyIcon.Text = sInfo;

                Color clrBk = Color.DodgerBlue;
                int iCY = 2;
                if (m_iAlertLevel >= 0 && btLeDevInfo.BatteryLevel < m_iAlertLevel)
                {
                    iCY = 1;
                    clrBk = Color.Red;
                }

                //m_NotifyIcon.Icon = SystemIcons.Exclamation;

                // SRC: https://stackoverflow.com/questions/25403169/get-application-icon-of-c-sharp-winforms-app
                //m_NotifyIcon.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);

                // SRC: https://stackoverflow.com/questions/34075264/i-want-to-display-numbers-on-the-system-tray-notification-icons-on-windows
                Brush brush = new SolidBrush(Color.White);
                Brush brushBk = new SolidBrush(clrBk);
                Pen penBk = new Pen(clrBk);
                // Create a bitmap and draw text on it
                Bitmap bitmap = new Bitmap(24, 24); // 32, 32); // 16, 16);
                Graphics graphics = Graphics.FromImage(bitmap);
                //graphics.DrawRectangle(new Pen(Color.Red), new Rectangle(0, 0, 23, 23));
                graphics.FillEllipse(brushBk, new Rectangle(3, 0, 23 - 4, 23 - 12));
                graphics.DrawEllipse(penBk, new Rectangle(3, 0, 23 - 4, 23 - 12));
                graphics.FillEllipse(brushBk, new Rectangle(3, 12, 23 - 4, 23 - 12));
                graphics.DrawEllipse(penBk, new Rectangle(3, 12, 23 - 4, 23 - 12));
                /*
                graphics.FillRectangle(brushBk, new Rectangle(3, 6, 23 - 5, 23 - 10));
                graphics.DrawRectangle(penBk, new Rectangle(3, 6, 23 - 5, 23 - 10));
                */
                graphics.FillRectangle(brushBk, new Rectangle(1, 6, 23 - 1, 23 - 10));
                graphics.DrawRectangle(penBk, new Rectangle(1, 6, 23 - 1, 23 - 10));
                Font font = new Font("Tahoma", 14);
                int iCX = 0;
                if (sBattLevel.Length < 2) iCX += 5;
                graphics.DrawString(sBattLevel, font, brush, iCX, iCY);
                // Convert the bitmap with text to an Icon

                IntPtr hIconOld = IntPtr.Zero;
                if (btLeDevInfo.notifyIcon.Icon != null)
                {
                    hIconOld = btLeDevInfo.notifyIcon.Icon.Handle;
                }

                btLeDevInfo.notifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());

                // SRC: https://stackoverflow.com/questions/12026664/a-generic-error-occurred-in-gdi-when-calling-bitmap-gethicon
                if (hIconOld != IntPtr.Zero)
                {
                    DestroyIcon(hIconOld);
                }

                if (!btLeDevInfo.notifyIcon.Visible)
                {
                    btLeDevInfo.notifyIcon.Visible = true;
                }

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
                            s_MainForm.NewMsg(false /*bError*/, "Device discovery completed.");

                            if (s_MainForm.chbAutoStopOnEnumComp.Checked)
                            {
                                s_MainForm.deviceWatcher.Stop();
                            }
                            break;
                        }

                    case DeviceWatcherStatus.Stopped:
                        {
                            s_MainForm.NewMsg(false /*bError*/, "Device discovery stopped.");

                            s_MainForm.btnEnum.Enabled = true;
                            s_MainForm.chbAutoStopOnEnumComp.Enabled = true;
                            s_MainForm.btnStop.Enabled = false;
                            s_MainForm.btnStop.Visible = false;

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
                        if (btLeDevInfo.Reason == BtLeDevInfo_Reason.ADDED)
                        {
                            btLeDevInfoItem.isConnectible   = btLeDevInfo.isConnectible;
                            btLeDevInfoItem.isPaired        = btLeDevInfo.isPaired;
                        }
                        btLeDevInfoItem.isConnected     = btLeDevInfo.isConnected;
                        if (btLeDevInfo.Name.Length > 0)
                        {
                            if (btLeDevInfoItem.Name != btLeDevInfo.Name)
                            {
                                btLeDevInfoItem.NotifyIcon_UpdateRequired = true;
                            }

                            btLeDevInfoItem.Name            = btLeDevInfo.Name; // Could change...
                        }
                        if (btLeDevInfoItem.ServiceCount == 0 && btLeDevInfo.ServiceCount > 0)
                        {
                            btLeDevInfoItem.ServiceCount    = btLeDevInfo.ServiceCount;
                        }
                        if (btLeDevInfo.BatteryLevel != 0)
                        {
                            if (btLeDevInfoItem.BatteryLevel != btLeDevInfo.BatteryLevel)
                            {
                                btLeDevInfoItem.NotifyIcon_UpdateRequired = true;
                            }

                            btLeDevInfoItem.BatteryLevel    = btLeDevInfo.BatteryLevel;
                        }
                        if (btLeDevInfo.DeviceID.Length > 0)
                        {
                            btLeDevInfoItem.DeviceID        = btLeDevInfo.DeviceID;
                        }
                        btLeDevInfoItem.LastError = btLeDevInfo.LastError; // Always overwrite!

                        // ATTN!!!
                        btLeDevInfo = btLeDevInfoItem;

                        string sBatteryLevel = "";
                        if (btLeDevInfo.BatteryLevel < 0)
                        {
                            sBatteryLevel = "?? %";
                        }
                        else if (btLeDevInfo.BatteryLevel > 0)
                        {
                            sBatteryLevel = btLeDevInfo.BatteryLevel.ToString() + " %";
                        }

                        string sServiceCount = "";
                        if (btLeDevInfo.ServiceCount > 0)
                        {
                            sServiceCount = btLeDevInfo.ServiceCount.ToString();
                        }

                        s_MainForm.lvDevices.Items[iIdxItem].Text = sBatteryLevel;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[1].Text = btLeDevInfo.Name;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[2].Text = btLeDevInfo.StatusText;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[3].Text = sServiceCount;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[4].Text = btLeDevInfo.MacAddress;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[5].Text = btLeDevInfo.DeviceID;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[6].Text = btLeDevInfo.LastError;

                        // TaskBar Notification Icon
                        if (btLeDevInfo.BatteryLevel != 0 && btLeDevInfo.NotifyIcon_UpdateRequired)
                        {
                            s_MainForm.RefreshNotifyIcon(btLeDevInfo);
                        }

                        break;
                    }
                }

                if (!bHit)
                {
                    s_MainForm.m_aDevices.Add(btLeDevInfo);

                    string sBatteryLevel = "";
                    if (btLeDevInfo.BatteryLevel < 0)
                    {
                        sBatteryLevel = "?? %";
                    }
                    else if (btLeDevInfo.BatteryLevel > 0)
                    {
                        sBatteryLevel = btLeDevInfo.BatteryLevel.ToString() + " %";
                    }

                    string sServiceCount = "";
                    if (btLeDevInfo.ServiceCount > 0)
                    {
                        sServiceCount = btLeDevInfo.ServiceCount.ToString();
                    }

                    s_MainForm.lvDevices.Items.Add(sBatteryLevel);

                    iIdxItem = s_MainForm.lvDevices.Items.Count - 1;

                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.Name);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.StatusText);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(sServiceCount);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.MacAddress);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.DeviceID);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.LastError);

                    // TaskBar Notification Icon
                    if (btLeDevInfo.BatteryLevel != 0)
                    {
                        s_MainForm.RefreshNotifyIcon(btLeDevInfo);
                    }
                }

                if (iIdxItem >= 0)
                {
                    bool bColored = false;

                    if (btLeDevInfo.LastError.Length > 0)
                    {
                        bColored = true;

                        s_MainForm.lvDevices.Items[iIdxItem].BackColor = Color.DarkRed;
                        s_MainForm.lvDevices.Items[iIdxItem].ForeColor = Color.White;
                    }

                    if ((!bColored) && (btLeDevInfo.Reason == BtLeDevInfo_Reason.QUERY))
                    {
                        bColored = true;

                        s_MainForm.lvDevices.Items[iIdxItem].BackColor = Color.YellowGreen;
                        s_MainForm.lvDevices.Items[iIdxItem].ForeColor = Color.Black;
                    }

                    if ((!bColored) && (btLeDevInfo.Reason != BtLeDevInfo_Reason.REMOVED)
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
            btnInfoBar.Visible = false;

            try
            {

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

                if (!localAdapter.IsCentralRoleSupported)
                {
                    NewMsg(true /*bError*/, "Bluetooth LE Device Discovery is not supported!");
                    return;
                }

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
                btnStop.Visible = true;

            }
            catch (Exception ex)
            {
                NewMsg(true /*bError*/, ex.Message);
            }

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

            btLeDevInfo.DeviceID = bluetoothLeDevice.DeviceId;

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

                    try
                    {

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
                    catch (Exception ex)
                    {
                        btLeDevInfo.BatteryLevel    = -1;
                        btLeDevInfo.LastError       = ex.Message;

                        LogMessage("QueryBtLeDevice - ERROR: " + ex.Message);
                    }
                }
            }

        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            try
            {
                await BluetoothLEDevicesLock.WaitAsync();

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {

                    LogMessage("---------------------- Device Watcher - ADDED");

                    BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                    btLeDevInfo.Reason = BtLeDevInfo_Reason.ADDED;

                    LogMessage("Device Name: " + deviceInfo.Name);

                    btLeDevInfo.Name = deviceInfo.Name;

                    btLeDevInfo.isConnectible = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.Bluetooth.Le.IsConnectable") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"]));

                    btLeDevInfo.isConnected = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsConnected") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.IsConnected"]));

                    btLeDevInfo.isPaired = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsPaired") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.IsPaired"]));

                    LogMessage("Is Connectible: " + btLeDevInfo.isConnectible);
                    LogMessage("Is Connected: " + btLeDevInfo.isConnected);
                    LogMessage("Is Paired: " + btLeDevInfo.isPaired);

                    // Let's make it connectable by default, we have error handles in case it doesn't work
                    bool shouldDisplay =
                        btLeDevInfo.isConnectible ||
                        btLeDevInfo.isConnected ||
                        btLeDevInfo.isPaired;

                    if (true) //shouldDisplay)
                    {
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
            finally
            {
                BluetoothLEDevicesLock.Release();
            }
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            try
            {
                await BluetoothLEDevicesLock.WaitAsync();

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
            finally
            {
                BluetoothLEDevicesLock.Release();
            }
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            try
            {
                await BluetoothLEDevicesLock.WaitAsync();

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {

                    LogMessage("---------------------- Device Watcher - REMOVED");

                    BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                    btLeDevInfo.Reason = BtLeDevInfo_Reason.REMOVED;

                    LogMessage("Device ID: " + deviceInfoUpdate.Id);

                    btLeDevInfo.DeviceID = deviceInfoUpdate.Id;

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
            finally
            {
                BluetoothLEDevicesLock.Release();
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

        private void tbAlertLevel_KeyPress(object sender, KeyPressEventArgs e)
        {
            // SRC: https://stackoverflow.com/questions/463299/how-do-i-make-a-textbox-that-only-accepts-numbers
            // Numbers only...
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void tbAlertLevel_TextChanged(object sender, EventArgs e)
        {
            if (tbAlertLevel.Text.Length == 0)
            {
                m_iAlertLevel = -1; // None...
            }
            else
            {
                m_iAlertLevel = Int32.Parse(tbAlertLevel.Text);
            }
        }

    }

    public enum BtLeDevInfo_Reason
    {
        NA = 0,
        ADDED = 1,
        REMOVED = 2,
        QUERY = 4
    }

    public class BtLeDevInfo
    {
        public BtLeDevInfo()
        {
            Reason = BtLeDevInfo_Reason.NA;
            MacAddressUlong = 0;
            MacAddress = "";
            Name = "";
            DeviceID = "";
            LastError = "";
        }

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

        public string DeviceID
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

        public string LastError
        {
            get;
            set;
        }

        public NotifyIcon notifyIcon
        {
            get;
            set;
        }

        public bool NotifyIcon_UpdateRequired
        {
            get;
            set;
        }

        public Task tskUpdate
        {
            get;
            set;
        }
    }
}
