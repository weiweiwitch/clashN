using ClashN.ViewModels;
using ReactiveUI;
using Splat;
using System.Reactive.Disposables;
using System.Windows.Input;

namespace ClashN.Views
{
    /// <summary>
    /// Interaction logic for ProxiesView.xaml
    /// </summary>
    public partial class ProxiesView
    {
        public ProxiesView()
        {
            InitializeComponent();
            ViewModel = new ProxiesViewModel();
            Locator.CurrentMutable.RegisterLazySingleton(() => ViewModel, typeof(ProxiesViewModel));
            LstProxyDetails.PreviewMouseDoubleClick += lstProxyDetails_PreviewMouseDoubleClick;

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => vm.ProxyGroups, v => v.LstProxyGroups.ItemsSource).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedGroup, v => v.LstProxyGroups.SelectedItem).DisposeWith(disposables);

                this.OneWayBind(ViewModel, vm => vm.ProxyDetails, v => v.LstProxyDetails.ItemsSource).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedDetail, v => v.LstProxyDetails.SelectedItem).DisposeWith(disposables);

                this.BindCommand(ViewModel, vm => vm.ProxiesReloadCmd, v => v.MenuProxiesReload).DisposeWith(disposables);
                this.BindCommand(ViewModel, vm => vm.ProxiesDelayTestCmd, v => v.MenuProxiesDelayTest).DisposeWith(disposables);

                this.BindCommand(ViewModel, vm => vm.ProxiesDelayTestPartCmd, v => v.MenuProxiesDelayTestPart).DisposeWith(disposables);
                this.BindCommand(ViewModel, vm => vm.ProxiesSelectActivityCmd, v => v.MenuProxiesSelectActivity).DisposeWith(disposables);

                this.Bind(ViewModel, vm => vm.SystemProxySelected, v => v.CmbSystemProxy.SelectedIndex).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.RuleModeSelected, v => v.CmbRulemode.SelectedIndex).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SortingSelected, v => v.CmbSorting.SelectedIndex).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.AutoRefresh, v => v.TogAutoRefresh.IsChecked).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.EnableTun, v => v.TogEnableTun.IsChecked).DisposeWith(disposables);
            });
        }

        private void ProxiesView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    ViewModel?.ProxiesReload();
                    break;

                case Key.Enter:
                    ViewModel?.SetActiveProxy();
                    break;
            }
        }

        private void lstProxyDetails_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.SetActiveProxy();
        }
    }
}