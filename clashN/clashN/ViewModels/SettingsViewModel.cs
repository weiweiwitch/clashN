using ClashN.Handler;
using ClashN.Mode;
using ClashN.Resx;
using ClashN.Views;
using DynamicData;
using DynamicData.Binding;
using MaterialDesignColors;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Splat;
using System.IO;
using System.Reactive;
using System.Windows;
using ClashN.Tool;

namespace ClashN.ViewModels
{
    public class SettingsViewModel : ReactiveValidationObject
    {
        #region Core

        [Reactive] public int MixedPort { get; set; }

        [Reactive] public int SocksPort { get; set; }

        [Reactive] public int HttpPort { get; set; }

        [Reactive] public int APIPort { get; set; }

        [Reactive] public bool AllowLANConn { get; set; }

        [Reactive] public bool EnableIpv6 { get; set; }

        [Reactive] public string LogLevel { get; set; }

        [Reactive] public bool EnableMixinContent { get; set; }

        public ReactiveCommand<Unit, Unit> EditMixinContentCmd { get; }

        #endregion Core

        #region ClashN

        [Reactive] public bool AutoRun { get; set; }

        [Reactive] public bool EnableStatistics { get; set; }

        [Reactive] public bool EnableSecurityProtocolTls13 { get; set; }

        [Reactive] public int autoUpdateSubInterval { get; set; }

        [Reactive] public int autoDelayTestInterval { get; set; }

        [Reactive] public string SubConvertUrl { get; set; }

        [Reactive] public string currentFontFamily { get; set; }

        [Reactive] public bool AutoHideStartup { get; set; }

        public ReactiveCommand<Unit, Unit> SetLoopbackCmd { get; }
        public ReactiveCommand<Unit, Unit> SetGlobalHotkeyCmd { get; }

        #endregion ClashN

        #region System proxy

        [Reactive] public string systemProxyExceptions { get; set; }

        [Reactive] public string systemProxyAdvancedProtocol { get; set; }

        [Reactive] public int PacPort { get; set; }

        #endregion System proxy

        #region UI

        private IObservableCollection<Swatch> _swatches = new ObservableCollectionExtended<Swatch>();
        public IObservableCollection<Swatch> Swatches => _swatches;

        [Reactive] public Swatch SelectedSwatch { get; set; }

        [Reactive] public bool ColorModeDark { get; set; }

        [Reactive] public string CurrentLanguage { get; set; }

        [Reactive] public int CurrentFontSize { get; set; }

