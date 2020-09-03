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

// SRC: https://www.andreasjakl.com/read-battery-level-bluetooth-le-devices/

namespace RscBtLeDevBattLvlMon
{
    public partial class FormMain : Form
    {
        BluetoothLEAdvertisementWatcher BleWatcher = null;

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            // TODO...
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            lbLog.Items.Clear();

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

            lbLog.Items.Add("Low energy supported? -> " + localAdapter.IsLowEnergySupported);
            lbLog.Items.Add("Central role supported? -> " + localAdapter.IsCentralRoleSupported);
            lbLog.Items.Add("Pheripherial role supported? -> " + localAdapter.IsPeripheralRoleSupported);

            //
            ////
            //

            // SRC: https://stackoverflow.com/questions/43568096/frombluetoothaddressasync-never-returns-on-windows-10-creators-update-in-wpf-app

            BleWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            BleWatcher.Received += Watcher_Received;
            BleWatcher.Start();
        }

        private /*async*/ void Watcher_Received(BluetoothLEAdvertisementWatcher sender,
                                           BluetoothLEAdvertisementReceivedEventArgs args)
        {
            lbLog.Items.Add("Bluetooth LE Device Advertisement Local Name -> " + args.Advertisement.LocalName);
            Windows.Foundation.IAsyncOperation<BluetoothLEDevice> tsk1;

            tsk1 = BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

            while (tsk1.Status == Windows.Foundation.AsyncStatus.Started)
            {
                System.Threading.Thread.Sleep(100);
            }

            var bluetoothLeDevice = tsk1.GetResults();

            lbLog.Items.Add("                    Address -> " + args.BluetoothAddress);
            lbLog.Items.Add("          Connection Status -> " + bluetoothLeDevice.ConnectionStatus);
            lbLog.Items.Add("                       Name -> " + bluetoothLeDevice.Name);
            lbLog.Items.Add("                  Device ID -> " + bluetoothLeDevice.DeviceId);

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
                lbLog.Items.Add("  Service: " + curService.Uuid);
            }

            // TODO...

            bluetoothLeDevice.Dispose();
        }
    }
}
