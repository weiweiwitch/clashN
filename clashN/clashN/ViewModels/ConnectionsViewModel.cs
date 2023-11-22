using ClashN.Handler;
using ClashN.Mode;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using ClashN.Tool;

namespace ClashN.ViewModels;

public class ConnectionsViewModel : ReactiveObject
{
    private IObservableCollection<ConnectionModel> _connectionItems =
        new ObservableCollectionExtended<ConnectionModel>();

    public IObservableCollection<ConnectionModel> ConnectionItems => _connectionItems;

    [Reactive] public ConnectionModel SelectedSource { get; set; }

    public ReactiveCommand<Unit, Unit> ConnectionCloseCmd { get; }
    public ReactiveCommand<Unit, Unit> ConnectionCloseAllCmd { get; }

    [Reactive] public int SortingSelected { get; set; }

    [Reactive] public bool AutoRefresh { get; set; }

    private const int AutoRefreshInterval = 10;

    public ConnectionsViewModel()
    {
        var config = LazyConfig.Instance.Config;
        SortingSelected = config.UiItem.ConnectionsSorting;
        AutoRefresh = config.UiItem.ConnectionsAutoRefresh;

        var canEditRemove = this.WhenAnyValue(
            x => x.SelectedSource,
            selectedSource => selectedSource != null && !string.IsNullOrEmpty(selectedSource.id));

        this.WhenAnyValue(
                x => x.SortingSelected,
                y => y >= 0)
            .Subscribe(c => DoSortingSelected(c));

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
                Utils.SaveLog($"ConnectionsViewModel:Init - Try GetClashConnections after delay {AutoRefreshInterval}");
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
            ConnectionModel model = new()
            {
                id = item.Id,
                network = item.metadata.Network,
                type = item.metadata.Type,
                host =
                    $"{(string.IsNullOrEmpty(item.metadata.Host) ? item.metadata.DestinationIP : item.metadata.Host)}:{item.metadata.DestinationPort}"
            };

            var sp = (dtNow - item.start);
            model.time = sp.TotalSeconds < 0 ? 1 : sp.TotalSeconds;
            model.upload = item.upload;
            model.download = item.download;
            model.uploadTraffic = $"{Utils.HumanFy(item.upload)}";
            model.downloadTraffic = $"{Utils.HumanFy(item.download)}";
            model.elapsed = sp.ToString(@"hh\:mm\:ss");
            model.chain = item.Chains.Count > 0 ? item.Chains[0] : String.Empty;

            lstModel.Add(model);
        }

        if (lstModel.Count <= 0)
        {
            return;
        }

        //sort
        lstModel = SortingSelected switch
        {
            0 => lstModel.OrderBy(t => t.upload / t.time).ToList(),
            1 => lstModel.OrderBy(t => t.download / t.time).ToList(),
            2 => lstModel.OrderBy(t => t.upload).ToList(),
            3 => lstModel.OrderBy(t => t.download).ToList(),
            4 => lstModel.OrderBy(t => t.time).ToList(),
            5 => lstModel.OrderBy(t => t.host).ToList(),
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

            id = item.id;
        }
        else
        {
            _connectionItems.Clear();
        }

        MainFormHandler.Instance.ClashConnectionClose(id);

        GetClashConnections();
    }
}