        #endregion UI

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public SettingsViewModel()
        {
            var config = LazyConfig.Instance.Config;

            //Core
            MixedPort = config.MixedPort;
            SocksPort = config.SocksPort;
            HttpPort = config.HttpPort;
            APIPort = config.ApiPort;
            AllowLANConn = config.AllowLANConn;
            EnableIpv6 = config.EnableIpv6;
            LogLevel = config.LogLevel;
            EnableMixinContent = config.EnableMixinContent;
            EditMixinContentCmd = ReactiveCommand.Create(() => { EditMixinContent(); }, this.IsValid());

            //ClashN
            AutoRun = config.AutoRun;
            EnableStatistics = config.EnableStatistics;
            EnableSecurityProtocolTls13 = config.EnableSecurityProtocolTls13;
            autoUpdateSubInterval = config.AutoUpdateSubInterval;
            autoDelayTestInterval = config.AutoDelayTestInterval;
            SubConvertUrl = config.ConstItem.SubConvertUrl;
            currentFontFamily = config.UiItem.CurrentFontFamily;
            AutoHideStartup = config.AutoHideStartup;

            SetLoopbackCmd =
                ReactiveCommand.Create(() => { Utils.ProcessStart(Utils.GetBinPath("EnableLoopback.exe")); },
                    this.IsValid());
            SetGlobalHotkeyCmd = ReactiveCommand.Create(() =>
            {
                GlobalHotkeySettingWindow dialog = new GlobalHotkeySettingWindow()
                {
                    Owner = App.Current.MainWindow
                };

                dialog.ShowDialog();
            }, this.IsValid());

            //System proxy
            systemProxyExceptions = config.SystemProxyExceptions;
            systemProxyAdvancedProtocol = config.SystemProxyAdvancedProtocol;
            PacPort = config.PacPort;

            //UI
            ColorModeDark = config.UiItem.ColorModeDark;
            _swatches.AddRange(new SwatchesProvider().Swatches);
            if (!string.IsNullOrEmpty(config.UiItem.ColorPrimaryName))
            {
                SelectedSwatch = _swatches.FirstOrDefault(t => t.Name == config.UiItem.ColorPrimaryName);
            }

            CurrentLanguage = Utils.RegReadValue(Global.MyRegPath, Global.MyRegKeyLanguage, Global.Languages[0]);
            CurrentFontSize = config.UiItem.CurrentFontSize;

            this.WhenAnyValue(
                    x => x.ColorModeDark,
                    y => y == true)
                .Subscribe(c =>
                {
                    if (config.UiItem.ColorModeDark != ColorModeDark)
                    {
                        config.UiItem.ColorModeDark = ColorModeDark;
                        Locator.Current.GetService<MainWindowViewModel>()?.ModifyTheme(ColorModeDark);
                        ConfigProc.SaveConfig();
                    }
                });

            this.WhenAnyValue(
                    x => x.SelectedSwatch,
                    y => y != null && !string.IsNullOrEmpty(y.Name))
                .Subscribe(c =>
                {
                    if (SelectedSwatch == null
                        || string.IsNullOrEmpty(SelectedSwatch.Name)
                        || SelectedSwatch.ExemplarHue == null
                        || SelectedSwatch.ExemplarHue?.Color == null)
                    {
                        return;
                    }

                    if (config.UiItem.ColorPrimaryName != SelectedSwatch?.Name)
                    {
                        config.UiItem.ColorPrimaryName = SelectedSwatch?.Name;
                        Locator.Current.GetService<MainWindowViewModel>()
                            ?.ChangePrimaryColor(SelectedSwatch.ExemplarHue.Color);
                        ConfigProc.SaveConfig();
                    }
                });

            this.WhenAnyValue(
                    x => x.CurrentLanguage,
                    y => y != null && !string.IsNullOrEmpty(y))
                .Subscribe(c =>
                {
                    if (!string.IsNullOrEmpty(CurrentLanguage))
                    {
                        Utils.RegWriteValue(Global.MyRegPath, Global.MyRegKeyLanguage, CurrentLanguage);
                        Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(CurrentLanguage);
                    }
                });

            this.WhenAnyValue(
                    x => x.CurrentFontSize,
                    y => y > 0)
                .Subscribe(c =>
                {
                    if (config.UiItem.ColorModeDark != ColorModeDark)
                    {
                        config.UiItem.ColorModeDark = ColorModeDark;
                        Locator.Current.GetService<MainWindowViewModel>()?.ModifyTheme(ColorModeDark);
                        ConfigProc.SaveConfig();
                    }
                });

            this.WhenAnyValue(
                    x => x.CurrentFontSize,
                    y => y > 0)
                .Subscribe(c =>
                {
                    if (CurrentFontSize >= Global.MinFontSize)
                    {
                        config.UiItem.CurrentFontSize = CurrentFontSize;
                        double size = (long)CurrentFontSize;
                        Application.Current.Resources["StdFontSize1"] = size;
                        Application.Current.Resources["StdFontSize2"] = size + 1;
                        Application.Current.Resources["StdFontSize3"] = size + 2;
                        Application.Current.Resources["StdFontSize4"] = size + 3;

                        ConfigProc.SaveConfig();
                    }
                });

            //CMD
            SaveCommand = ReactiveCommand.Create(() => { SaveConfig(); }, this.IsValid());
        }

        private void SaveConfig()
        {
            //Core
            var config = LazyConfig.Instance.Config;
            config.MixedPort = MixedPort;
            config.SocksPort = SocksPort;
            config.HttpPort = HttpPort;
            config.ApiPort = APIPort;
            config.AllowLANConn = AllowLANConn;
            config.EnableIpv6 = EnableIpv6;
            config.LogLevel = LogLevel;
            config.EnableMixinContent = EnableMixinContent;

            //ClashN
            Utils.SetAutoRun(AutoRun);
            config.AutoRun = AutoRun;
            config.EnableStatistics = EnableStatistics;
            config.EnableSecurityProtocolTls13 = EnableSecurityProtocolTls13;
            config.AutoUpdateSubInterval = autoUpdateSubInterval;
            config.AutoDelayTestInterval = autoDelayTestInterval;
            config.ConstItem.SubConvertUrl = SubConvertUrl;
            config.UiItem.CurrentFontFamily = currentFontFamily;
            config.AutoHideStartup = AutoHideStartup;

            //System proxy
            config.SystemProxyExceptions = systemProxyExceptions;
            config.SystemProxyAdvancedProtocol = systemProxyAdvancedProtocol;
            config.PacPort = PacPort;

            if (ConfigProc.SaveConfig() == 0)
            {
                NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
                Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
            }
            else
            {
                NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
            }
        }

        private void EditMixinContent()
        {
            var address = Utils.GetConfigPath(Global.MixinConfigFileName);
            if (!File.Exists(address))
            {
                var contents = Utils.GetEmbedText(Global.SampleMixin);
                if (!string.IsNullOrEmpty(contents))
                {
                    File.WriteAllText(address, contents);
                }
            }

            if (File.Exists(address))
            {
                Utils.ProcessStart(address);
            }
            else
            {
                NoticeHandler.Instance.Enqueue(ResUI.FailedReadConfiguration);
            }
        }
    }
}