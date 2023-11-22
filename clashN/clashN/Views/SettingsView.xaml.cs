using ClashN.Handler;
using ClashN.Mode;
using ClashN.ViewModels;
using ReactiveUI;
using System.Globalization;
using System.IO;
using System.Reactive.Disposables;
using System.Windows.Media;
using ClashN.Tool;

namespace ClashN.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView
{
    public SettingsView()
    {
        InitializeComponent();
        
        ViewModel = new SettingsViewModel();

        Global.SubConvertUrls.ForEach(it => { CmbSubConvertUrl.Items.Add(it); });
        Global.Languages.ForEach(it => { CmbCurrentLanguage.Items.Add(it); });
        Global.IEProxyProtocols.ForEach(it => { CmbSystemProxyAdvancedProtocol.Items.Add(it); });
        Global.LogLevel.ForEach(it => { CmbLogLevel.Items.Add(it); });

        for (var i = Global.MinFontSize; i <= Global.MinFontSize + 8; i++)
        {
            CmbCurrentFontSize.Items.Add(i.ToString());
        }

        //fill fonts
        try
        {
            var dir = new DirectoryInfo(Utils.GetFontsPath());
            var files = dir.GetFiles("*.ttf");
            const string culture = "zh-cn";
            const string culture2 = "en-us";
            foreach (var it in files)
            {
                var families = Fonts.GetFontFamilies(Utils.GetFontsPath(it.Name));
                foreach (var family in families)
                {
                    var typefaces = family.GetTypefaces();
                    foreach (var typeface in typefaces)
                    {
                        typeface.TryGetGlyphTypeface(out GlyphTypeface glyph);
                        var fontFamily = glyph.Win32FamilyNames[new CultureInfo(culture)];
                        if (string.IsNullOrEmpty(fontFamily))
                        {
                            fontFamily = glyph.Win32FamilyNames[new CultureInfo(culture2)];
                            if (string.IsNullOrEmpty(fontFamily))
                            {
                                continue;
                            }
                        }

                        CmbCurrentFontFamily.Items.Add(fontFamily);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog("fill fonts error", ex);
        }

        CmbCurrentFontFamily.Items.Add(string.Empty);

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.MixedPort, v => v.TxtMixedPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SocksPort, v => v.TxtSocksPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.HttpPort, v => v.TxtHttpPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.APIPort, v => v.TxtApiPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AllowLANConn, v => v.TogAllowLanConn.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableIpv6, v => v.TogEnableIpv6.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.LogLevel, v => v.CmbLogLevel.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableMixinContent, v => v.TogEnableMixinContent.IsChecked)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.EditMixinContentCmd, v => v.BtnEditMixinContent)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.Swatches, v => v.CmbSwatches.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSwatch, v => v.CmbSwatches.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.ColorModeDark, v => v.TogDarkMode.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CurrentLanguage, v => v.CmbCurrentLanguage.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CurrentFontSize, v => v.CmbCurrentFontSize.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AutoRun, v => v.TogAutoRun.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableStatistics, v => v.TogEnableStatistics.IsChecked)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableSecurityProtocolTls13, v => v.TogEnableSecurityProtocolTls13.IsChecked)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AutoHideStartup, v => v.TogAutoHideStartup.IsChecked)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.autoUpdateSubInterval, v => v.TxtAutoUpdateSubInterval.Text)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.autoDelayTestInterval, v => v.TxtAutoDelayTestInterval.Text)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SubConvertUrl, v => v.CmbSubConvertUrl.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.currentFontFamily, v => v.CmbCurrentFontFamily.Text).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SetLoopbackCmd, v => v.BtnSetLoopback).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SetGlobalHotkeyCmd, v => v.BtnSetGlobalHotkey)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.systemProxyExceptions, v => v.TxtSystemProxyExceptions.Text)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.systemProxyAdvancedProtocol, v => v.CmbSystemProxyAdvancedProtocol.Text)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PacPort, v => v.TxtPacPort.Text).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SaveCommand, v => v.BtnSave).DisposeWith(disposables);
        });
    }
}