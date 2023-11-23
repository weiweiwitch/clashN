using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using ClashN.Handler;
using ClashN.Mode;
using ClashN.Tool;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ClashN.ViewModels;

public class ConnectionsViewModel : ReactiveObject
{
    private readonly IObservableCollection<ConnectionModel> _connectionItems =
        new ObservableCollectionExtended<ConnectionModel>();

    public ICollectionView ConnectionItems => CollectionViewSource.GetDefaultView(_connectionItems);

    [Reactive] public int ConnectionItemsCount { get; set; }
    [Reactive] public int SortingSelected { get; set; }
    [Reactive] public bool AutoRefresh { get; set; }

    [Reactive] public ConnectionModel? SelectedSource { get; set; }

    public ReactiveCommand<Unit, Unit> ConnectionCloseCmd { get; }
    public ReactiveCommand<Unit, Unit> ConnectionCloseAllCmd { get; }


    private const int AutoRefreshInterval = 1;

    public ConnectionsViewModel()
    {
        var config = LazyConfig.Instance.Config;
        ConnectionItemsCount = _connectionItems.Count;
        SortingSelected = config.UiItem.ConnectionsSorting;
        AutoRefresh = config.UiItem.ConnectionsAutoRefresh;
        
        var canEditRemove = this.WhenAnyValue(
            x => x.SelectedSource,
            selectedSource => selectedSource != null && !string.IsNullOrEmpty(selectedSource.Id));

        this.WhenAnyValue(
                x => x._connectionItems.Count)
            .Subscribe(_ => { ConnectionItemsCount = _connectionItems.Count; });

        this.WhenAnyValue(
                x => x.SortingSelected,
                y => y >= 0)
            .Subscribe(DoSortingSelected);

        this.WhenAnyValue(
                x => x.AutoRefresh,
                y => y == true)
            .Subscribe(c => { config.UiItem.ConnectionsAutoRefresh = AutoRefresh; });

        ConnectionCloseCmd = ReactiveCommand.Create(() => { ClashConnectionClose(false); }, canEditRemove);

        ConnectionCloseAllCmd = ReactiveCommand.Create(() => { ClashConnectionClose(true); });

        Init();
    }

    private void DoSortingSelected(bool c)
    {
        if (!c)
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        if (SortingSelected != config.UiItem.ConnectionsSorting)
        {
            config.UiItem.ConnectionsSorting = SortingSelected;
        }

        GetClashConnections();
    }

    private void Init()
    {
        Utils.SaveLog("ConnectionsViewModel:Init - Start");

        Observable.Interval(TimeSpan.FromSeconds(AutoRefreshInterval))
            .Subscribe(x =>
            {
                // Utils.SaveLogDebug($"ConnectionsViewModel:Init - Try GetClashConnections after delay {AutoRefreshInterval}");
                if (AutoRefresh && Global.ShowInTaskbar)
                {
                    GetClashConnections();
                }
            });
    }

    private void GetClashConnections()
    {
        MainFormHandler.Instance.GetClashConnections(it =>
        {
            if (it == null)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() => { RefreshConnections(it.Connections); });
        });
    }

    private void RefreshConnections(List<ConnectionItem> connections)
    {
        _connectionItems.Clear();

        var dtNow = DateTime.Now;
        var lstModel = new List<ConnectionModel>();
        foreach (var item in connections)
        {
            var sp = dtNow - item.start;

            ConnectionModel model = new()
            {
                Id = item.Id,
                Network = item.metadata.Network,
                Type = item.metadata.Type,
                Host =
                    $"{(string.IsNullOrEmpty(item.metadata.Host) ? item.metadata.DestinationIP : item.metadata.Host)}:{item.metadata.DestinationPort}",
                Time = sp.TotalSeconds < 0 ? 1 : sp.TotalSeconds,
                Upload = item.upload,
                Download = item.download,
                UploadTraffic = $"{Utils.HumanFy(item.upload)}",
                DownloadTraffic = $"{Utils.HumanFy(item.download)}",
                Elapsed = sp.ToString(@"hh\:mm\:ss"),
                Chain = item.Chains.Count > 0 ? item.Chains[0] : string.Empty
            };

            lstModel.Add(model);
        }

        if (lstModel.Count <= 0)
        {
            return;
        }

        //sort
        lstModel = SortingSelected switch
        {
            0 => lstModel.OrderBy(t => t.Upload / t.Time).ToList(),
            1 => lstModel.OrderBy(t => t.Download / t.Time).ToList(),
            2 => lstModel.OrderBy(t => t.Upload).ToList(),
            3 => lstModel.OrderBy(t => t.Download).ToList(),
            4 => lstModel.OrderBy(t => t.Time).ToList(),
            5 => lstModel.OrderBy(t => t.Host).ToList(),
            _ => lstModel
        };

        _connectionItems.AddRange(lstModel);
    }

    public void ClashConnectionClose(bool all)
    {
        var id = string.Empty;
        if (!all)
        {
            var item = SelectedSource;
            if (item is null)
            {
                return;
            }

            id = item.Id;
        }
        else
        {
            _connectionItems.Clear();
        }

        MainFormHandler.Instance.ClashConnectionClose(id);

        GetClashConnections();
    }
}