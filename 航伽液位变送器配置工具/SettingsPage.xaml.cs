using Medo.IO.Hashing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 航伽液位变送器配置工具
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        MainWindow? mainWindow = null;
        string LogString = "";

        public SettingsPage()
        {
            InitializeComponent();

            mainWindow = Application.Current.MainWindow as MainWindow;
            if(mainWindow == null ) {
                throw new Exception("Get MainWindow Error");
            }
        }

        private void AddLog(string msg)
        {
            LogString += msg + "\n";
            Trace.WriteLine(msg);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                while (true) {
                    await Task.Delay(200);
                    await Dispatcher.BeginInvoke(() =>
                    {
                        logdisplay.Text = LogString;
                    });
                }
                
            });
        }

        private async void SetAddressButton_Click(object sender, RoutedEventArgs e)
        {
      
            if(mainWindow == null) 
            {
                return;
            }
            if(mainWindow.CommPort == null)
            {
                return;
            }
            if(mainWindow.currentDeviceAddr == "请重新搜索" || mainWindow.currentDeviceAddr == "N/D")
            {
                MessageBox.Show("请先搜索设备获取当前地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if(tb_Address.Text == "")
            {
                MessageBox.Show("请输入目标地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (mainWindow.CommPort!.IsOpen == true)
            {
                Crc16 crc = Crc16.GetModbus();

                byte TargetAddr = 0x02;

                bool parseResult = byte.TryParse(tb_Address.Text, out TargetAddr);
                if(!parseResult || TargetAddr < 1 || TargetAddr > 247)
                {
                    MessageBox.Show("地址输入错误，请输入1-247之间的数字！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                List<byte> DeviceCmd = new List<byte>();

                DeviceCmd.Add(byte.Parse(mainWindow.currentDeviceAddr));

                DeviceCmd.Add(0x06);

                DeviceCmd.Add(0x00);
                DeviceCmd.Add(0x00);

                DeviceCmd.Add(0x00);
                DeviceCmd.Add(TargetAddr);

                crc.Reset();
                crc.Append(DeviceCmd.ToArray());
                byte[] crcValue = crc.GetCurrentHash();

                DeviceCmd.Add(crcValue[0]);
                DeviceCmd.Add(crcValue[1]);

                string FindDeviceCmdString = "";
                foreach (byte b in DeviceCmd)
                {
                    FindDeviceCmdString += b.ToString("X2");
                    FindDeviceCmdString += " ";
                }
                AddLog("----------------------------------------------------------------------------");
                AddLog("发送指令:" + FindDeviceCmdString);
                mainWindow.CommPort.Write(DeviceCmd.ToArray(), 0, DeviceCmd.Count);

                mainWindow.waitPortRecvSignal.Reset();
                mainWindow.RecvPortData.Clear();
                await Task.Run(() =>
                {
                    mainWindow.waitPortRecvSignal.Wait(5000);

                });

                string RecvDataStr = "";
                mainWindow.RecvPortData.ForEach(b =>
                {
                    RecvDataStr += b.ToString("X2");
                    RecvDataStr += " ";
                });
                AddLog("接收到:" + RecvDataStr);
                if (mainWindow.RecvPortData[0] == byte.Parse(mainWindow.currentDeviceAddr) && mainWindow.RecvPortData[1] == 0x06 && mainWindow.RecvPortData[5] == TargetAddr)
                {
                    MessageBox.Show("设置成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    mainWindow.currentDeviceAddr = TargetAddr.ToString();
                }
                else
                {
                    MessageBox.Show("设置失败，设备响应错误！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    mainWindow.currentDeviceAddr = "请重新搜索";
                }


            }

        }

        private async void SetBaudButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }
            if (mainWindow.CommPort == null)
            {
                return;
            }
            if (mainWindow.currentDeviceAddr == "请重新搜索" || mainWindow.currentDeviceAddr == "N/D")
            {
                MessageBox.Show("请先搜索设备获取当前地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (mainWindow.CommPort!.IsOpen == true)
            {
                Crc16 crc = Crc16.GetModbus();

                byte Targetbaud = 0x03;

                bool parseResult = byte.TryParse((BaudRateCombobox.SelectedValue as ComboBoxItem)!.Tag.ToString(), out Targetbaud);

                if (!parseResult)
                {
                    MessageBox.Show("波特率错误", "异常错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                List<byte> DeviceCmd = new List<byte>();

                DeviceCmd.Add(byte.Parse(mainWindow.currentDeviceAddr));

                DeviceCmd.Add(0x06);

                DeviceCmd.Add(0x00);
                DeviceCmd.Add(0x01);

                DeviceCmd.Add(0x00);
                DeviceCmd.Add(Targetbaud);

                crc.Reset();
                crc.Append(DeviceCmd.ToArray());
                byte[] crcValue = crc.GetCurrentHash();

                DeviceCmd.Add(crcValue[0]);
                DeviceCmd.Add(crcValue[1]);

                string FindDeviceCmdString = "";
                foreach (byte b in DeviceCmd)
                {
                    FindDeviceCmdString += b.ToString("X2");
                    FindDeviceCmdString += " ";
                }
                AddLog("----------------------------------------------------------------------------");
                AddLog("发送指令:" + FindDeviceCmdString);
                mainWindow.CommPort.Write(DeviceCmd.ToArray(), 0, DeviceCmd.Count);

                mainWindow.waitPortRecvSignal.Reset();
                mainWindow.RecvPortData.Clear();
                await Task.Run(() =>
                {
                    mainWindow.waitPortRecvSignal.Wait(5000);

                });
                string RecvDataStr = "";
                mainWindow.RecvPortData.ForEach(b =>
                {
                    RecvDataStr += b.ToString("X2");
                    RecvDataStr += " ";
                });
                AddLog("接收到:" + RecvDataStr);

            }


        }

    }
}
