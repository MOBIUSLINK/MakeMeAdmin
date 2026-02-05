# Make Me Admin AdminUI 独立安装程序

## 概述

`AdminUISetup` 项目用于创建 Make Me Admin AdminUI（管理端审批界面）的独立安装程序。这个安装程序可以单独分发给管理员，无需安装完整的 Make Me Admin 服务端。

## 功能特点

- ✅ **独立打包**：只包含 AdminUI 及其依赖项
- ✅ **自动依赖管理**：自动包含所有必需的 DLL
- ✅ **开始菜单快捷方式**：安装后自动创建
- ✅ **版本管理**：支持自动升级
- ✅ **WIX 打包**：使用与主安装程序相同的打包方式

## 包含的组件

### 主程序
- `MakeMeAdminAdminUI.exe` - 管理端审批界面

### 依赖库
- `ProcessCommunication.dll` - WCF 通信接口
- `Shared.dll` - 共享类和数据结构
- `UserList.dll` - 用户列表管理
- `Settings.dll` - 设置管理
- `Logging.dll` - 日志库
- `LsaLogonSessions.dll` - 登录会话库
- `SyslogNet.Client.dll` - Syslog 客户端（Logging 的依赖）

## ⚠️ 重要提示

**AdminUI 是客户端应用程序，需要连接到已安装的 Make Me Admin 服务**

- AdminUI **不包含** Service.exe（Windows 服务）
- 目标计算机必须**已安装并运行** Make Me Admin 服务
- AdminUI 通过 WCF 命名管道连接到本地服务
- 服务版本必须与 AdminUI 兼容

## 快速开始

### 打包安装程序

#### 方法一：使用快速打包脚本

双击运行：
```
AdminUISetup\快速打包.bat
```

#### 方法二：在 Visual Studio 中

1. 设置配置：
   - 解决方案配置：`Release`
   - 解决方案平台：`x64`

2. 生成安装包：
   - 右键 `AdminUISetup` 项目 → 生成

3. 查找 MSI 文件：
   - `AdminUISetup\bin\Release x64\Make Me Admin AdminUI x64.msi`
   - 或 `Installers\Make Me Admin AdminUI x64.msi`

### 安装要求

在安装 AdminUI 之前，目标计算机必须：

1. ✅ 已安装 Make Me Admin 服务（使用 `Setup` 项目安装）
2. ✅ 服务正在运行
3. ✅ 已安装 .NET Framework 4.8
4. ✅ Windows 7 或更高版本

### 安装步骤

1. 以管理员身份运行 MSI 文件
2. 按照安装向导完成安装
3. 从开始菜单启动 AdminUI
4. 验证可以连接到服务

## 使用场景

### 场景 1：服务器 + 管理端分离部署

**服务器端**：
- 安装完整的 Make Me Admin（使用 `Setup` 项目）
- 服务运行在服务器上

**管理端**：
- 在其他计算机上安装 AdminUI（使用 `AdminUISetup` 项目）
- AdminUI 连接到服务器上的服务

### 场景 2：本地管理

**单机部署**：
- 先安装完整的 Make Me Admin（使用 `Setup` 项目）
- 可选：安装 AdminUI（如果使用独立安装程序）

## 项目结构

```
AdminUISetup/
├── AdminUISetup.wixproj    # WIX 项目文件
├── Product.wxs              # 产品定义文件
├── en-US.wxl                # 英文本地化文件
├── 打包说明.md              # 详细打包说明
├── 快速打包.bat             # 快速打包脚本
└── README.md                # 本文件
```

## 与完整安装程序的区别

| 特性 | Setup（完整版） | AdminUISetup（管理端） |
|------|----------------|----------------------|
| **包含服务** | ✅ 是 | ❌ 否 |
| **安装服务** | ✅ 是 | ❌ 否 |
| **包含 AdminUI** | ❌ 否 | ✅ 是 |
| **包含用户端** | ✅ 是 | ❌ 否 |
| **包含组策略** | ✅ 是 | ❌ 否 |
| **文件大小** | 较大 (~10MB+) | 较小 (~5MB) |
| **用途** | 服务器/完整部署 | 管理端分发 |

## 版本管理

### 更新版本号

修改 `Product.wxs` 中的 `Version` 属性：
```xml
<Product ... Version="2.3" ...>
```

### 升级处理

- `MajorUpgrade` 配置会自动处理版本升级
- 安装新版本时会自动卸载旧版本

## 故障排除

### 问题：安装后无法连接服务

**解决方法**：
1. 检查服务状态：`Get-Service -Name "Make Me Admin"`
2. 如果服务未安装，先安装完整的 Make Me Admin
3. 如果服务版本不匹配，更新服务端

### 问题：MSI 生成失败

**解决方法**：
1. 确保 WIX Toolset v3.x 已安装
2. 检查所有依赖项目是否成功编译
3. 查看详细错误信息

## 相关文档

- [打包说明](打包说明.md) - 详细的打包和使用说明
- [Setup 项目文档](../Setup/打包说明.md) - 完整安装程序的打包说明
- [AdminUI 使用说明](../AdminUI/README.md) - AdminUI 的使用指南

## 参考

- [WIX Toolset 文档](https://wixtoolset.org/documentation/)
- [Make Me Admin 主项目](../README.md)
