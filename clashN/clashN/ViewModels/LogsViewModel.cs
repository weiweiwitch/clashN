using System.ComponentModel;
using System.Windows.Data;
using ClashN.Mode;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ClashN.ViewModels;

public enum LogType
{
    Log4Clash,
    Log4ClashN
}

public class LogsViewModel : ReactiveObject
{
    private readonly IObservableCollection<MetaLogModel> _metaLogItems = new ObservableCollectionExtended<MetaLogModel>();

    public ICollectionView MetaLogItems => CollectionViewSource.GetDefaultView(_metaLogItems);

    [Reactive] public bool ScrollToEnd { get; set; }
    [Reactive] public bool AutoRefresh { get; set; }
    [Reactive] public string MsgFilter { get; set; }
    [Reactive] public int LineCount { get; set; }

    public LogsViewModel()
    {
        ScrollToEnd = true;
        AutoRefresh = true;
        MsgFilter = string.Empty;
        LineCount = 1000;
        
        this.WhenAnyValue(
                x => x.MsgFilter)
            .Subscribe(_ =>
            {
                if (!string.IsNullOrEmpty(MsgFilter))
                {
                    MetaLogItems.Filter = item => (item as MetaLogModel).Msg.Contains(MsgFilter);
                }
                else
                {
                    MetaLogItems.Filter = _ => true;
                }
            });
        
    }
    public void AddLog(MetaLogModel metaLog)
    {
        _metaLogItems.Add(metaLog);
    }

    public void RemoveTop()
    {
        _metaLogItems.RemoveAt(0);
    }

    public int MetaLogCount()
    {
        return _metaLogItems.Count;
    }

    public void MetaLogClear()
    {
        _metaLogItems.Clear();
    }
    
}