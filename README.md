# 5071Control - E5071C 网络分析仪控制程序

<div align="center">

![Platform](https://img.shields.io/badge/Platform-.NET%2010-blue)
![Language](https://img.shields.io/badge/Language-C%23-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Status](https://img.shields.io/badge/Status-Active-brightgreen)

一个用 C# 和 WPF 编写的 **Keysight E5071C 网络分析仪**控制应用程序

[快速开始](#快速开始) • [功能特性](#功能特性) • [系统要求](#系统要求) • [技术栈](#技术栈) • [常见问题](#常见问题)

</div>

---

## 📋 项目概述

**5071Control** 是一个基于 .NET 10 的桌面应用程序，用于通过 LAN 连接控制 **Keysight E5071C 网络分析仪**，实现远程自动化测量和数据采集。

程序遵循 **MVVM 架构模式**，使用 **VISA COM 互操作**与仪器通信，通过 **SCPI 标准命令**进行控制。

### 主要用途
- 🔌 远程连接 E5071C 网络分析仪
- 📊 自动化获取 S 参数测量数据
- 📈 实时显示和分析测量结果
- 💾 记录历史测量数据

---

## ✨ 功能特性

### 核心功能
- ✅ **LAN 连接**：通过 TCP/IP 协议连接仪器
- ✅ **自动测量**：一键触发仪器扫频测量
- ✅ **数据采集**：获取完整的 F 参数数据
- ✅ **智能分析**：自动计算最大值、最小值、平均值
- ✅ **实时显示**：在 DataGrid 中实时展示测量结果
- ✅ **错误检查**：自动查询仪器错误队列
- ✅ **详细日志**：完整的调试日志输出

### 技术特性
- 🏗️ **MVVM 架构**：清晰的分层设计（Model、View、ViewModel）
- 🔄 **异步操作**：使用 `async/await` 防止 UI 卡顿
- 📡 **标准 VISA**：完全符合 IEEE 488.2 和 SCPI 标准
- 🛡️ **完善的异常处理**：详细的错误提示和诊断信息

---

## 🚀 快速开始

### 系统要求

| 要求 | 版本 |
|------|------|
| **.NET** | 10.0 或更高 |
| **操作系统** | Windows 10/11 |
| **VISA** | Keysight VISA 或 NI VISA |
| **E5071C 仪器** | 固件最新版本 |

### 环境配置

#### 1. 安装 VISA
仪器通信需要 VISA 库：
```bash

# NI VISA
# 下载地址：https://www.ni.com/zh-cn/support/downloads/drivers/download.ni-visa.html?srsltid=AfmBOooaun8Zu1CWrhvfuUTrmUXD---zE-yAJ2-x-IXbzvTyJns-cDfk#590234
```

#### 2. 安装 VISA COM 互操作程序集引用
程序需要 VISA COM 互操作库：
```
C:\Program Files\IVI Foundation\VISA\VisaCom64\Primary Interop Assemblies\Ivi.Visa.Interop.dll
```

#### 3. 克隆或下载项目
```bash
git clone https://github.com/OnlyACry/5071Control
cd 5071Control
```

---

## 📖 使用说明

### 基本使用流程

#### 步骤 1：输入仪器 IP 地址
- 在文本框中输入 E5071C 的 IP 地址（默认：`192.168.100.124`）
- 确保与仪器处于同一网络

#### 步骤 2：点击"获取最大值"按钮


#### 步骤 3：查看测量结果
结果会自动显示在表格中：

| 时间 | 最大值 |
|------|--------|
| 2026-07-2 10:30:45.123 | -12.3456 |
| 2026-07-2 10:31:12.456 | -12.2341 |

---

## 🏛️ 项目结构

```
5071Control/
├── MainWindow.xaml              # UI 界面定义（XAML）
├── MainWindow.xaml.cs           # UI 代码后置
├── ViewModels/
│   └── MainViewModel.cs         # 视图模型（核心业务逻辑）
├── Models/
│   └── MeasurementData.cs       # 数据模型
├── App.xaml                     # 应用程序配置
├── App.xaml.cs                  # 应用程序代码
├── 5071Control.csproj           # 项目文件
└── README.md                    # 本文件
```

### 关键类说明

#### **MainViewModel.cs** (视图模型)
```csharp
// 核心职责：
- 处理与 E5071C 的通信
- 解析仪器响应数据
- 管理测量结果集合
- 控制 UI 状态和提示消息

// 关键方法：
GetMaxValueCommand           // 获取最大值命令
GetMaxValueFrom5071CAsync()  // 异步连接并获取数据
ParseFDataAndFindMax()       // 解析并分析 F 参数数据
CheckE5071CErrors()          // 检查仪器错误
```

#### **MeasurementData.cs** (数据模型)
```csharp
// 表示单次测量结果
Time      // 测量时间戳
MaxValue  // F 参数最大值
```

#### **MainWindow.xaml** (用户界面)
```xml
<!-- IP 地址输入区域 -->
<TextBox Text="{Binding IpAddress}" />

<!-- 获取按钮 -->
<Button Command="{Binding GetMaxValueCommand}" />

<!-- 结果显示表格 -->
<DataGrid ItemsSource="{Binding Measurements}" />
```

---

### SCPI 命令执行顺序

| 步骤 | 命令 | 作用 |
|------|------|------|
| 1 | `*IDN?` | 查询仪器型号和序列号 |
| 2 | `*CLS` | 清除错误队列 |
| 3 | `:TRIG:SOUR BUS` | 设置触发源为外部触发 |
| 4 | `:INIT:IMM` | 触发一次扫描 |
| 5 | `*WAI` | 等待测量完成 |
| 6 | `:SENS:SWE:POIN?` | 查询扫频点数 |
| 7 | `:CALC1:PAR1:SEL` | 选择Trace1 |
| 8 | `:CALC:DATA:FDATA?` | 查询 F 参数数据 |
| 9 | `SYST:ERR?` | 检查错误信息 |

---

## 🔍 调试和诊断

### 查看调试日志

程序运行时会输出详细的调试日志。在 Visual Studio 中查看：

```
菜单：视图 → 输出 → 调试
```

### 典型的调试日志

```
[DEBUG] GetMaxValueAsync 开始执行，IP: 192.168.1.100
[DEBUG] 后台线程开始执行
[DEBUG] VISA 资源地址: TCPIP0::192.168.1.100::inst0::INSTR
[DEBUG] 连接仪器...
[DEBUG] 连接成功，超时设置为 5000ms
[DEBUG] 查询仪器 ID
[DEBUG] 仪器 ID: Agilent Technologies,E5071C,...
[DEBUG] 清除错误队列
[DEBUG] 设置触发源为 BUS
[DEBUG] 触发一次扫描
[DEBUG] 等待测量完成...
[DEBUG] 查询扫描点数
[DEBUG] 扫描点数: 101
[DEBUG] 查询 S 参数数据 (SDATA)...
[DEBUG] 收到 2020 字符的数据
[DEBUG] 开始解析 SDATA 数据，期望 101 个点
[DEBUG] 成功分割出 202 个数据项
[DEBUG] 成功解析 202 个数值
[DEBUG] 迹线最大值: -12.3456
[DEBUG] 迹线最小值: -45.6789
[DEBUG] 迹线平均值: -28.5123
[DEBUG] 检查错误队列...
[DEBUG] 无错误
[DEBUG] 关闭连接...
[DEBUG] 连接已关闭
✓ 完成! 最大值: -12.3456 dB
```

### 常见问题排查

#### 连接失败

```
错误信息：E5071C 通信失败: (603, The VI/session does not have any handlers)

解决方案：
✓ 检查 IP 地址是否正确
✓ 确保仪器已开启并连接网络
✓ 尝试 ping 仪器 IP：ping 192.168.1.100
✓ 检查防火墙设置（允许端口 5025或关闭防火墙）
✓ 确保 VISA 已正确安装
```

#### 数据解析失败

```
错误信息：数据解析失败: 无法从数据中解析出有效的数值

解决方案：
✓ 检查仪器是否已完成测量
✓ 检查仪器是否处于错误状态
✓ 尝试手动按仪器前面板的 "Single Sweep" 按钮
✓ 查看调试日志中的具体错误信息
```

---

## 🏗️ 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| **Framework** | .NET | 10.0 |
| **UI Framework** | WPF | - |
| **Architecture** | MVVM | - |
| **MVVM Toolkit** | CommunityToolkit.Mvvm | 8.4.2 |
| **Instrumentation** | VISA COM | IVI Foundation |
| **Language** | C# | 12.0 |

### 核心依赖

```xml
<ItemGroup>
	<!-- MVVM 工具包 -->
	<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />

	<!-- VISA COM 互操作 -->
	<Reference Include="Ivi.Visa.Interop">
		<HintPath>C:\Program Files\IVI Foundation\VISA\VisaCom64\...\Ivi.Visa.Interop.dll</HintPath>
	</Reference>
</ItemGroup>
```

---


**⭐ 如果对你有帮助，请给个 Star ⭐**

---

Made with ❤️ for E5071C Users

Last Updated: 2026-07-3

</div>
