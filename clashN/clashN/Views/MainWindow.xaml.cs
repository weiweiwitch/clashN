using ClashN.Resx;
using ClashN.ViewModels;
using ReactiveUI;
using Splat;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows;

namespace ClashN.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        
        Closing += MainWindow_Closing;
        App.Current.SessionEnding += Current_SessionEnding;

        ViewModel = new MainWindowViewModel(MainSnackbar.MessageQueue!);
        Locator.CurrentMutable.RegisterLazySingleton(() => ViewModel, typeof(MainWindowViewModel));

        this.WhenActivated(disposables =>
        {
            //this.OneWayBind(ViewModel, vm => vm.GetDashboardView, v => v.dashboardTabItem.Content).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.GetProxyView, v => v.ProxiesTabItem.Content).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.GetProfilesView, v => v.ProfilesTabItem.Content).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.GetLogsView, v => v.LogsTabItem.Content).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.GetConnectionsView, v => v.ConnectionsTabItem.Content).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.GetSettingsView, v => v.SettingsTabItem.Content).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.GetHelpView, v => v.HelpTabItem.Content).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.GetPromotionView, v => v.PromotionTabItem.Content).DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.SpeedUpload, v => v.TxtSpeedUpload.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.SpeedDownload, v => v.TxtSpeedDownload.Text).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.BlSystemProxyClear, v => v.MenuSystemProxyClear2.Visibility, conversionHint: BooleanToVisibilityHint.UseHidden, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlSystemProxySet, v => v.MenuSystemProxySet2.Visibility, conversionHint: BooleanToVisibilityHint.UseHidden, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlSystemProxyNothing, v => v.MenuSystemProxyNothing2.Visibility, conversionHint: BooleanToVisibilityHint.UseHidden, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlSystemProxyPac, v => v.MenuSystemProxyPac2.Visibility, conversionHint: BooleanToVisibilityHint.UseHidden, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SystemProxyClearCmd, v => v.MenuSystemProxyClear).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SystemProxySetCmd, v => v.MenuSystemProxySet).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SystemProxyPacCmd, v => v.MenuSystemProxyPac).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SystemProxyNothingCmd, v => v.MenuSystemProxyNothing).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.BlModeRule, v => v.MenuModeRule2.Visibility, conversionHint: BooleanToVisibilityHint.UseHidden, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlModeGlobal, v => v.MenuModeGlobal2.Visibility, conversionHint: BooleanToVisibilityHint.UseHidden, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlModeDirect, v => v.MenuModeDirect2.Visibility, conversionHint: BooleanToVisibilityHint.UseHidden, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlModeNothing, v => v.MenuModeNothing2.Visibility, conversionHint: BooleanToVisibilityHint.UseHidden, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ModeRuleCmd, v => v.MenuModeRule).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ModeGlobalCmd, v => v.MenuModeGlobal).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ModeDirectCmd, v => v.MenuModeDirect).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ModeNothingCmd, v => v.MenuModeNothing).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.AddProfileViaScanCmd, v => v.MenuAddProfileViaScan).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateCmd, v => v.MenuSubUpdate).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateViaProxyCmd, v => v.MenuSubUpdateViaProxy).DisposeWith(disposables);

            //this.BindCommand(ViewModel, vm => vm.ExitCmd, v => v.menuExit).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ReloadCmd, v => v.BtnReload).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.NotifyIcon, v => v.TbNotify.Icon).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.NotifyLeftClickCmd, v => v.TbNotify.LeftClickCommand).DisposeWith(disposables);
        });

        Title = $"{Utils.GetVersion()} - {(Utils.IsAdministrator() ? ResUI.RunAsAdmin : ResUI.NotRunAsAdmin)}";
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        ViewModel?.ShowHideWindow(false);
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        TbNotify.Dispose();
        ViewModel?.MyAppExit(false);
    }

    private void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        Utils.SaveLog("Current_SessionEnding");
        ViewModel?.MyAppExit(true);
    }
}