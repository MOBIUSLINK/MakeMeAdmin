//
// 提权时限诊断窗体：用于检查为什么用户端未能按审批时间移除管理员权限。
//

namespace SinclairCC.MakeMeAdmin
{
    using System;
    using System.Text;
    using System.Security.Principal;
    using System.ServiceModel;
    using System.Windows.Forms;

    /// <summary>
    /// 提权时限诊断：显示服务器审批时长、本地注册表、加密配置等，便于排查“始终永久”问题。
    /// </summary>
    internal partial class PrivilegeTimeoutDiagnosticsForm : Form
    {
        public PrivilegeTimeoutDiagnosticsForm()
        {
            InitializeComponent();
        }

        private void PrivilegeTimeoutDiagnosticsForm_Load(object sender, EventArgs e)
        {
            reportTextBox.Text = "正在收集诊断信息...";
            reportTextBox.Select(0, 0);
            this.Refresh();

            string report = BuildDiagnosticsReport();
            reportTextBox.Text = report;
            reportTextBox.Select(0, 0);
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(reportTextBox.Text);
                MessageBox.Show("已复制到剪贴板。", "复制", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("复制失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string BuildDiagnosticsReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== 提权时限诊断报告 ==========");
            sb.AppendLine("生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            WindowsIdentity currentIdentity = null;
            try
            {
                currentIdentity = WindowsIdentity.GetCurrent();
            }
            catch (Exception ex)
            {
                sb.AppendLine("[错误] 无法获取当前用户身份: " + ex.Message);
                return sb.ToString();
            }

            string userSid = currentIdentity?.User?.Value ?? "(未知)";
            string userName = currentIdentity?.Name ?? "(未知)";

            sb.AppendLine("【1. 当前用户】");
            sb.AppendLine("  用户名: " + userName);
            sb.AppendLine("  SID: " + userSid);
            sb.AppendLine();

            // 2. 服务器返回的审批时长
            sb.AppendLine("【2. 服务器审批时长 (GetApprovedTimeout)】");
            try
            {
                Tuple<ChannelFactory<IAdminGroup>, IAdminGroup> channelTuple = null;
                try
                {
                    channelTuple = CreateTcpChannelForDiagnostics();
                    if (channelTuple != null)
                    {
                        int? approvedTimeout = channelTuple.Item2.GetApprovedTimeout();
                        if (approvedTimeout.HasValue)
                        {
                            if (approvedTimeout.Value == 0)
                                sb.AppendLine("  结果: 永久 (0 分钟)");
                            else
                                sb.AppendLine("  结果: " + approvedTimeout.Value + " 分钟");
                        }
                        else
                            sb.AppendLine("  结果: 未返回 (null)。可能服务端未存储该用户的超时覆盖。");
                    }
                    else
                        sb.AppendLine("  结果: 无法连接服务器，未查询。");
                }
                finally
                {
                    if (channelTuple != null)
                    {
                        try
                        {
                            var comm = channelTuple.Item2 as ICommunicationObject;
                            if (comm != null && comm.State == CommunicationState.Opened)
                                comm.Close();
                        }
                        catch { }
                        try
                        {
                            channelTuple.Item1?.Close();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("  错误: " + ex.Message);
                if (ex.InnerException != null)
                    sb.AppendLine("  内部: " + ex.InnerException.Message);
            }
            sb.AppendLine();

            // 3. 本地注册表 TimeoutOverrides
            sb.AppendLine("【3. 本地注册表 (Timeout Overrides)】");
            sb.AppendLine("  说明: 用户端将审批时长写入 HKLM\\Software\\...\\Make Me Admin\\Timeout Overrides。");
            sb.AppendLine("        本地客户端服务以 SYSTEM 身份读取同一注册表。若当前用户无写 HKLM 权限，则无法写入，服务端看到的即为空或默认。");
            try
            {
                var overrides = Settings.TimeoutOverrides;
                if (overrides != null && overrides.Count > 0)
                {
                    if (overrides.ContainsKey(userSid))
                    {
                        string val = overrides[userSid];
                        if (val == "0")
                            sb.AppendLine("  当前用户 SID 的覆盖值: 永久 (0)");
                        else
                            sb.AppendLine("  当前用户 SID 的覆盖值: " + val + " 分钟");
                    }
                    else
                        sb.AppendLine("  当前用户 SID 在 TimeoutOverrides 中: 未找到。");
                    sb.AppendLine("  所有覆盖项数量: " + overrides.Count);
                }
                else
                    sb.AppendLine("  TimeoutOverrides: 为空或未配置。");
            }
            catch (Exception ex)
            {
                sb.AppendLine("  读取失败: " + ex.Message);
            }

            // 尝试写入（检测是否有写权限）
            try
            {
                var current = Settings.TimeoutOverrides;
                Settings.TimeoutOverrides = current;
                sb.AppendLine("  写入测试: 成功（当前进程可写注册表）。");
            }
            catch (Exception ex)
            {
                sb.AppendLine("  写入测试: 失败 - " + ex.Message);
                sb.AppendLine("  >>> 可能原因: 当前用户无 HKLM 写权限，审批时长无法写入，本地服务会使用默认超时（如 0=永久）。");
            }
            sb.AppendLine();

            // 4. 默认超时
            sb.AppendLine("【4. 默认管理员权限超时 (AdminRightsTimeout)】");
            try
            {
                int def = Settings.AdminRightsTimeout;
                if (def == 0)
                    sb.AppendLine("  值: 0 分钟（表示永久）");
                else
                    sb.AppendLine("  值: " + def + " 分钟");
            }
            catch (Exception ex)
            {
                sb.AppendLine("  读取失败: " + ex.Message);
            }
            sb.AppendLine();

            // 5. 用户端读取的 EncryptedSettings（当前用户 AppData）
            sb.AppendLine("【5. 用户端读取的过期时间 (EncryptedSettings)】");
            string userAppDataPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Make Me Admin", "users.xml");
            sb.AppendLine("  用户端读取路径: " + userAppDataPath);
            try
            {
                var encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
                DateTime? exp = encryptedSettings.GetExpirationTime(currentIdentity.User);
                if (exp.HasValue)
                {
                    if (exp.Value <= DateTime.Now)
                        sb.AppendLine("  当前用户过期时间: " + exp.Value.ToString("yyyy-MM-dd HH:mm:ss") + " (已过期)");
                    else
                        sb.AppendLine("  当前用户过期时间: " + exp.Value.ToString("yyyy-MM-dd HH:mm:ss") + " (未过期)");
                }
                else
                    sb.AppendLine("  当前用户过期时间: 未找到（可能为永久或不在本列表）。");
            }
            catch (Exception ex)
            {
                sb.AppendLine("  读取失败: " + ex.Message);
            }
            sb.AppendLine();

            // 6. 服务端使用的 EncryptedSettings 路径说明
            sb.AppendLine("【6. 本地客户端服务使用的数据路径】");
            sb.AppendLine("  说明: 本地「Make Me Admin 客户端服务」以 SYSTEM 身份运行。");
            string systemAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            sb.AppendLine("  当前进程 ApplicationData: " + systemAppData);
            sb.AppendLine("  服务进程(SYSTEM) 的 ApplicationData 通常为: C:\\Windows\\System32\\config\\systemprofile\\AppData\\Roaming");
            sb.AppendLine("  因此服务读写的是: C:\\Windows\\System32\\config\\systemprofile\\AppData\\Roaming\\Make Me Admin\\users.xml");
            sb.AppendLine("  到期移除逻辑由该文件中的过期时间驱动；若添加用户时未写入过期时间，则视为永久。");
            sb.AppendLine();

            sb.AppendLine("========== 诊断结束 ==========");
            return sb.ToString();
        }

        /// <summary>
        /// 仅用于诊断的简单 TCP 通道，不依赖主窗体状态。
        /// </summary>
        private static Tuple<ChannelFactory<IAdminGroup>, IAdminGroup> CreateTcpChannelForDiagnostics()
        {
            string serverAddress = Settings.ServerAddress;
            if (string.IsNullOrWhiteSpace(serverAddress))
                return null;
            string tcpAddress = Shared.GetTcpServiceAddress(serverAddress);
            NetTcpBinding binding;
            if (Settings.UseSecureMode)
            {
                binding = new NetTcpBinding(SecurityMode.Transport);
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            }
            else
            {
                binding = new NetTcpBinding(SecurityMode.None);
            }
            var factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
            IAdminGroup channel = factory.CreateChannel();
            ICommunicationObject comm = channel as ICommunicationObject;
            if (comm != null && comm.State == CommunicationState.Created)
                comm.Open();
            if (comm != null && comm.State != CommunicationState.Opened)
                return null;
            return new Tuple<ChannelFactory<IAdminGroup>, IAdminGroup>(factory, channel);
        }
    }
}
