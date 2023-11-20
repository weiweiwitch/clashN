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
    private IObservableCollection<MetaLogModel> _metaLogItems = new ObservableCollectionExtended<MetaLogModel>();

    public IObservableCollection<MetaLogModel> MetaLogItems => _metaLogItems;
    
    [Reactive]
    public int SortingSelected { get; set; }

    [Reactive]
    public bool ScrollToEnd { get; set; }
        
    [Reactive]
    public bool AutoRefresh { get; set; }

    [Reactive]
    public string MsgFilter { get; set; }

    [Reactive]
    public int LineCount { get; set; }

    public LogsViewModel()
    {
        ScrollToEnd = true;
        AutoRefresh = true;
        MsgFilter = string.Empty;
        LineCount = 1000;
    }
}