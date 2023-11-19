using ClashN.Mode;
using ClashN.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows.Input;

namespace ClashN.Views
{
    /// <summary>
    /// PorfileEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PorfileEditWindow
    {
        public PorfileEditWindow(ProfileItem profileItem)
        {
            InitializeComponent();
            ViewModel = new ProfileEditViewModel(profileItem, this);
            Global.coreTypes.ForEach(it =>
            {
                CmbCoreType.Items.Add(it);
            });
            this.WhenActivated(disposables =>
            {
                this.Bind(ViewModel, vm => vm.SelectedSource.remarks, v => v.TxtRemarks.Text).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedSource.url, v => v.TxtUrl.Text).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedSource.address, v => v.TxtAddress.Text).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedSource.userAgent, v => v.TxtUserAgent.Text).DisposeWith(disposables);

                this.Bind(ViewModel, vm => vm.CoreType, v => v.CmbCoreType.Text).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedSource.enabled, v => v.TogEnabled.IsChecked).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedSource.enableConvert, v => v.TogEnableConvert.IsChecked).DisposeWith(disposables);

                this.BindCommand(ViewModel, vm => vm.BrowseProfileCmd, v => v.BtnBrowse).DisposeWith(disposables);
                this.BindCommand(ViewModel, vm => vm.EditProfileCmd, v => v.BtnEdit).DisposeWith(disposables);
                this.BindCommand(ViewModel, vm => vm.SaveProfileCmd, v => v.BtnSave).DisposeWith(disposables);
            });
        }

        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void PorfileEditWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}