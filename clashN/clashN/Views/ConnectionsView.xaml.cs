using System.ComponentModel;
using ClashN.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows.Controls;
using ClashN.Tool;

namespace ClashN.Views;

/// <summary>
/// Interaction logic for ConnectionsView.xaml
/// </summary>
public partial class ConnectionsView
{
    public ConnectionsView()
    {
        InitializeComponent();

        ViewModel = new ConnectionsViewModel();
        
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ConnectionItems, v => v.LstConnections.ItemsSource)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedSource, v => v.LstConnections.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.ConnectionItemsCount, v => v.ChipCount.Content)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.ConnectionCloseCmd, v => v.MenuConnectionClose)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ConnectionCloseAllCmd, v => v.MenuConnectionCloseAll)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SortingSelected, v => v.CmbSorting.SelectedIndex).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ConnectionCloseAllCmd, v => v.BtnConnectionCloseAll)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AutoRefresh, v => v.TogAutoRefresh.IsChecked).DisposeWith(disposables);
        });
    }

    private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        ViewModel?.ClashConnectionClose(false);
    }

    private void LstConnections_OnSorting(object sender, DataGridSortingEventArgs e)
    {
        ViewModel.ConnectionItems.SortDescriptions.Clear();
        ViewModel.ConnectionItems.SortDescriptions.Add(new SortDescription(e.Column.SortMemberPath, ListSortDirection.Ascending));
        ViewModel.ConnectionItems.Refresh();
    }
}