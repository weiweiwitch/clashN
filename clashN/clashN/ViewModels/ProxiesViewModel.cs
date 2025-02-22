using ClashN.Handler;
using ClashN.Mode;
using ClashN.Resx;
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
using static ClashN.Mode.ClashProviders;
using static ClashN.Mode.ClashProxies;

namespace ClashN.ViewModels;

public class ProxiesViewModel : ReactiveObject
{
    private const int DelayTimeout = 99999999;

    private Dictionary<string, ProxiesItem> _proxies;
    private Dictionary<string, ProvidersItem> _providers;

    private IObservableCollection<ProxyModel> _proxyGroups = new ObservableCollectionExtended<ProxyModel>();
    private IObservableCollection<ProxyModel> _proxyDetails = new ObservableCollectionExtended<ProxyModel>();

    public IObservableCollection<ProxyModel> ProxyGroups => _proxyGroups;
    public IObservableCollection<ProxyModel> ProxyDetails => _proxyDetails;

    [Reactive] public ProxyModel SelectedGroup { get; set; }

    [Reactive] public ProxyModel SelectedDetail { get; set; }

    public ReactiveCommand<Unit, Unit> ProxiesReloadCmd { get; }
    public ReactiveCommand<Unit, Unit> ProxiesDelayTestCmd { get; }
    public ReactiveCommand<Unit, Unit> ProxiesDelayTestPartCmd { get; }
    public ReactiveCommand<Unit, Unit> ProxiesSelectActivityCmd { get; }

    [Reactive] public int SystemProxySelected { get; set; }

    [Reactive] public int RuleModeSelected { get; set; }

    [Reactive] public int SortingSelected { get; set; }

    [Reactive] public bool AutoRefresh { get; set; }

    [Reactive] public bool EnableTun { get; set; }

    public ProxiesViewModel()
    {
        var config = LazyConfig.Instance.Config;

        SelectedGroup = new ProxyModel();
        SelectedDetail = new ProxyModel();
        AutoRefresh = config.UiItem.ProxiesAutoRefresh;
        EnableTun = config.EnableTun;
        SortingSelected = config.UiItem.ProxiesSorting;

        //GetClashProxies(true);
        this.WhenAnyValue(
                x => x.SelectedGroup,
                y => y != null && !string.IsNullOrEmpty(y.name))
            .Subscribe(c => RefreshProxyDetails(c));

        this.WhenAnyValue(
                x => x.SystemProxySelected,
                y => y >= 0)
            .Subscribe(c => DoSystemProxySelected(c));

        this.WhenAnyValue(
                x => x.RuleModeSelected,
                y => y >= 0)
            .Subscribe(c => DoRuleModeSelected(c));

        this.WhenAnyValue(
                x => x.SortingSelected,
                y => y >= 0)
            .Subscribe(c => DoSortingSelected(c));

        this.WhenAnyValue(
                x => x.EnableTun,
                y => y == true)
            .Subscribe(c => DoEnableTun(c));

        this.WhenAnyValue(
                x => x.AutoRefresh,
                y => y == true)
            .Subscribe(c => { config.UiItem.ProxiesAutoRefresh = AutoRefresh; });

        ProxiesReloadCmd = ReactiveCommand.Create(() => { ProxiesReload(); });
        ProxiesDelayTestCmd = ReactiveCommand.Create(() => { ProxiesDelayTest(true); });

        ProxiesDelayTestPartCmd = ReactiveCommand.Create(() => { ProxiesDelayTest(false); });
        ProxiesSelectActivityCmd = ReactiveCommand.Create(() => { SetActiveProxy(); });

        ReloadSystemProxySelected();
        ReloadRuleModeSelected();

        DelayTestTask();
    }

    private void DoSystemProxySelected(bool c)
    {
        if (!c)
        {
            return;
        }

        if (LazyConfig.Instance.Config.SysProxyType == (SysProxyType)SystemProxySelected)
        {
            return;
        }

        Locator.Current.GetService<MainWindowViewModel>()?.SetListenerType((SysProxyType)SystemProxySelected);
    }

    private void DoRuleModeSelected(bool c)
    {
        if (!c)
        {
            return;
        }

        if (LazyConfig.Instance.Config.RuleMode == (ERuleMode)RuleModeSelected)
        {
            return;
        }

        Locator.Current.GetService<MainWindowViewModel>()?.SetRuleModeCheck((ERuleMode)RuleModeSelected);
    }

