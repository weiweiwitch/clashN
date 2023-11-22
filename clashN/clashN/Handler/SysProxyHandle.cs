using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using ClashN.Mode;
using ClashN.Properties;
using ClashN.Tool;
using PacLib;

namespace ClashN.Handler;

internal enum RetErrors
{
    RetNoError = 0,
    InvalidFormat = 1,
    NoPermission = 2,
    SyscallFailed = 3,
    NoMemory = 4,
    InvalidOptionCount = 5,
}

public static class SysProxyHandle
{
    static SysProxyHandle()
    {
        try
        {
            FileManager.UncompressFile(Utils.GetTempPath("sysproxy.exe"),
                Environment.Is64BitOperatingSystem ? Resources.sysproxy64_exe : Resources.sysproxy_exe);
        }
        catch (IOException ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    public static bool UpdateSysProxy(bool forceDisable)
    {
        Utils.SaveLogDebug($"SysProxyHandle:UpdateSysProxy - Start, forceDisable: {forceDisable}");

        var config = LazyConfig.Instance.Config;
        var type = config.SysProxyType;

        if (forceDisable && (type == SysProxyType.ForcedChange || type == SysProxyType.Pac))
        {
            type = SysProxyType.ForcedClear;
        }

        try
        {
            var port = config.HttpPort;
            var socksPort = config.SocksPort;
            if (port <= 0)
            {
                return false;
            }

            if (type == SysProxyType.ForcedChange)
            {
                var strExceptions = $"{config.ConstItem.DefIeProxyExceptions};{config.SystemProxyExceptions}";

                string strProxy;
                if (string.IsNullOrEmpty(config.SystemProxyAdvancedProtocol))
                {
                    strProxy = $"{Global.Loopback}:{port}";
                }
                else
                {
                    strProxy = config.SystemProxyAdvancedProtocol
                        .Replace("{ip}", Global.Loopback)
                        .Replace("{http_port}", port.ToString())
                        .Replace("{socks_port}", socksPort.ToString());
                }

                SetIEProxy(true, strProxy, strExceptions);
            }
            else if (type == SysProxyType.ForcedClear)
            {
                ResetIEProxy();
            }
            else if (type == SysProxyType.Unchanged)
            {
            }
            else if (type == SysProxyType.Pac)
            {
                PacHandler.Start(Utils.GetConfigPath(), port, config.PacPort);
                var strProxy = $"{Global.HttpProtocol}{Global.Loopback}:{config.PacPort}/pac?t={DateTime.Now.Ticks}";
                SetIEProxy(false, strProxy, "");
            }

            if (type != SysProxyType.Pac)
            {
                PacHandler.Stop();
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }

        Utils.SaveLogDebug($"SysProxyHandle:UpdateSysProxy - Finished. type: {type}");

        return true;
    }

    public static void ResetIEProxy4WindowsShutDown()
    {
        try
        {
            //TODO To be verified
            Utils.RegWriteValue(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", "ProxyEnable", 0);
        }
        catch
        {
        }
    }

    private static void SetIEProxy(bool global, string strProxy, string strExceptions)
    {
        var arguments = global
            ? $"global {strProxy} {strExceptions}"
            : $"pac {strProxy}";

        ExecSysProxy(arguments);
    }

    // set system proxy to 1 (null) (null) (null)
    private static bool ResetIEProxy()
    {
        try
        {
            // clear user-wininet.json
            //_userSettings = new SysproxyConfig();
            //Save();
            // clear system setting
            ExecSysProxy("set 1 - - -");
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    private static void ExecSysProxy(string arguments)
    {
        // using event to avoid hanging when redirect standard output/error
        // ref: https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
        // and http://blog.csdn.net/zhangweixing0/article/details/7356841
        using (var outputWaitHandle = new AutoResetEvent(false))
        using (var errorWaitHandle = new AutoResetEvent(false))
        {
            using (var process = new Process())
            {
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = Utils.GetTempPath("sysproxy.exe");
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = Utils.GetTempPath();
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                // Need to provide encoding info, or output/error strings we got will be wrong.
                process.StartInfo.StandardOutputEncoding = Encoding.Unicode;
                process.StartInfo.StandardErrorEncoding = Encoding.Unicode;

                process.StartInfo.CreateNoWindow = true;

                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };
                try
                {
                    process.Start();

                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();

                    process.WaitForExit();
                }
                catch (Win32Exception e)
                {
                    // log the arguments
                    throw new Exception(process.StartInfo.Arguments, e);
                }

                var stderr = error.ToString();
                var stdout = output.ToString();

                var exitCode = process.ExitCode;
                if (exitCode != (int)RetErrors.RetNoError)
                {
                    throw new Exception(stderr);
                }
            }
        }
    }
}