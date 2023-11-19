using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ClashN.ViewModels
{
    public class LogsViewModel : ReactiveObject
    {
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
}