    private void DoSortingSelected(bool c)
    {
        if (!c)
        {
            return;
        }

        if (SortingSelected != LazyConfig.Instance.Config.UiItem.ProxiesSorting)
        {
            LazyConfig.Instance.Config.UiItem.ProxiesSorting = SortingSelected;
        }

        RefreshProxyDetails(c);
    }

    public void ProxiesReload()
    {
        Utils.SaveLogDebug($"ProxiesViewModel:ProxiesReload - Start");

        GetClashProxies(true);
    }

    public void ProxiesClear()
    {
        _proxies = null;
        _providers = null;

        LazyConfig.Instance.SetProxies(_proxies);

        _proxyGroups.Clear();
        _proxyDetails.Clear();
    }

    public void ReloadSystemProxySelected()
    {
        SystemProxySelected = (int)LazyConfig.Instance.Config.SysProxyType;
    }

    public void ReloadRuleModeSelected()
    {
        RuleModeSelected = (int)LazyConfig.Instance.Config.RuleMode;
    }

    private void DoEnableTun(bool c)
    {
        if (LazyConfig.Instance.Config.EnableTun != EnableTun)
        {
            LazyConfig.Instance.Config.EnableTun = EnableTun;
            TunModeSwitch();
        }
    }

    private static void TunModeSwitch()
    {
        Global.ReloadCore = true;

        Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
    }

    #region proxy function

    private void GetClashProxies(bool refreshUI)
    {
        MainFormHandler.Instance.GetClashProxies((it, it2) =>
        {
            NoticeHandler.SendMessage4ClashN("Refresh Clash Proxies");

            Application.Current.Dispatcher.Invoke(() =>
            {
                _proxies = it.proxies;
                _providers = it2.providers;

                LazyConfig.Instance.SetProxies(_proxies);
                if (_proxies == null)
                {
                    return;
                }

                if (refreshUI)
                {
                    RefreshProxyGroups();
                }
            });
        });
    }

    private void RefreshProxyGroups()
    {
        var selectedName = SelectedGroup?.name;
        _proxyGroups.Clear();

        var proxyGroups = MainFormHandler.GetClashProxyGroups();
        if (proxyGroups != null && proxyGroups.Count > 0)
        {
            foreach (var it in proxyGroups)
            {
                if (string.IsNullOrEmpty(it.name) || !_proxies.ContainsKey(it.name))
                {
                    continue;
                }

                var item = _proxies[it.name];
                if (!Global.AllowSelectType.Contains(item.type.ToLower()))
                {
                    continue;
                }

                _proxyGroups.Add(new ProxyModel()
                {
                    now = item.now,
                    name = item.name,
                    type = item.type
                });
            }
        }

        //from api
        foreach (var kv in _proxies)
        {
            if (!Global.AllowSelectType.Contains(kv.Value.type.ToLower()))
            {
                continue;
            }

            var item = _proxyGroups.FirstOrDefault(t => t.name == kv.Key);
            if (item != null && !string.IsNullOrEmpty(item.name))
            {
                continue;
            }

            _proxyGroups.Add(new ProxyModel()
            {
                now = kv.Value.now,
                name = kv.Key,
                type = kv.Value.type
            });
        }

        if (_proxyGroups != null && _proxyGroups.Count > 0)
        {
            if (selectedName != null && _proxyGroups.Any(t => t.name == selectedName))
            {
                SelectedGroup = _proxyGroups.FirstOrDefault(t => t.name == selectedName);
            }
            else
            {
                SelectedGroup = _proxyGroups[0];
            }
        }
        else
        {
            SelectedGroup = new();
        }
    }

