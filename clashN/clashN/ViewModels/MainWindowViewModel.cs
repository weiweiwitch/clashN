using ClashN.Handler;
using ClashN.Mode;
using ClashN.Views;
using MaterialDesignColors;
using MaterialDesignColors.ColorManipulation;
using MaterialDesignThemes.Wpf;
using NHotkey;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using ClashN.Tool;
using Application = System.Windows.Application;

namespace ClashN.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private StatisticsHandler? _statistics;
    private readonly PaletteHelper _paletteHelper = new();

    #region Views

    //public DashboardView GetDashboardView { get; }
    public ProxiesView GetProxyView { get; }

    public ProfilesView GetProfilesView { get; }
    public LogsView GetLogsView { get; }
    public ConnectionsView GetConnectionsView { get; }
    public SettingsView GetSettingsView { get; }
    public HelpView GetHelpView { get; }
    public PromotionView GetPromotionView { get; }

    [Reactive] public string SpeedUpload { get; set; } = "0.00";

    [Reactive] public string SpeedDownload { get; set; } = "0.00";

    #endregion Views

    #region System Proxy

    [Reactive] public bool BlSystemProxyClear { get; set; }

    [Reactive] public bool BlSystemProxySet { get; set; }

    [Reactive] public bool BlSystemProxyNothing { get; set; }

    [Reactive] public bool BlSystemProxyPac { get; set; }

    public ReactiveCommand<Unit, Unit> SystemProxyClearCmd { get; }
    public ReactiveCommand<Unit, Unit> SystemProxySetCmd { get; }
    public ReactiveCommand<Unit, Unit> SystemProxyNothingCmd { get; }

    public ReactiveCommand<Unit, Unit> SystemProxyPacCmd { get; }

    #endregion System Proxy

    #region Rule mode

    [Reactive] public bool BlModeRule { get; set; }

    [Reactive] public bool BlModeGlobal { get; set; }

    [Reactive] public bool BlModeDirect { get; set; }

    [Reactive] public bool BlModeNothing { get; set; }

    public ReactiveCommand<Unit, Unit> ModeRuleCmd { get; }
    public ReactiveCommand<Unit, Unit> ModeGlobalCmd { get; }
    public ReactiveCommand<Unit, Unit> ModeDirectCmd { get; }
    public ReactiveCommand<Unit, Unit> ModeNothingCmd { get; }

    #endregion Rule mode

    #region Timer

    // For Update Profile
    private DispatcherTimer? _updateTaskDispatcherTimer;
    private DateTime _autoUpdateSubTime = DateTime.Now;

    #endregion Timer

    #region Other

    public ReactiveCommand<Unit, Unit> AddProfileViaScanCmd { get; }
    public ReactiveCommand<Unit, Unit> SubUpdateCmd { get; }
    public ReactiveCommand<Unit, Unit> SubUpdateViaProxyCmd { get; }

    public ReactiveCommand<Unit, Unit> ReloadCmd { get; }
    public ReactiveCommand<Unit, Unit> NotifyLeftClickCmd { get; }

    [Reactive] public Icon NotifyIcon { get; set; }

    #endregion Other

    #region Init

    public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
    {
        // Views
        //GetDashboardView = new();
        GetProxyView = new ProxiesView();
        GetProfilesView = new ProfilesView();
        GetLogsView = new LogsView();
        GetConnectionsView = new ConnectionsView();
        GetSettingsView = new SettingsView();
        GetHelpView = new HelpView();
        GetPromotionView = new PromotionView();

        NoticeHandler.Instance.ConfigMessageQueue(snackbarMessageQueue);


        //System proxy
        SystemProxyClearCmd = ReactiveCommand.Create(() =>
        {
            SetListenerType(SysProxyType.ForcedClear);
        }); //, this.WhenAnyValue(x => x.BlSystemProxyClear, y => !y));
        SystemProxySetCmd = ReactiveCommand.Create(() =>
        {
            SetListenerType(SysProxyType.ForcedChange);
        }); //, this.WhenAnyValue(x => x.BlSystemProxySet, y => !y));
        SystemProxyNothingCmd = ReactiveCommand.Create(() =>
        {
            SetListenerType(SysProxyType.Unchanged);
        }); //, this.WhenAnyValue(x => x.BlSystemProxyNothing, y => !y));
        SystemProxyPacCmd = ReactiveCommand.Create(() =>
        {
            SetListenerType(SysProxyType.Pac);
        }); //, this.WhenAnyValue(x => x.BlSystemProxyNothing, y => !y));

        //Rule mode
        ModeRuleCmd = ReactiveCommand.Create(() =>
        {
            SetRuleModeCheck(ERuleMode.Rule);
        }); //, this.WhenAnyValue(x => x.BlModeRule, y => !y));
        ModeGlobalCmd = ReactiveCommand.Create(() =>
        {
            SetRuleModeCheck(ERuleMode.Global);
        }); //, this.WhenAnyValue(x => x.BlModeGlobal, y => !y));
        ModeDirectCmd = ReactiveCommand.Create(() =>
        {
            SetRuleModeCheck(ERuleMode.Direct);
        }); //, this.WhenAnyValue(x => x.BlModeDirect, y => !y));
        ModeNothingCmd = ReactiveCommand.Create(() =>
        {
            SetRuleModeCheck(ERuleMode.Unchanged);
        }); //, this.WhenAnyValue(x => x.BlModeNothing, y => !y));

        //Other
        AddProfileViaScanCmd = ReactiveCommand.CreateFromTask(() =>
        {
            return Locator.Current.GetService<ProfilesViewModel>()?.ScanScreenTaskAsync();
        });
        SubUpdateCmd = ReactiveCommand.Create(() =>
        {
            Locator.Current.GetService<ProfilesViewModel>()?.UpdateSubscriptionProcess(false, false);
        });
        SubUpdateViaProxyCmd = ReactiveCommand.Create(() =>
        {
            Locator.Current.GetService<ProfilesViewModel>()?.UpdateSubscriptionProcess(true, false);
        });
        ReloadCmd = ReactiveCommand.Create(() =>
        {
            Global.reloadCore = true;
            _ = LoadCore();
        });

        NotifyLeftClickCmd = ReactiveCommand.Create(() => { ShowHideWindow(null); });

        Global.ShowInTaskbar = true; //Application.Current.MainWindow.ShowInTaskbar;

        ThreadPool.RegisterWaitForSingleObject(App.ProgramStarted, OnProgramStarted, null, -1, false);

        RestoreUI();

        // Main Logic
        // init
        Init();
    }

    private void OnProgramStarted(object? state, bool timeout)
    {
        Utils.SaveLog("MainWindowViewModel:OnProgramStarted");

        Application.Current.Dispatcher.Invoke((Action)(() =>
        {
            var clipboardData = Utils.GetClipboardData();
            if (state != null && clipboardData != null)
            {
                if (string.IsNullOrEmpty(clipboardData) || !clipboardData.StartsWith(Global.ClashProtocol))
                {
                    return;
                }
            }

            ShowHideWindow(true);

            Locator.Current.GetService<ProfilesViewModel>()?.AddProfilesViaClipboard(true);

            StartAllTimerTask();
        }));
    }

    public void MyAppExit(bool blWindowsShutDown)
    {
        try
        {
            CoreHandler.Instance.CoreStop();

            //HttpProxyHandle.CloseHttpAgent(config);
            if (blWindowsShutDown)
            {
                SysProxyHandle.ResetIEProxy4WindowsShutDown();
            }
            else
            {
                SysProxyHandle.UpdateSysProxy(true);
            }

            StorageUI();

            ConfigProc.SaveConfig();

            _statistics?.Close();
        }
        catch
        {
        }
        finally
        {
            Application.Current.Shutdown();
            Environment.Exit(0);
        }
    }

    private void OnHotkeyHandler(object? sender, HotkeyEventArgs e)
    {
        switch (Utils.ToInt(e.Name))
        {
            case (int)GlobalHotkeyAction.ShowForm:
                ShowHideWindow(null);
                break;

            case (int)GlobalHotkeyAction.SystemProxyClear:
                SetListenerType(SysProxyType.ForcedClear);
                break;

            case (int)GlobalHotkeyAction.SystemProxySet:
                SetListenerType(SysProxyType.ForcedChange);
                break;

            case (int)GlobalHotkeyAction.SystemProxyUnchanged:
                SetListenerType(SysProxyType.Unchanged);
                break;

            case (int)GlobalHotkeyAction.SystemProxyPac:
                SetListenerType(SysProxyType.Pac);
                break;
        }

        e.Handled = true;
    }

    private void Init()
    {
        Utils.SaveLog("MainWindowViewModel:Init");
        
        MainFormHandler.BackupGuiNConfig(true);

        MainFormHandler.InitRegister();

        var config = LazyConfig.Instance.Config;
        if (config.EnableStatistics)
        {
            _statistics = new StatisticsHandler(CbStatisticUpdate);
        }

        // Timer 4 Update 
        Utils.SaveLog($"MainWindowViewModel:Init - Create Timer 4 UpdateTask");
        _autoUpdateSubTime = DateTime.Now;
        _updateTaskDispatcherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromHours(1)
        };
        _updateTaskDispatcherTimer.Tick += (_, _) =>
        {
            MainFormHandler.Instance.OnTimer4UpdateTask(ref _autoUpdateSubTime, config, CbUpdateTaskFinish);
        };

        // HotKey
        MainFormHandler.RegisterGlobalHotkey(config, OnHotkeyHandler);

        OnProgramStarted("shown", true);

        _ = LoadCore();
    }

    private async void CbUpdateTaskFinish(bool success, string msg)
    {
        NoticeHandler.SendMessage4ClashN(msg);

        if (success)
        {
            Global.reloadCore = true;

            await LoadCore();
        }
    }

    private void CbStatisticUpdate(ulong up, ulong down)
    {
        try
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (!Global.ShowInTaskbar)
                {
                    return;
                }

                SpeedUpload = @$"{Utils.HumanFy(up)}/s";
                SpeedDownload = @$"{Utils.HumanFy(down)}/s";
            }));
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    #endregion Init

    #region Core

    public async Task LoadCore()
    {
        Utils.SaveLog("MainWindowViewModel:LoadCore");

        var proxiesViewModel = Locator.Current.GetService<ProxiesViewModel>();
        proxiesViewModel?.ProxiesClear();

        await Task.Run(() => { CoreHandler.Instance.LoadCore(); });

        Global.reloadCore = false;

        ConfigProc.SaveConfig(false);

        var config = LazyConfig.Instance.Config;
        ChangePACButtonStatus(config.SysProxyType);

        SetRuleMode(config.RuleMode);

        proxiesViewModel?.ProxiesReload();
        proxiesViewModel?.ProxiesDelayTest();
        
        Locator.Current.GetService<ProfilesViewModel>()?.RefreshProfiles();
    }

    public void CloseCore()
    {
        ConfigProc.SaveConfig(false);

        ChangePACButtonStatus(SysProxyType.ForcedClear);

        CoreHandler.Instance.CoreStop();
    }

    #endregion Core

    #region System proxy and Rule mode

    public void SetListenerType(SysProxyType type)
    {
        var config = LazyConfig.Instance.Config;
        if (config.SysProxyType == type)
        {
            return;
        }

        config.SysProxyType = type;
        ChangePACButtonStatus(type);

        Locator.Current.GetService<ProxiesViewModel>()?.ReloadSystemProxySelected();
    }

    private void ChangePACButtonStatus(SysProxyType type)
    {
        SysProxyHandle.UpdateSysProxy(false);

        BlSystemProxyClear = type == SysProxyType.ForcedClear;
        BlSystemProxySet = type == SysProxyType.ForcedChange;
        BlSystemProxyNothing = type == SysProxyType.Unchanged;
        BlSystemProxyPac = type == SysProxyType.Pac;

        NoticeHandler.SendMessage4ClashN($"Change system proxy");

        ConfigProc.SaveConfig(false);

        var config = LazyConfig.Instance.Config;
        NotifyIcon = MainFormHandler.Instance.GetNotifyIcon(config);
    }

    public void SetRuleModeCheck(ERuleMode mode)
    {
        var config = LazyConfig.Instance.Config;
        if (config.RuleMode == mode)
        {
            return;
        }

        SetRuleMode(mode);

        Locator.Current.GetService<ProxiesViewModel>()?.ReloadRuleModeSelected();
        
        ConfigProc.SaveConfig(false);
    }

    private void SetRuleMode(ERuleMode mode)
    {
        BlModeRule = mode == ERuleMode.Rule;
        BlModeGlobal = mode == ERuleMode.Global;
        BlModeDirect = mode == ERuleMode.Direct;
        BlModeNothing = mode == ERuleMode.Unchanged;

        var config = LazyConfig.Instance.Config;
        NoticeHandler.SendMessage4ClashN($"Set rule mode {config.RuleMode.ToString()}->{mode.ToString()}");

        config.RuleMode = mode;

        if (mode != ERuleMode.Unchanged)
        {
            var headers = new Dictionary<string, string>
            {
                { "mode", config.RuleMode.ToString().ToLower() }
            };
            MainFormHandler.Instance.ClashConfigUpdate(headers);
        }
    }

    #endregion System proxy and Rule mode

    #region UI

    public static void ShowHideWindow(bool? blShow)
    {
        Utils.SaveLog($"MainWindowViewModel:ShowHideWindow");

        var bl = blShow.HasValue ? blShow.Value : !Global.ShowInTaskbar;
        if (bl)
        {
            Application.Current.MainWindow.Show();
            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }

            Application.Current.MainWindow.Activate();
            Application.Current.MainWindow.Focus();
        }
        else
        {
            Application.Current.MainWindow.Hide();
        }

        Global.ShowInTaskbar = bl;
    }

    private void RestoreUI()
    {
        Utils.SaveLog("MainWindowViewModel:RestoreUI");
        
        var config = LazyConfig.Instance.Config;

        ModifyTheme(config.UiItem.ColorModeDark);

        var colorPrimaryName = config.UiItem.ColorPrimaryName;
        if (!string.IsNullOrEmpty(colorPrimaryName))
        {
            var swatch = new SwatchesProvider().Swatches.FirstOrDefault(t => t.Name == colorPrimaryName);
            if (swatch?.ExemplarHue.Color != null)
            {
                ChangePrimaryColor(swatch.ExemplarHue.Color);
            }
        }

        var mainWindow = Application.Current.MainWindow;
        if (config.UiItem.MainWidth > 0 && config.UiItem.MainHeight > 0)
        {
            mainWindow.Width = config.UiItem.MainWidth;
            mainWindow.Height = config.UiItem.MainHeight;
        }

        var hWnd = new WindowInteropHelper(mainWindow).EnsureHandle();
        var g = Graphics.FromHwnd(hWnd);
        if (mainWindow.Width > SystemInformation.WorkingArea.Width * 96 / g.DpiX)
        {
            mainWindow.Width = SystemInformation.WorkingArea.Width * 96 / g.DpiX;
        }

        if (mainWindow.Height > SystemInformation.WorkingArea.Height * 96 / g.DpiY)
        {
            mainWindow.Height = SystemInformation.WorkingArea.Height * 96 / g.DpiY;
        }

        // Auto Hide
        if (config.AutoHideStartup)
        {
            Observable.Range(1, 1)
                .Delay(TimeSpan.FromSeconds(1))
                .Subscribe(x => { Application.Current.Dispatcher.Invoke((Action)(() => { ShowHideWindow(false); })); });
        }
    }

    private void StorageUI()
    {
        var config = LazyConfig.Instance.Config;
        config.UiItem.MainWidth = Application.Current.MainWindow.Width;
        config.UiItem.MainHeight = Application.Current.MainWindow.Height;
    }

    public void ModifyTheme(bool isDarkTheme)
    {
        var theme = _paletteHelper.GetTheme();

        theme.SetBaseTheme(isDarkTheme ? Theme.Dark : Theme.Light);
        _paletteHelper.SetTheme(theme);

        Utils.SetDarkBorder(Application.Current.MainWindow, isDarkTheme);
    }

    public void ChangePrimaryColor(System.Windows.Media.Color color)
    {
        //  var  Swatches = new SwatchesProvider().Swatches;

        var theme = _paletteHelper.GetTheme();

        theme.PrimaryLight = new ColorPair(color.Lighten());
        theme.PrimaryMid = new ColorPair(color);
        theme.PrimaryDark = new ColorPair(color.Darken());

        _paletteHelper.SetTheme(theme);
    }

    #endregion UI

    public void StartAllTimerTask()
    {
        _updateTaskDispatcherTimer?.Start();
    }

    public void StopAllTimerTask()
    {
        _updateTaskDispatcherTimer?.Stop();
    }
}