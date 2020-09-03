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

        protected const string csAPP_TITLE = "Bluetooth LE Device Battery Level Monitor";
        protected const string csAPP_NAME = "RscBtLeDevBattLvlMon";

        protected const int ciWIDTH_NORMAL = 795;
        protected const int ciHEIGHT_NORMAL = 489;

        // SRC: https://stackoverflow.com/questions/12026664/a-generic-error-occurred-in-gdi-when-calling-bitmap-gethicon
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        // SRC: https://stackoverflow.com/questions/4645171/environment-tickcount-is-not-enough/4645208
        [DllImport("kernel32.dll")]
        public static extern UInt64 GetTickCount64();

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

        public static bool s_bCloseApp = false;

        public List<BtLeDevInfo> m_aDevices = new List<BtLeDevInfo>();

        public int m_iAlertLevel = 30;

        public static UInt64 s_tcAppStart;

        public bool bAppWasShown = false;

        public static bool s_Log = false;

        public int m_iBottomGap = 0;

        public static bool s_bTestTaskDelay = false;

        // SRC: https://stackoverflow.com/questions/5168249/c-showing-an-invisible-form
        // INFO: This ensures that the window doesn't become visible the first time you call Show().
        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated)
            {
                this.CreateHandle();
                value = false;   // Prevent window from becoming visible
            }
            base.SetVisibleCore(value);
        }

        public FormMain()
        {
            InitializeComponent();

            StorageRegistry.m_sAppName = csAPP_NAME;
            this.Text = csAPP_TITLE;

            s_tcAppStart = GetTickCount64();

            s_MainForm = this;

            try
            {
                InitializeForm();
            }
            catch (Exception ex)
            {
                NewMsg(true /*bError*/, ex.Message);
            }
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            // NOT CALLED ON STARTUP
            // because of the implemented SetVisibleCore

            this.Left = /*Math.Max(0,*/ StorageRegistry.Read("FormMain\\Left", this.Left); //);
            this.Top = Math.Max(0, StorageRegistry.Read("FormMain\\Top", this.Top));
            this.Width = Math.Max(ciWIDTH_NORMAL, StorageRegistry.Read("FormMain\\Width", this.Width));
            this.Height = Math.Max(ciHEIGHT_NORMAL, StorageRegistry.Read("FormMain\\Height", this.Height));

            // ATTN!
            bAppWasShown = true;

            //InitializeForm();
        }

        public void InitializeForm()
        {

            lvDevices.Columns.Add("Battery Level");
            lvDevices.Columns.Add("Name");
            lvDevices.Columns.Add("Status");
            lvDevices.Columns.Add("Service Count");
            lvDevices.Columns.Add("MAC Address");
            lvDevices.Columns.Add("Update Count");
            lvDevices.Columns.Add("Device ID");
            lvDevices.Columns.Add("Last Error");

            int iCol = -1;
            foreach (ColumnHeader ch in lvDevices.Columns)
            {
                iCol++;
                int iColWidth = StorageRegistry.Read("FormMain\\GridDevices\\Column" + iCol.ToString(), -1);
                if (iColWidth > 0)
                {
                    ch.Width = iColWidth;
                }
            }

            m_iAlertLevel = StorageRegistry.Read("Settings\\Alert Level", m_iAlertLevel);
            if (m_iAlertLevel < 0)
                tbAlertLevel.Text = "";
            else
                tbAlertLevel.Text = m_iAlertLevel.ToString();

            int iInterval = StorageRegistry.Read("Settings\\Update Interval", tmrUpdate.Interval);
            if (iInterval <= 0) iInterval = 1000; // 1 sec
            if (iInterval != tmrUpdate.Interval) tmrUpdate.Interval = iInterval;
            if (iInterval < (60 * 1000))
                tbUpdateInterval.Text = "";
            else
                tbUpdateInterval.Text = (iInterval / (60 * 1000)).ToString();

            bool bAutoHide = StorageRegistry.Read("Settings\\Auto Hide", false);
            chbAutoHide.Checked = bAutoHide;

            chbAutoStart.Checked = IsAppStartWithWindowsOn();

            bool bHasUpdateableDevice = false;

            int iDevRegCnt = StorageRegistry.Read("Devices\\DeviceCount", 0);
            for (int iDevReg = 0; iDevReg < iDevRegCnt; iDevReg++)
            {
                string sMacAddress = StorageRegistry.Read("Devices\\Device" + iDevReg.ToString() + "\\MAC Address", "");

                if (sMacAddress.Length > 0)
                {
                    BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                    btLeDevInfo.Reason = BtLeDevInfo_Reason.PRELOADED;

                    btLeDevInfo.MacAddress = sMacAddress;
                    btLeDevInfo.MacAddressUlong = Convert.ToUInt64(sMacAddress.Replace(":", String.Empty), 16);

                    btLeDevInfo.Name = StorageRegistry.Read("Devices\\Device" + iDevReg.ToString() + "\\Name", "");
                    btLeDevInfo.DeviceID = StorageRegistry.Read("Devices\\Device" + iDevReg.ToString() + "\\Device ID", "");
                    //StorageRegistry.Read("Devices\\Device" + iDevReg.ToString() + "\\Service Count", 0);
                    btLeDevInfo.BatteryLevel = -1; //StorageRegistry.Read("Devices\\Device" + iDevReg.ToString() + "\\Battery Level", 0);
                    btLeDevInfo.ShowNotifyIcon = StorageRegistry.Read("Devices\\Device" + iDevReg.ToString() + "\\Show NotifyIcon", false);

                    UpdateDevice(btLeDevInfo);

                    if (btLeDevInfo.ShowNotifyIcon)
                    {
                        bHasUpdateableDevice = true;

                        RefreshNotifyIcon(btLeDevInfo);

                        BeginQueryBtLeDevice(btLeDevInfo);
                    }

                }
            }

            tmrUpdate.Enabled = bHasUpdateableDevice;

            // ATTN!
            if ((!bHasUpdateableDevice) || (!bAutoHide))
            {
                this.Visible = true;
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // ATTN!
            s_bCloseApp = true;
            tmrUpdate.Enabled = false;

            if (deviceWatcher != null)
            {
                if (deviceWatcher.Status == DeviceWatcherStatus.Started ||
                 deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    NewMsg(false /*bError*/, "Stopping Device discovery before closing application.");

                    s_bCloseApp = true;
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

                    if (btLeDevInfoItem.tskUpdate != null)
                    {
                        if ((btLeDevInfoItem.tskUpdate.Status != TaskStatus.Canceled) &&
                            (btLeDevInfoItem.tskUpdate.Status != TaskStatus.Faulted) &&
                            (btLeDevInfoItem.tskUpdate.Status != TaskStatus.RanToCompletion))
                        {
                            if (s_Log) LogMessage("DEBUG -> (CLOSING APP) WAITING FOR QueryBtLeDevice_Known_Async...");

                            NewMsg(false /*bError*/, "Waiting for Device update to complete before closing application.");

                            s_bCloseApp = true;

                            e.Cancel = true;
                        }
                    }
                }

            }

            if (!e.Cancel)
            {
                foreach (BtLeDevInfo btLeDevInfoItem in s_MainForm.m_aDevices)
                {
                    HideNotifyIcon(btLeDevInfoItem);
                }
            }

            // ATTN!!!
            if (e.Cancel)
            {
                return;
            }

            if (bAppWasShown)
            {
                // TODO: Closing Minimized Form is not handled!!!
                if (this.Left >= 0) StorageRegistry.Write("FormMain\\Left", this.Left);
                if (this.Top >= 0) StorageRegistry.Write("FormMain\\Top", this.Top);
                if (this.Width >= ciWIDTH_NORMAL) StorageRegistry.Write("FormMain\\Width", this.Width);
                if (this.Height >= ciHEIGHT_NORMAL) StorageRegistry.Write("FormMain\\Height", this.Height);

                int iCol = -1;
                foreach (ColumnHeader ch in lvDevices.Columns)
                {
                    iCol++;
                    StorageRegistry.Write("FormMain\\GridDevices\\Column" + iCol.ToString(), ch.Width);
                }
            }
        }

        public void HideNotifyIcon(BtLeDevInfo btLeDevInfo)
        {
            if (btLeDevInfo.notifyIcon != null)
            {
                btLeDevInfo.notifyIcon.Visible = false;

                IntPtr hIcon = IntPtr.Zero;
                if (btLeDevInfo.notifyIcon.Icon != null)
                {
                    hIcon = btLeDevInfo.notifyIcon.Icon.Handle;

                    btLeDevInfo.notifyIcon.Icon = null;

                    // SRC: https://stackoverflow.com/questions/12026664/a-generic-error-occurred-in-gdi-when-calling-bitmap-gethicon
                    if (hIcon != IntPtr.Zero)
                    {
                        DestroyIcon(hIcon);
                    }
                }

                btLeDevInfo.notifyIcon = null;
            }
        }

        public void NewMsg(bool bError, string sMsg)
        {
            if (bError)
            {
                // ATTN!
                this.Visible = true;

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
            if (s_Log) LogMessage("DEBUG -> TIMER...");

            tmrUpdate.Enabled = false;

            foreach (BtLeDevInfo btLeDevInfoItem in s_MainForm.m_aDevices)
            {
                BeginQueryBtLeDevice(btLeDevInfoItem);
            }

            if (!s_bCloseApp)
            {
                tmrUpdate.Enabled = true;
            }
        }

        public void BeginQueryBtLeDevice(BtLeDevInfo btLeDevInfo)
        {
            if (btLeDevInfo.BatteryLevel != 0 && btLeDevInfo.ShowNotifyIcon)
            {
                bool bGo = true;

                if (btLeDevInfo.tskUpdate != null)
                {

                    //if (s_Log) LogMessage("DEBUG -> tskUpdate.IsCompleted = " + btLeDevInfo.tskUpdate.IsCompleted);
                    //if (s_Log) LogMessage("DEBUG -> tskUpdate.Status = " + btLeDevInfo.tskUpdate.Status);

                    if ((btLeDevInfo.tskUpdate.Status != TaskStatus.Canceled) &&
                        (btLeDevInfo.tskUpdate.Status != TaskStatus.Faulted) &&
                        (btLeDevInfo.tskUpdate.Status != TaskStatus.RanToCompletion))
                    {
                        bGo = false;

                        if (s_Log) LogMessage("DEBUG -> WAITING FOR QueryBtLeDevice_Known_Async...");
                    }
                }

                if (bGo)
                {
                    if (s_Log) LogMessage("DEBUG -> STARTING QueryBtLeDevice_Known_Async...");

                    // BUG: Reported as Completed while NOT...
                    //btLeDevInfo.tskUpdate = QueryBtLeDevice_Known_Async_Task(btLeDevInfo);

                    // BUG: Reported as Completed while NOT...
                    //btLeDevInfo.tskUpdate = Task.Run(() => QueryBtLeDevice_Known_Async(btLeDevInfo));

                    // BUG: UI freez experienced...
                    //btLeDevInfo.tskUpdate = QueryBtLeDevice_Known_Async(btLeDevInfo);

                    // FIX: No UI freez!!!
                    btLeDevInfo.tskUpdate = QueryBtLeDevice_Known_Async_Task2(btLeDevInfo);

                    if (s_Log) LogMessage("DEBUG -> STARTED QueryBtLeDevice_Known_Async...");
                }
            }
        }

        // BUG: Reported as Completed while NOT...
        /*
        // SRC: https://stackoverflow.com/questions/17119075/do-you-have-to-put-task-run-in-a-method-to-make-it-async
        public async Task QueryBtLeDevice_Known_Async_Task(BtLeDevInfo btLeDevInfoWhat)
        {
            await Task.Run(() => QueryBtLeDevice_Known_Async(btLeDevInfoWhat));
        }
        */
        public async Task QueryBtLeDevice_Known_Async_Task2(BtLeDevInfo btLeDevInfoWhat)
        {
            if (s_Log) LogMessage("DEBUG -> Root Task - BEGIN");

            Task tsk = Task.Run(() => QueryBtLeDevice_Known_Async(btLeDevInfoWhat));

            if (s_Log) LogMessage("DEBUG -> Root Task - AWAIT");

            await tsk;

            if (s_Log) LogMessage("DEBUG -> Root Task - END");
        }

        public async Task QueryBtLeDevice_Known_Async(BtLeDevInfo btLeDevInfoWhat)
        {
            try
            {
                await BluetoothLEDevicesLock.WaitAsync();

                if (s_Log) LogMessage("---------------------- QueryBtLeDevice_Known");

                BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                btLeDevInfo.Reason = BtLeDevInfo_Reason.QUERY;

                if (s_Log) LogMessage("Device ID: " + btLeDevInfoWhat.DeviceID);

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

                if (s_Log) LogMessage("Device Address: " + sDeviceAddress + " (ULONG: " + ulDeviceAddress + ")");

                if (ulDeviceAddress != 0)
                {
                    btLeDevInfo.MacAddress = sDeviceAddress;

                    QueryBtLeDevice(bluetoothLeDevice, btLeDevInfo);
                }

                bluetoothLeDevice.Dispose();

                // DEBUG...
                if (s_bTestTaskDelay)
                {
                    if (s_Log) LogMessage("DEBUG -> await Task.Delay(8000);");
                    await Task.Delay(8000);
                }

                UpdateDevice(btLeDevInfo);
            }
            catch (Exception ex)
            {
                if (s_Log) LogMessage("QueryBtLeDevice_Known - ERROR: " + ex.Message);
            }
            finally
            {
                BluetoothLEDevicesLock.Release();
            }
        }

        private void btnTogleIcon_Click(object sender, EventArgs e)
        {
            btnInfoBar.Visible = false;

            if (lvDevices.Items.Count == 0)
            {
                NewMsg(true /*bError*/, "There are no discovered / preloaded Bluetooth LE Devices listed!");
                return;
            }

            if (lvDevices.SelectedIndices.Count == 0)
            {
                NewMsg(true /*bError*/, "There are no selected Bluetooth LE Device!");
                return;
            }

            BtLeDevInfo btLeDevInfo = m_aDevices[lvDevices.SelectedIndices[0]];

            if (btLeDevInfo.BatteryLevel == 0)
            {
                NewMsg(true /*bError*/, "There are no Battery Service for selected Bluetooth LE Device!");
                return;
            }

            if (btLeDevInfo.ShowNotifyIcon)
            {
                btLeDevInfo.ShowNotifyIcon = false;
            }
            else
            {
                btLeDevInfo.ShowNotifyIcon = true;
            }

            int iDevRegHit = -1;

            int iDevRegCount = StorageRegistry.Read("Devices\\DeviceCount", 0);

            // ATTN!
            int iDevRegNextAvail = iDevRegCount;

            for (int iDevReg = 0; iDevReg < iDevRegCount; iDevReg++)
            {
                string sMacAddress = StorageRegistry.Read("Devices\\Device" + iDevReg.ToString() + "\\MAC Address", "");

                if (sMacAddress.Length == 0)
                {
                    iDevRegNextAvail = iDevReg;
                }
                else
                {
                    if (sMacAddress == btLeDevInfo.MacAddress)
                    {
                        iDevRegHit = iDevReg;
                        break;
                    }
                }
            }

            if (iDevRegHit < 0)
            {
                iDevRegHit = iDevRegNextAvail;

                if (iDevRegHit == iDevRegCount)
                {
                    iDevRegCount++;
                    StorageRegistry.Write("Devices\\DeviceCount", iDevRegCount);
                }
            }

            if (btLeDevInfo.ShowNotifyIcon)
            {
                StorageRegistry.Write("Devices\\Device" + iDevRegHit.ToString() + "\\MAC Address", btLeDevInfo.MacAddress);
                StorageRegistry.Write("Devices\\Device" + iDevRegHit.ToString() + "\\Name", btLeDevInfo.Name);
                StorageRegistry.Write("Devices\\Device" + iDevRegHit.ToString() + "\\Device ID", btLeDevInfo.DeviceID);
                StorageRegistry.Write("Devices\\Device" + iDevRegHit.ToString() + "\\Service Count", btLeDevInfo.ServiceCount);
                StorageRegistry.Write("Devices\\Device" + iDevRegHit.ToString() + "\\Battery Level", btLeDevInfo.BatteryLevel);
                StorageRegistry.Write("Devices\\Device" + iDevRegHit.ToString() + "\\Show NotifyIcon", btLeDevInfo.ShowNotifyIcon);
            }
            else
            {
                StorageRegistry.DeleteValue("Devices\\Device" + iDevRegHit.ToString() + "\\MAC Address");
                StorageRegistry.DeleteValue("Devices\\Device" + iDevRegHit.ToString() + "\\Name");
                StorageRegistry.DeleteValue("Devices\\Device" + iDevRegHit.ToString() + "\\Device ID");
                StorageRegistry.DeleteValue("Devices\\Device" + iDevRegHit.ToString() + "\\Service Count");
                StorageRegistry.DeleteValue("Devices\\Device" + iDevRegHit.ToString() + "\\Battery Level");
                StorageRegistry.DeleteValue("Devices\\Device" + iDevRegHit.ToString() + "\\Show NotifyIcon");

                if (iDevRegHit == iDevRegCount - 1)
                {
                    iDevRegCount--;
                    StorageRegistry.Write("Devices\\DeviceCount", iDevRegCount);

                    StorageRegistry.DeleteSubKey("Devices\\Device" + iDevRegHit.ToString());
                }
            }

            if (btLeDevInfo.ShowNotifyIcon)
            {
                RefreshNotifyIcon(btLeDevInfo);

                BeginQueryBtLeDevice(btLeDevInfo);

                if (!tmrUpdate.Enabled)
                {
                    tmrUpdate.Enabled = true;
                }
            }
            else
            {
                HideNotifyIcon(btLeDevInfo);
            }
        }

        private void RefreshNotifyIcon(BtLeDevInfo btLeDevInfo)
        {
            if (!btLeDevInfo.ShowNotifyIcon)
            {
                // ATTN!
                return;
            }

            bool bJustCreated = false;

            if (btLeDevInfo.notifyIcon == null)
            {
                bJustCreated = true;

                btLeDevInfo.notifyIcon = new NotifyIcon();

                btLeDevInfo.notifyIcon.Click += NotifyIcon_Click;

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
                if (m_iAlertLevel >= 0 && (btLeDevInfo.BatteryLevel >= 0 && btLeDevInfo.BatteryLevel <= m_iAlertLevel))
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

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                this.Visible = false;
            }
            else
            {
                this.Visible = true;
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
                    UInt64 tcRunning = GetTickCount64() - s_tcAppStart;

                    string sLogLine = (tcRunning / 1000).ToString() + "." + (tcRunning % 1000).ToString() + " - " + sMessage;

                    s_MainForm.lbLog.Items.Add(sLogLine);
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

                            if (s_bCloseApp)
                            {
                                if (s_Log) LogMessage("DEBUG -> (CLOSING APP) DeviceWatcherStatus.Stopped...");
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

                        if (btLeDevInfo.Reason == BtLeDevInfo_Reason.QUERY)
                        {
                            btLeDevInfoItem.UpdateCount += 1;
                        }

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

                        string sUpdateCount = "";
                        if (btLeDevInfo.UpdateCount > 0)
                        {
                            sUpdateCount = btLeDevInfo.UpdateCount.ToString();
                        }

                        s_MainForm.lvDevices.Items[iIdxItem].Text = sBatteryLevel;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[1].Text = btLeDevInfo.Name;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[2].Text = btLeDevInfo.StatusText;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[3].Text = sServiceCount;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[4].Text = btLeDevInfo.MacAddress;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[5].Text = sUpdateCount;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[6].Text = btLeDevInfo.DeviceID;
                        s_MainForm.lvDevices.Items[iIdxItem].SubItems[7].Text = btLeDevInfo.LastError;

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

                    string sUpdateCount = "";
                    if (btLeDevInfo.UpdateCount > 0)
                    {
                        sUpdateCount = btLeDevInfo.UpdateCount.ToString();
                    }

                    s_MainForm.lvDevices.Items.Add(sBatteryLevel);

                    iIdxItem = s_MainForm.lvDevices.Items.Count - 1;

                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.Name);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.StatusText);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(sServiceCount);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(btLeDevInfo.MacAddress);
                    s_MainForm.lvDevices.Items[iIdxItem].SubItems.Add(sUpdateCount);
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
                        // ATTN!
                        s_MainForm.Visible = true;

                        bColored = true;

                        s_MainForm.lvDevices.Items[iIdxItem].BackColor = Color.DarkRed;
                        s_MainForm.lvDevices.Items[iIdxItem].ForeColor = Color.White;
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

                            case BtLeDevInfo_Reason.PRELOADED:
                                s_MainForm.lvDevices.Items[iIdxItem].BackColor = SystemColors.Info;
                                s_MainForm.lvDevices.Items[iIdxItem].ForeColor = SystemColors.InfoText;
                                break;

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

                    if (btLeDevInfo.Reason == BtLeDevInfo_Reason.QUERY
                        && s_bCloseApp)
                    {
                        // FIX(PROVEN): To avoid identifying as still running...
                        btLeDevInfo.tskUpdate = null;

                        if (s_Log) LogMessage("DEBUG -> (CLOSING APP) UpdateDevice...");
                        s_MainForm.Close();
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
                NewMsg(false /*bError*/, "Stopping Device discovery...");

                deviceWatcher.Stop();
            }
        }

        private void btnEnum_Click(object sender, EventArgs e)
        {
            btnInfoBar.Visible = false;

            if (s_Log) LogMessage(""); // Clears LOG...

            try
            {

                Windows.Foundation.IAsyncOperation<BluetoothAdapter> tsk;

                tsk = BluetoothAdapter.GetDefaultAsync();

                while (tsk.Status == Windows.Foundation.AsyncStatus.Started)
                {
                    System.Threading.Thread.Sleep(100);
                }

                var localAdapter = tsk.GetResults();

                if (s_Log) LogMessage("Low energy supported? -> " + localAdapter.IsLowEnergySupported);
                if (s_Log) LogMessage("Central role supported? -> " + localAdapter.IsCentralRoleSupported);
                if (s_Log) LogMessage("Pheripherial role supported? -> " + localAdapter.IsPeripheralRoleSupported);

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
            if (s_Log) LogMessage("Bluetooth LE Device Advertisement Local Name -> " + args.Advertisement.LocalName);
  
            Windows.Foundation.IAsyncOperation<BluetoothLEDevice> tsk1;

            tsk1 = BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

            while (tsk1.Status == Windows.Foundation.AsyncStatus.Started)
            {
                System.Threading.Thread.Sleep(100);
            }

            var bluetoothLeDevice = tsk1.GetResults();

            if (s_Log) LogMessage("                    Address -> " + args.BluetoothAddress);
            if (s_Log) LogMessage("          Connection Status -> " + bluetoothLeDevice.ConnectionStatus);
            if (s_Log) LogMessage("                       Name -> " + bluetoothLeDevice.Name);
            if (s_Log) LogMessage("                  Device ID -> " + bluetoothLeDevice.DeviceId);

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
                if (s_Log) LogMessage("  Service: " + curService.Uuid);
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

            if (s_Log) LogMessage("Connection Status: " + bluetoothLeDevice.ConnectionStatus);
            if (s_Log) LogMessage("Name: " + bluetoothLeDevice.Name);
            if (s_Log) LogMessage("Device ID: " + bluetoothLeDevice.DeviceId);

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

            if (s_Log) LogMessage("Service Count: " + gattServices.Services.Count);

            btLeDevInfo.ServiceCount = gattServices.Services.Count;

            btLeDevInfo.BatteryLevel = 0;

            foreach (var curService in gattServices.Services)
            {
                if (s_Log) LogMessage(" - Service GUID: " + curService.Uuid);

                /*
                if (curService.Uuid.ToString().ToLower() == BatteryServiceGUID)
                */
                if (curService.Uuid.Equals(UuidBatteryService))
                {
                    if (s_Log) LogMessage("   Battery Service!");

                    try
                    {

                        Windows.Foundation.IAsyncOperation<Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicsResult> tsk3;

                        tsk3 = curService.GetCharacteristicsAsync();

                        while (tsk3.Status == Windows.Foundation.AsyncStatus.Started)
                        {
                            System.Threading.Thread.Sleep(100);
                        }

                        var gattCharacteristics = tsk3.GetResults();

                        if (s_Log) LogMessage("   Characteristic Count: " + gattCharacteristics.Characteristics.Count);

                        foreach (var curCharacteristic in gattCharacteristics.Characteristics)
                        {
                            if (s_Log) LogMessage("   Characteristic Handle: " + curCharacteristic.AttributeHandle);
                            if (s_Log) LogMessage("   Characteristic GUID: " + curCharacteristic.Uuid);

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

                                if (s_Log) LogMessage("   Characteristic Value (HEX): 0x" + BitConverter.ToString(input));

                                if (s_Log) LogMessage("   Characteristic Value (Dec):   " + input[0].ToString());

                                btLeDevInfo.BatteryLevel = (int)input[0];
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        btLeDevInfo.BatteryLevel    = -1;
                        btLeDevInfo.LastError       = ex.Message;

                        if (s_Log) LogMessage("QueryBtLeDevice - ERROR: " + ex.Message);
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

                    if (s_Log) LogMessage("---------------------- Device Watcher - ADDED");

                    BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                    btLeDevInfo.Reason = BtLeDevInfo_Reason.ADDED;

                    if (s_Log) LogMessage("Device Name: " + deviceInfo.Name);

                    btLeDevInfo.Name = deviceInfo.Name;

                    btLeDevInfo.isConnectible = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.Bluetooth.Le.IsConnectable") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"]));

                    btLeDevInfo.isConnected = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsConnected") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.IsConnected"]));

                    btLeDevInfo.isPaired = ((deviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsPaired") &&
                            (bool)deviceInfo.Properties["System.Devices.Aep.IsPaired"]));

                    if (s_Log) LogMessage("Is Connectible: " + btLeDevInfo.isConnectible);
                    if (s_Log) LogMessage("Is Connected: " + btLeDevInfo.isConnected);
                    if (s_Log) LogMessage("Is Paired: " + btLeDevInfo.isPaired);

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
                        if (s_Log) LogMessage("Device Address: " + sDeviceAddress + " (ULONG: " + btLeDevInfo.MacAddressUlong + ")");

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
                if (s_Log) LogMessage("DeviceWatcher_Added - ERROR: " + ex.Message);
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
                    if (s_Log) LogMessage("---------------------- Device Watcher - UPDATED");
                    if (s_Log) LogMessage("  DeviceWatcher_Updated - Device Id: " + deviceInfoUpdate.Id);
                    */

                }
            }
            catch (Exception ex)
            {
                if (s_Log) LogMessage("DeviceWatcher_Updated - ERROR: " + ex.Message);
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

                    if (s_Log) LogMessage("---------------------- Device Watcher - REMOVED");

                    BtLeDevInfo btLeDevInfo = new BtLeDevInfo();

                    btLeDevInfo.Reason = BtLeDevInfo_Reason.REMOVED;

                    if (s_Log) LogMessage("Device ID: " + deviceInfoUpdate.Id);

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

                    if (s_Log) LogMessage("Device Address: " + sDeviceAddress + " (ULONG: " + ulDeviceAddress + ")");

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
                if (s_Log) LogMessage("DeviceWatcher_Removed - ERROR: " + ex.Message);
            }
            finally
            {
                BluetoothLEDevicesLock.Release();
            }
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            try
            {
                await BluetoothLEDevicesLock.WaitAsync();

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {

                    if (s_Log) LogMessage("---------------------- Device Watcher - ENUMERATION COMPLETED");

                    WatcherStatusChanged();

                }
            }
            catch (Exception ex)
            {
                if (s_Log) LogMessage("DeviceWatcher_EnumerationCompleted - ERROR: " + ex.Message);
            }
            finally
            {
                BluetoothLEDevicesLock.Release();
            }
        }

        private async void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            try
            {
                await BluetoothLEDevicesLock.WaitAsync();

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {

                    if (s_Log) LogMessage("---------------------- Device Watcher - STOPPED");

                    WatcherStatusChanged();

                }
            }
            catch (Exception ex)
            {
                if (s_Log) LogMessage("DeviceWatcher_Removed - ERROR: " + ex.Message);
            }
            finally
            {
                BluetoothLEDevicesLock.Release();
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

            StorageRegistry.Write("Settings\\Alert Level", m_iAlertLevel);

            // Update Notify Icon...
            foreach (BtLeDevInfo btLeDevInfoItem in m_aDevices)
            {
                btLeDevInfoItem.NotifyIcon_UpdateRequired = true;
                RefreshNotifyIcon(btLeDevInfoItem);
            }
        }

        private void chbLog_CheckedChanged(object sender, EventArgs e)
        {
            if (chbLog.Checked)
            {
                s_Log = true;
                if (s_Log) LogMessage("LOG STARTED...");

                m_iBottomGap = ClientRectangle.Height - lvDevices.Bottom;

                lvDevices.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right); // | AnchorStyles.Bottom);

                int iCY = lvDevices.Height / 2;

                lvDevices.Height = iCY;

                lbLog.Height = iCY - m_iBottomGap;
                lbLog.Top = lvDevices.Top + iCY + m_iBottomGap;

                lbLog.Visible = true;
            }
            else
            {
                s_Log = false;
                lbLog.Items.Clear();

                lbLog.Visible = false;

                lvDevices.Height = (ClientRectangle.Height - lvDevices.Top) - m_iBottomGap;

                lvDevices.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 17 /*WM_QUERYENDSESSION*/)
            {
                if (!this.Visible)
                {
                    this.Visible = true;
                }
            }

            base.WndProc(ref m);
        }

        private void chbAutoHide_CheckedChanged(object sender, EventArgs e)
        {
            StorageRegistry.Write("Settings\\Auto Hide", chbAutoHide.Checked);
        }

        private void tbUpdateInterval_KeyPress(object sender, KeyPressEventArgs e)
        {
            // SRC: https://stackoverflow.com/questions/463299/how-do-i-make-a-textbox-that-only-accepts-numbers
            // Numbers only...
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void tbUpdateInterval_TextChanged(object sender, EventArgs e)
        {
            if (tbUpdateInterval.Text.Length == 0)
            {
                tmrUpdate.Interval = 1000; // 1 sec
            }
            else
            {
                int iVal = Int32.Parse(tbUpdateInterval.Text);

                if (iVal <= 0)
                    tmrUpdate.Interval = 1000; // 1 sec
                else
                    tmrUpdate.Interval = iVal * (60 * 1000);
            }

            StorageRegistry.Write("Settings\\Update Interval", tmrUpdate.Interval);
        }

        private void chbAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            // SRC: https://stackoverflow.com/questions/5089601/how-to-run-a-c-sharp-application-at-windows-startup
            Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey
                        ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (chbAutoStart.Checked)
            {
                registryKey.SetValue(csAPP_NAME, Application.ExecutablePath);
            }
            else
            {
                registryKey.DeleteValue(csAPP_NAME);
            }

            registryKey.Dispose();
        }

        public bool IsAppStartWithWindowsOn()
        {
            Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey
                        ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            string sValue = (string) registryKey.GetValue(csAPP_NAME, "");

            return (sValue == Application.ExecutablePath);
        }

        private void chbDebugDelay_CheckedChanged(object sender, EventArgs e)
        {
            s_bTestTaskDelay = chbDebugDelay.Checked;
        }
    }

    public enum BtLeDevInfo_Reason
    {
        NA = 0,
        PRELOADED = 1,
        ADDED = 2,
        REMOVED = 4,
        QUERY = 8
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
                if (Reason == BtLeDevInfo_Reason.PRELOADED)
                {
                    return "";
                }

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

        public bool ShowNotifyIcon
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

        public int UpdateCount
        {
            get;
            set;
        }
    }
}
