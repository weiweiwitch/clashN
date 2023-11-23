using ClashN.Mode;
using ClashN.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows.Input;

namespace ClashN.Views;

/// <summary>
/// ProfileEditWindow.xaml 的交互逻辑
/// </summary>
public partial class ProfileEditWindow
{
    public ProfileEditWindow(ProfileItem profileItem)
    {
        InitializeComponent();
        ViewModel = new ProfileEditViewModel(profileItem, this);
        Global.CoreTypes.ForEach(it =>
        {
            CmbCoreType.Items.Add(it);
        });
        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.TxtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Url, v => v.TxtUrl.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Address, v => v.TxtAddress.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.UserAgent, v => v.TxtUserAgent.Text).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.CoreType, v => v.CmbCoreType.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Enabled, v => v.TogEnabled.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.EnableConvert, v => v.TogEnableConvert.IsChecked).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.BrowseProfileCmd, v => v.BtnBrowse).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.EditProfileCmd, v => v.BtnEdit).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SaveProfileCmd, v => v.BtnSave).DisposeWith(disposables);
        });
    }

    private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        this.Close();
    }

    private void ProfileEditWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.Close();
        }
    }
}