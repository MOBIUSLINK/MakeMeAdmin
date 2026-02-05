# Make Me Admin (MG)

[![License: GPL-3.0](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://www.microsoft.com/windows)

**Make Me Admin (MG)** 是一个基于 .NET 开发的 Windows 权限管理系统，旨在为企业或组织提供一种安全、可控且便捷的方式，让普通用户在需要时段内临时获取管理员权限。

本项目是对原始 Make Me Admin 系统的功能增强与升级版，引入了集中化审批机制、实时通知、深色模式支持以及更强大的诊断工具。

---

## 🌟 核心功能

- **🚀 临时提权**：允许普通用户申请并获取临时的本地管理员权限，权限到期后自动移除。
- **🛡️ 集中化审批**：支持将申请提交至中央服务器进行手动审批或自动规则过滤。
- **🔔 实时通知**：利用 WCF 双工（Duplex）通信，确保审批结果能实时推送至客户端。
- **🌙 现代 UI**：客户端支持深色模式，提供更优雅的用户交互体验。
- **📝 全面日志**：集成了日志记录功能，记录所有的权限变更与申请操作，便于安全审计。
- **🛠️ 诊断工具**：内置 WCF 诊断与提权超时诊断工具，方便快速定位网络或服务配置问题。

---

## 🏗️ 系统架构

系统主要由以下模块组成：

| 模块名称 | 描述 |
| :--- | :--- |
| **UserRequestApp** | 客户端 UI（托盘程序），用户在此提交申请并接收状态通知。 |
| **ClientService** | 客户端后台服务，负责执行本地权限调整逻辑。 |
| **ServerService** | 中央管理服务，处理所有客户端的连接与申请逻辑。 |
| **AdminUI** | 管理员审批界面，用于集中管理并审批挂起的权限申请。 |
| **ConnectionTestTool** | 连通性测试工具，用于排查客户端与服务器之间的连接问题。 |
| **ServerDiagnosticsTool** | 服务器诊断工具，提供实时的服务状态监控。 |
| **ProcessCommunication** | 封装进程间通信（IPC）逻辑，支持 Named Pipes 与 TCP WCF 通信。 |
| **Shared** | 通用库，包含数据模型、接口定义与共享逻辑。 |

---

## ⚙️ 技术栈

- **开发语言**：C# 
- **框架**：.NET Framework
- **通信**：Windows Communication Foundation (WCF) - 支持 TCP 双工与命名管道
- **界面**：WinForms (支持双模式切换)
- **权限管理**：Windows Security & Principal API

---

## 🚀 快速上手

### 1. 环境准备
- Windows 10/11
- .NET Framework 4.7.2 或更高版本
- Visual Studio 2022

### 2. 编译项目
打开 `MakeMeAdmin.sln` 解决方案文件，恢复 NuGet 包后，直接编译整个项目。

### 3. 部署说明
- **服务器端**：在核心节点安装 `ServerService` 并运行 `AdminUI`。
- **客户端**：在终端机器安装 `ClientService` 与 `UserRequestApp`。
- **配置**：通过 `App.config` 或注册表配置服务器地址与安全模式（SecureMode）。

---

## 📸 预览

*(在此处可以运行 `generate_image` 生成界面预览图，或由用户手动替换)*

---

## 📜 许可证

本项目基于 [GPL-3.0 License](LICENSE) 开源。

---

## 👥 贡献者

- **Patrick Seymour** - 核心开发与原始作者
- **Etienne Croteau** - 法语本地化
- **MakeMeAdminMG Team** - 功能增强与维护

感谢所有对本项目做出贡献的开发者！
