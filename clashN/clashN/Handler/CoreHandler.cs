using ClashN.Mode;
using ClashN.Resx;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using ClashN.Tool;
using ClashN.ViewModels;

namespace ClashN.Handler;

/// <summary>
/// core进程处理类
/// </summary>
internal class CoreHandler
{
    private static Lazy<CoreHandler> _instance = new(() => new CoreHandler());

    public static CoreHandler Instance => _instance.Value;
    
    private const string CoreConfigRes = Global.CoreConfigFileName;

    private CoreInfo _coreInfo;
    private Process _process;

    /// <summary>
    /// 载入Core
    /// </summary>
    public void LoadCore()
    {
        Utils.SaveLogDebug($"CoreHandler:LoadCore - Start to Load Core: {Global.ReloadCore}");

        if (!Global.ReloadCore)
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        var item = ConfigHandler.GetDefaultProfile(ref config);
        if (item == null)
        {
            CoreStop();

            ShowMsg(false, LogType.Log4ClashN, ResUI.CheckProfileSettings);
            return;
        }

        if (config.EnableTun && !Utils.IsAdministrator())
        {
            ShowMsg(true, LogType.Log4ClashN, ResUI.EnableTunModeFailed);
            return;
        }

        if (config.EnableTun && item.CoreType == CoreKind.Clash)
        {
            ShowMsg(true, LogType.Log4ClashN, ResUI.TunModeCoreTip);
            return;
        }

        SetCore(config, item, out var blChanged);

        var fileName = Utils.GetConfigPath(CoreConfigRes);
        Utils.SaveLogDebug($"CoreHandler:LoadCore - Load file: {fileName}");
        if (CoreConfigHandler.GenerateClientConfig(item, fileName, false, out var msg) != 0)
        {
            Utils.SaveLogDebug("CoreHandler:LoadCore - GenerateClientConfig Failed");
            
            CoreStop();

            ShowMsg(false, LogType.Log4ClashN, msg);
            return;
        }

        ShowMsg(true, LogType.Log4ClashN, msg);

        if (_process != null && !_process.HasExited && !blChanged)
        {
            MainFormHandler.Instance.ClashConfigReload(fileName);
        }
        else
        {
            CoreRestart(item);
        }
    }

    /// <summary>
    /// Core重启
    /// </summary>
    private void CoreRestart(ProfileItem item)
    {
        CoreStop();

        Thread.Sleep(1000);

        CoreStart(item);
    }

    /// <summary>
    /// Core停止
    /// </summary>
    public void CoreStop()
    {
        Utils.SaveLogDebug(
            $"CoreHandler:CoreStop - Stop the Core: {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");

        try
        {
            if (_process != null)
            {
                KillProcess(_process);
                
                Utils.SaveLogDebug(
                    $"CoreHandler:CoreStop - KillProcess finished: {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");
                
                _process.Dispose();
                _process = null;
            }
            else
            {
                if (_coreInfo == null || _coreInfo.CoreExes == null)
                {
                    return;
                }

                foreach (var vName in _coreInfo.CoreExes)
                {
                    var existing = Process.GetProcessesByName(vName);
                    foreach (var p in existing)
                    {
                        var path = p.MainModule.FileName;
                        if (path == $"{Utils.GetBinPath(vName, _coreInfo.CoreType)}.exe")
                        {
                            KillProcess(p);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    /// <summary>
    /// Core停止
    /// </summary>
    public void CoreStopPid(int pid)
    {
        try
        {
            var _p = Process.GetProcessById(pid);
            KillProcess(_p);
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    private string FindCoreExe()
    {
        var fileName = string.Empty;
        foreach (var name in _coreInfo.CoreExes)
        {
            var vName = $"{name}.exe";
            vName = Utils.GetBinPath(vName, _coreInfo.CoreType);
            if (File.Exists(vName))
            {
                fileName = vName;
                break;
            }
        }

        if (string.IsNullOrEmpty(fileName))
        {
            var msg = string.Format(ResUI.NotFoundCore, _coreInfo.CoreUrl);
            ShowMsg(false, LogType.Log4ClashN, msg);
        }

        return fileName;
    }

    /// <summary>
    /// Core启动
    /// </summary>
    private void CoreStart(ProfileItem item)
    {
        Utils.SaveLogDebug(
            $"CoreHandler:CoreStart - Start the Core: {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");

        ShowMsg(false, LogType.Log4ClashN,
            string.Format(ResUI.StartService, DateTime.Now.ToString(CultureInfo.CurrentCulture)));
        ShowMsg(false, LogType.Log4ClashN, $"{ResUI.TbCoreType} {_coreInfo.CoreType.ToString()}");

        try
        {
            var fileName = FindCoreExe();
            if (fileName == "")
            {
                Utils.SaveLogError($"CoreHandler:CoreStart - Can't find any core");
                return;
            }

            //Portable Mode
            var arguments = _coreInfo.Arguments;
            var data = Utils.GetPath("data");
            if (Directory.Exists(data))
            {
                arguments += $" -d \"{data}\"";
            }

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = Utils.GetConfigPath(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
            //if (config.enableTun)
            //{
            //    p.StartInfo.Verb = "runas";
            //}
            p.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    return;
                }

                var msg = e.Data + Environment.NewLine;
                ShowMsg(false, LogType.Log4Clash, msg);
            };
            p.Start();
            //p.PriorityClass = ProcessPriorityClass.High;
            p.BeginOutputReadLine();
            //processId = p.Id;
            _process = p;

            Utils.SaveLogDebug($"CoreHandler:CoreStart - Core started 1. fileName: {fileName}, arguments: {arguments}");

            if (p.WaitForExit(1000))
            {
                throw new Exception(p.StandardError.ReadToEnd());
            }

            Global.ProcessJob.AddProcess(p.Handle);

            Utils.SaveLogDebug($"CoreHandler:CoreStart - Core started after AddProcess");
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);

            var msg = ex.Message;
            ShowMsg(true, LogType.Log4ClashN, msg);
        }
    }

    private static void ShowMsg(bool updateToTrayTooltip, LogType logType, string msg)
    {
        NoticeHandler.Instance.OnShowMsg(updateToTrayTooltip, logType, msg);
    }

    private static void KillProcess(Process p)
    {
        try
        {
            p.CloseMainWindow();
            
            p.WaitForExit(1000);
            if (!p.HasExited)
            {
                p.Kill();
                p.WaitForExit(1000);
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    private void SetCore(Config config, ProfileItem item, out bool blChanged)
    {
        blChanged = true;
        var coreType = LazyConfig.GetCoreType(item);
        var tempInfo = LazyConfig.Instance.GetCoreInfo(coreType);
        if (tempInfo != null && _coreInfo != null && tempInfo.CoreType == _coreInfo.CoreType)
        {
            blChanged = false;
        }

        _coreInfo = tempInfo;
    }
}