    private void RefreshProxyDetails(bool c)
    {
        _proxyDetails.Clear();
        if (!c)
        {
            return;
        }

        var name = SelectedGroup?.name;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (_proxies == null)
        {
            return;
        }

        _proxies.TryGetValue(name, out ProxiesItem proxy);
        if (proxy == null || proxy.all == null)
        {
            return;
        }

        var lstDetails = new List<ProxyModel>();
        foreach (var item in proxy.all)
        {
            var isActive = item == proxy.now;

            var proxy2 = TryGetProxy(item);
            if (proxy2 == null)
            {
                continue;
            }

            int delay = -1;
            if (proxy2.history.Count > 0)
            {
                delay = proxy2.history[proxy2.history.Count - 1].delay;
            }

            lstDetails.Add(new ProxyModel()
            {
                isActive = isActive,
                name = item,
                type = proxy2.type,
                delay = delay <= 0 ? DelayTimeout : delay,
                delayName = delay <= 0 ? string.Empty : $"{delay}ms",
            });
        }

        //sort
        switch (SortingSelected)
        {
            case 0:
                lstDetails = lstDetails.OrderBy(t => t.delay).ToList();
                break;

            case 1:
                lstDetails = lstDetails.OrderBy(t => t.name).ToList();
                break;

            default:
                break;
        }

        _proxyDetails.AddRange(lstDetails);
    }

    private ProxiesItem TryGetProxy(string name)
    {
        _proxies.TryGetValue(name, out ProxiesItem proxy2);
        if (proxy2 != null)
        {
            return proxy2;
        }

        //from providers
        if (_providers != null)
        {
            foreach (KeyValuePair<string, ProvidersItem> kv in _providers)
            {
                if (Global.ProxyVehicleType.Contains(kv.Value.vehicleType.ToLower()))
                {
                    var proxy3 = kv.Value.proxies.FirstOrDefault(t => t.name == name);
                    if (proxy3 != null)
                    {
                        return proxy3;
                    }
                }
            }
        }

        return null;
    }

    public void SetActiveProxy()
    {
        if (SelectedGroup == null || string.IsNullOrEmpty(SelectedGroup.name))
        {
            return;
        }

        if (SelectedDetail == null || string.IsNullOrEmpty(SelectedDetail.name))
        {
            return;
        }

        var name = SelectedGroup.name;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var nameNode = SelectedDetail.name;
        if (string.IsNullOrEmpty(nameNode))
        {
            return;
        }

        var selectedProxy = TryGetProxy(name);
        if (selectedProxy == null || selectedProxy.type != "Selector")
        {
            NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
            return;
        }

        MainFormHandler.Instance.ClashSetActiveProxy(name, nameNode);

        selectedProxy.now = nameNode;
        var group = _proxyGroups.Where(it => it.name == SelectedGroup.name).FirstOrDefault();
        if (group != null)
        {
            group.now = nameNode;
            var group2 = Utils.DeepCopy(group);
            _proxyGroups.Replace(group, group2);

            SelectedGroup = group2;
        }

        NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
    }

    public void ProxiesDelayTest(bool blAll)
    {
        NoticeHandler.SendMessage4ClashN("ProxiesViewModel:ProxiesDelayTest - Clash Proxies Latency Test");

        MainFormHandler.Instance.ClashProxiesDelayTest(blAll, _proxyDetails.ToList(), (item, result) =>
        {
            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var detail = _proxyDetails.FirstOrDefault(it => it.name == item.name);
                if (detail != null)
                {
                    var dicResult = Utils.FromJson<Dictionary<string, object>>(result);
                    if (dicResult != null && dicResult.ContainsKey("delay"))
                    {
                        detail.delay = Convert.ToInt32(dicResult["delay"]);
                        detail.delayName = $"{dicResult["delay"]}ms";
                    }
                    else if (dicResult != null && dicResult.ContainsKey("message"))
                    {
                        detail.delay = DelayTimeout;
                        detail.delayName = $"{dicResult["message"]}";
                    }
                    else
                    {
                        detail.delay = DelayTimeout;
                        detail.delayName = string.Empty;
                    }

                    _proxyDetails.Replace(detail, Utils.DeepCopy(detail));
                }
            });
        });
    }

    #endregion proxy function

    #region task

    private void DelayTestTask()
    {
        var autoDelayTestTime = DateTime.Now;

        Observable.Interval(TimeSpan.FromSeconds(60))
            .Subscribe(x =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!AutoRefresh || !Global.ShowInTaskbar)
                    {
                        return;
                    }

                    var dtNow = DateTime.Now;

                    var autoDelayTestInterval = LazyConfig.Instance.Config.AutoDelayTestInterval;
                    if (autoDelayTestInterval > 0)
                    {
                        if ((dtNow - autoDelayTestTime).Minutes % autoDelayTestInterval == 0)
                        {
                            ProxiesDelayTest(true);

                            autoDelayTestTime = dtNow;
                        }
                    }
                });
            });
    }

    #endregion task
}