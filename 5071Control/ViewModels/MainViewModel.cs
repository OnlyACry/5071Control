using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using _5071Control.Models;
using Ivi.Visa.Interop;

namespace _5071Control.ViewModels
{
    /// <summary>
    /// E5071C 网络分析仪控制
    /// 参考官网 SCPI 命令手册
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _ipAddress = "192.168.100.124";

        [ObservableProperty]
        private ObservableCollection<MeasurementData> _measurements = new();

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [RelayCommand]
        private async Task GetMaxValueAsync()
        {
            try
            {
                // 步骤1：验证 IP 地址输入
                System.Diagnostics.Debug.WriteLine($"[DEBUG] GetMaxValueAsync 开始执行");

                if (!ValidateIpAddress(IpAddress))
                {
                    StatusMessage = "✗ IP 地址格式错误";
                    System.Diagnostics.Debug.WriteLine($"[ERROR] IP 地址格式无效: {IpAddress}");
                    MessageBox.Show("请输入有效的 IP 地址，格式: xxx.xxx.xxx.xxx", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] IP 地址验证通过: {IpAddress}");

                // 步骤2：更新状态消息 - 连接中
                StatusMessage = "正在连接仪器...";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 调用 GetMaxValueFrom5071CAsync，IP: {IpAddress}");

                // 步骤3：执行异步获取操作
                double maxValue = await GetMaxValueFrom5071CAsync(IpAddress);

                // 步骤4：添加测量数据
                Measurements.Add(new MeasurementData
                {
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    MaxValue = maxValue
                });

                // 步骤5：更新状态消息 - 完成
                StatusMessage = $"✓ 完成! 最大值: {maxValue:F4} dB | 总数: {Measurements.Count}";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 成功获取最大值: {maxValue}, 总数: {Measurements.Count}");
            }
            catch (Exception ex)
            {
                // 步骤6：更新状态消息 - 失败
                StatusMessage = $"✗ 失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[ERROR] 异常: {ex}");
                MessageBox.Show($"获取失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 验证 IP 地址格式
        /// </summary>
        /// <param name="ip">要验证的 IP 地址字符串</param>
        /// <returns>如果格式有效返回 true，否则返回 false</returns>
        private bool ValidateIpAddress(string ip)
        {
            try
            {
                // 检查 IP 是否为空或仅包含空白
                if (string.IsNullOrWhiteSpace(ip))
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] IP 地址为空");
                    return false;
                }

                // 移除首尾空格
                ip = ip.Trim();

                // 检查是否能解析为 IPAddress
                if (!System.Net.IPAddress.TryParse(ip, out var ipAddress))
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] 无法解析 IP 地址: {ip}");
                    return false;
                }

                // 确保是 IPv4 地址（E5071C 使用 IPv4）
                if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] 不是 IPv4 地址: {ip}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] IP 地址有效: {ip}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] IP 验证异常: {ex.Message}");
                return false;
            }
        }

        private async Task<double> GetMaxValueFrom5071CAsync(string ip)
        {
            return await Task.Run(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 后台线程开始执行");

                // E5071C VISA 资源字符串
                string resourceName = $"TCPIP0::{ip}::inst0::INSTR";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] VISA 资源地址: {resourceName}");

                var rm = new ResourceManager();
                var fio = new FormattedIO488();

                try
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 连接仪器...");
                    fio.IO = (IMessage)rm.Open(resourceName);

                    // 设置超时
                    fio.IO.Timeout = 5000;  // 5 秒超时
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 连接成功，超时设置为 5000ms");

                    // 步骤1：查询仪器 ID（验证连接）
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 查询仪器 ID");
                    fio.WriteString("*IDN?");
                    string instrumentId = fio.ReadString();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 仪器 ID: {instrumentId}");

                    // 步骤2：清除错误队列
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 清除错误队列");
                    fio.WriteString("*CLS");

                    // 步骤3：设置触发源为 BUS（外部触发）
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 设置触发源为 BUS");
                    fio.WriteString(":TRIG:SOUR BUS");

                    // 步骤4：触发一次扫描
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 触发一次扫描");
                    fio.WriteString(":INIT:IMM");

                    // 步骤5：等待测量完成
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 等待测量完成...");
                    fio.WriteString("*WAI");

                    // 步骤6：查询扫描点数（用于了解数据量）
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 查询扫描点数");
                    fio.WriteString(":SENS:SWE:POIN?");
                    string pointsStr = fio.ReadString();
                    int points = int.Parse(pointsStr.Trim());
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 扫描点数: {points}");

                    // *****步骤7：选择Trace1
                    fio.WriteString(":CALC1:PAR1:SEL");
                    ////:CALC1:DATA:SDAT?   测试SDATA
                    // 步骤8：读取格式化数据（LogMag）
                    System.Diagnostics.Debug.WriteLine("[DEBUG] 查询 FDATA...");
                    fio.WriteString(":CALC1:DATA:FDAT?");
                    string responseData = fio.ReadString().Trim();

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 收到 {responseData.Length} 字符的数据");

                    // 步骤9：解析最大值
                    double maxValue = ParseFDataAndFindMax(responseData);

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 最大值 = {maxValue}");

                    //等待上一条结束
                    fio.WriteString("*OPC?");
                    string opc = fio.ReadString();

                    //fio.WriteString(":CALC1:DATA:SDAT?");
                    //responseData = fio.ReadString().Trim();

                    // 步骤9：检查仪器错误
                    CheckE5071CErrors(fio);

                    return maxValue;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] 内部异常: {ex}");
                    throw new Exception($"E5071C 通信失败: {ex.Message}", ex);
                }
                finally
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 关闭连接...");
                    if (fio?.IO != null)
                    {
                        try
                        {
                            fio.IO.Close();
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] 连接已关闭");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] 关闭连接异常: {ex.Message}");
                        }
                    }
                }
            });
        }

        private double ParseFDataAndFindMax(string response)
        {
            string[] values = response.Split(',');

            double max = double.MinValue;

            for (int i = 0; i < values.Length; i += 2)
            {
                if (string.IsNullOrWhiteSpace(values[i]))
                    continue;

                if (double.TryParse(
                        values[i],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double value))
                {
                    max = Math.Max(max, value);
                }
            }

            return max;
        }

        /// <summary>
        /// 检查 E5071C 错误队列
        /// </summary>
        private void CheckE5071CErrors(FormattedIO488 fio)
        {
            try
            {
                while (true)
                {
                    fio.WriteString(":SYST:ERR?");
                    string err = fio.ReadString().Trim();

                    System.Diagnostics.Debug.WriteLine("[DEBUG] " + err);

                    if (err.StartsWith("+0") ||
                        err.StartsWith("0"))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}
