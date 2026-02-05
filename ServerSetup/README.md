# Make Me Admin 服务器端安装程序

## 概述

`ServerSetup` 项目用于创建 Make Me Admin 服务器端的独立安装程序。这个安装程序只包含 Service（Windows 服务）及其依赖项，适用于服务器端部署场景。

## 功能特点

- ✅ **只包含服务**：只包含 Service.exe 及其依赖项
- ✅ **自动服务安装**：安装时自动安装并启动 Windows 服务
- ✅ **故障自动恢复**：配置服务故障后自动重启
- ✅ **版本管理**：支持自动升级
- ✅ **WIX 打包**：使用与主安装程序相同的打包方式

## 包含的组件

### 主程序
- `MakeMeAdminService.exe` - Windows 服务主程序

### 依赖库
- `ProcessCommunication.dll` - WCF 通信接口
- `Shared.dll` - 共享类和数据结构
- `UserList.dll` - 用户列表管理
- `Settings.dll` - 设置管理
- `Logging.dll` - 日志库
- `LsaLogonSessions.dll` - 登录会话库
- `SyslogNet.Client.dll` - Syslog 客户端（Logging 的依赖）

## ⚠️ 重要提示

**这是服务器端安装程序，只包含 Service**

- ServerSetup **不包含** UserRequestApp.exe（用户端）
- ServerSetup **不包含** AdminUI.exe（管理端）
- ServerSetup **不包含** 组策略文件
- 用户端和管理端需要单独安装

## 快速开始

### 打包安装程序

#### 方法一：使用快速打包脚本

双击运行：
```
ServerSetup\快速打包.bat
```

#### 方法二：在 Visual Studio 中

1. 设置配置：
   - 解决方案配置：`Release`
   - 解决方案平台：`x64`

2. 生成安装包：
   - 右键 `ServerSetup` 项目 → 生成

3. 查找 MSI 文件：
   - `ServerSetup\bin\Release x64\Make Me Admin Server 2.3.0 x64.msi`
   - 或 `Installers\Make Me Admin Server 2.3.0 x64.msi`

### 安装要求

在安装服务器端之前，目标计算机必须：

1. ✅ Windows 7 或更高版本
2. ✅ 已安装 .NET Framework 4.8
3. ✅ 管理员权限（安装时需要）
4. ✅ 开放 TCP 端口（WCF 默认端口）

### 安装步骤

1. 以管理员身份运行 MSI 文件
2. 按照安装向导完成安装
3. 服务会自动安装并启动
4. 验证服务状态：`Get-Service -Name "Make Me Admin"`

## 使用场景

### 场景 1：服务器 + 客户端分离部署

**服务器端**：
- 安装 ServerSetup（包含 Service）
- 服务运行在服务器上

**用户端**：
- 在其他计算机上安装用户端安装程序
- 配置服务器地址连接到服务器

**管理端**：
- 在其他计算机上安装 AdminUISetup
- 配置服务器地址连接到服务器

### 场景 2：单机部署

**单机部署**：
- 先安装 ServerSetup（包含 Service）
- 可选：安装用户端和管理端（如果使用独立安装程序）

## 项目结构

```
ServerSetup/
├── ServerSetup.wixproj    # WIX 项目文件
├── Product.wxs              # 产品定义文件
├── en-US.wxl                # 英文本地化文件
├── 打包说明.md              # 详细打包说明
├── 快速打包.bat             # 快速打包脚本
└── README.md                # 本文件
```

## 与完整安装程序的区别

| 特性 | Setup（完整版） | ServerSetup（服务器端） |
|------|----------------|----------------------|
| **包含服务** | ✅ 是 | ✅ 是 |
| **安装服务** | ✅ 是 | ✅ 是 |
| **包含用户端** | ✅ 是 | ❌ 否 |
| **包含管理端** | ❌ 否 | ❌ 否 |
| **包含组策略** | ✅ 是 | ❌ 否 |
| **文件大小** | 较大 (~10MB+) | 较小 (~5MB) |
| **用途** | 完整部署 | 服务器端部署 |

## 相关文档

- [打包说明](打包说明.md) - 详细的打包和使用说明
- [架构重新设计方案](../架构重新设计方案.md) - 架构设计说明
- [打包架构说明](../打包架构说明.md) - 打包架构对比

## 参考

- [WIX Toolset 文档](https://wixtoolset.org/documentation/)
- [Make Me Admin 主项目](../README.md)
