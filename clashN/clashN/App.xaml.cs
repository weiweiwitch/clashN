using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using ClashN.Handler;
using ClashN.Tool;

namespace ClashN;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static readonly EventWaitHandle? ProgramStarted;

    static App()
    {
        ProgramStarted = new EventWaitHandle(false, EventResetMode.AutoReset, "ProgramStartedEvent", out var bCreatedNew);
        if (!bCreatedNew)
        {
            ProgramStarted.Set();
            
            Current.Shutdown();
            
            Environment.Exit(-1);
        }
    }

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// 只打开一个进程
    /// </summary>
    /// <param name="e"></param>
    protected override void OnStartup(StartupEventArgs e)
    {
        foreach (var arg in e.Args)
        {
            Utils.SetClipboardData(arg);
        }

        Global.ProcessJob = new Job();

        // Logging
        Logging.Setup();
        Utils.SaveLog($"App:OnStartup - ClashN start up | {Utils.GetVersion()} | {Utils.GetExePath()}");
        Logging.ClearLogs();

        Init();

        var lang = Utils.RegReadValue(Global.MyRegPath, Global.MyRegKeyLanguage, Global.Languages[0]);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
        Utils.SaveLog($"App:OnStartup - Thread.CurrentThread.CurrentUICulture Set {lang}");
        
        base.OnStartup(e);
    }

    private void Init()
    {
        if (ConfigProc.LoadConfig() != 0)
        {
            Utils.SaveLogError($"Loading GUI configuration file is abnormal, please restart the application {Environment.NewLine} 加载GUI配置文件异常,请重启应用");
            UI.ShowWarning($"Loading GUI configuration file is abnormal,please restart the application{Environment.NewLine}加载GUI配置文件异常,请重启应用");
            
            Current.Shutdown();
            
            Environment.Exit(0);
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Utils.SaveLog("App_DispatcherUnhandledException", e.Exception);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject != null)
        {
            Utils.SaveLog("CurrentDomain_UnhandledException", (Exception)e.ExceptionObject!);
        }
        else
        {
            Utils.SaveLog("CurrentDomain_UnhandledException");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Utils.SaveLog("TaskScheduler_UnobservedTaskException", e.Exception);
    }
}