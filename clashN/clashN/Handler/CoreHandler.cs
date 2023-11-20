using ClashN.Mode;
using ClashN.Resx;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ClashN.Handler;

/// <summary>
/// core进程处理类
/// </summary>
internal class CoreHandler
{
    private const string CoreConfigRes = Global.CoreConfigFileName;
        
    private CoreInfo _coreInfo;
    private Process _process;
    
    private readonly Action<bool, string> _showMsgHandler;

    public CoreHandler(Action<bool, string> showMsgHandler)
    {
        _showMsgHandler = showMsgHandler;
    }

    /// <summary>
    /// 载入Core
    /// </summary>
    public void LoadCore(Config config)
    {
        if (!Global.reloadCore)
        {
            return;
        }
        
        var item = ConfigProc.GetDefaultProfile(ref config);
        if (item == null)
        {
            CoreStop();
            
            ShowMsg(false, ResUI.CheckProfileSettings);
            return;
        }

        if (config.EnableTun && !Utils.IsAdministrator())
        {
            ShowMsg(true, ResUI.EnableTunModeFailed);
            return;
        }
        if (config.EnableTun && item.coreType == CoreKind.Clash)
        {
            ShowMsg(true, ResUI.TunModeCoreTip);
            return;
        }

        SetCore(config, item, out bool blChanged);
        
        var fileName = Utils.GetConfigPath(CoreConfigRes);
        if (CoreConfigHandler.GenerateClientConfig(item, fileName, false, out var msg) != 0)
        {
            CoreStop();
                
            ShowMsg(false, msg);
        }
        else
        {
            ShowMsg(true, msg);

            if (_process != null && !_process.HasExited && !blChanged)
            {
                MainFormHandler.Instance.ClashConfigReload(fileName);
            }
            else
            {
                CoreRestart(item);
            }
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
        try
        {
            if (_process != null)
            {
                KillProcess(_process);
                _process.Dispose();
                _process = null;
            }
            else
            {
                if (_coreInfo == null || _coreInfo.coreExes == null)
                {
                    return;
                }

                foreach (var vName in _coreInfo.coreExes)
                {
                    var existing = Process.GetProcessesByName(vName);
                    foreach (Process p in existing)
                    {
                        var path = p.MainModule.FileName;
                        if (path == $"{Utils.GetBinPath(vName, _coreInfo.coreType)}.exe")
                        {
                            KillProcess(p);
                        }
                    }
                }
            }

            //bool blExist = true;
            //if (processId > 0)
            //{
            //    Process p1 = Process.GetProcessById(processId);
            //    if (p1 != null)
            //    {
            //        p1.Kill();
            //        blExist = false;
            //    }
            //}
            //if (blExist)
            //{
            //    foreach (string vName in lstCore)
            //    {
            //        Process[] killPro = Process.GetProcessesByName(vName);
            //        foreach (Process p in killPro)
            //        {
            //            p.Kill();
            //        }
            //    }
            //}
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
            Process _p = Process.GetProcessById(pid);
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
        foreach (var name in _coreInfo.coreExes)
        {
            var vName = $"{name}.exe";
            vName = Utils.GetBinPath(vName, _coreInfo.coreType);
            if (File.Exists(vName))
            {
                fileName = vName;
                break;
            }
        }
        if (string.IsNullOrEmpty(fileName))
        {
            var msg = string.Format(ResUI.NotFoundCore, _coreInfo.coreUrl);
            ShowMsg(false, msg);
        }
        return fileName;
    }

    /// <summary>
    /// Core启动
    /// </summary>
    private void CoreStart(ProfileItem item)
    {
        ShowMsg(false, string.Format(ResUI.StartService, DateTime.Now.ToString()));
        ShowMsg(false, $"{ResUI.TbCoreType} {_coreInfo.coreType.ToString()}");

        try
        {
            var fileName = FindCoreExe();
            if (fileName == "") return;

            //Portable Mode
            var arguments = _coreInfo.arguments;
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
            p.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var msg = e.Data + Environment.NewLine;
                    ShowMsg(false, msg);
                }
            };
            p.Start();
            //p.PriorityClass = ProcessPriorityClass.High;
            p.BeginOutputReadLine();
            //processId = p.Id;
            _process = p;

            if (p.WaitForExit(1000))
            {
                throw new Exception(p.StandardError.ReadToEnd());
            }

            Global.processJob.AddProcess(p.Handle);
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
                
            var msg = ex.Message;
            ShowMsg(true, msg);
        }
    }

    private void ShowMsg(bool updateToTrayTooltip, string msg)
    {
        _showMsgHandler(updateToTrayTooltip, msg);
    }

    private void KillProcess(Process p)
    {
        try
        {
            p.CloseMainWindow();
            p.WaitForExit(100);
            if (!p.HasExited)
            {
                p.Kill();
                p.WaitForExit(100);
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    private int SetCore(Config config, ProfileItem item, out bool blChanged)
    {
        blChanged = true;
        if (item == null)
        {
            return -1;
        }
        var coreType = LazyConfig.Instance.GetCoreType(item);
        var tempInfo = LazyConfig.Instance.GetCoreInfo(coreType);
        if (tempInfo != null && _coreInfo != null && tempInfo.coreType == _coreInfo.coreType)
        {
            blChanged = false;
        }

        _coreInfo = tempInfo;
        if (_coreInfo == null)
        {
            return -1;
        }
        return 0;
    }
}