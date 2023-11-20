using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Threading;
using ClashN.Mode;
using ClashN.ViewModels;
using ReactiveUI;

namespace ClashN.Views;

/// <summary>
/// Interaction logic for LogsView.xaml
/// </summary>
public partial class LogsView
{
    public LogsView()
    {
        InitializeComponent();

        ViewModel = new LogsViewModel();

        MessageBus.Current.Listen<string>(LogType.Log4Clash.ToString()).Subscribe(DelegateAppendText4Clash);
        MessageBus.Current.Listen<string>(LogType.Log4ClashN.ToString()).Subscribe(DelegateAppendText4ClashN);

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.MetaLogItems, v => v.ListMetaLogs.ItemsSource).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.MsgFilter, v => v.TxtFilter.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.ScrollToEnd, v => v.TogScrollToEnd.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AutoRefresh, v => v.TogAutoRefresh.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.LineCount, v => v.CmbLineCount.Text).DisposeWith(disposables);
        });
    }

    private void DelegateAppendText4Clash(string msg)
    {
        Dispatcher.BeginInvoke(new Action<string>(logMsg => { ShowMsg(LogType.Log4Clash, logMsg); }),
            DispatcherPriority.Send, msg);
    }

    private void DelegateAppendText4ClashN(string msg)
    {
        Dispatcher.BeginInvoke(new Action<string>(logMsg => { ShowMsg(LogType.Log4ClashN, logMsg); }),
            DispatcherPriority.Send, msg);
    }

    private void ShowMsg(LogType logType, string msg)
    {
        if (ViewModel?.AutoRefresh == false)
        {
            return;
        }

        if (logType == LogType.Log4ClashN)
        {
            var compLog = TxtMsg4ClashN;
            if (compLog.LineCount > ViewModel?.LineCount)
            {
                Dispatcher.Invoke((Action)(() => { TxtMsg4ClashN.Clear(); }));
            }

            compLog.AppendText(msg);

            if (!msg.EndsWith(Environment.NewLine))
            {
                compLog.AppendText(Environment.NewLine);
            }

            if (ViewModel?.ScrollToEnd == true)
            {
                compLog.ScrollToEnd();
            }
        }

        if (logType == LogType.Log4Clash)
        {
            var vm = ViewModel;
            if (vm != null)
            {
                var msgFilter = vm.MsgFilter;
                if (msgFilter != vm.OldMsgFilterStr)
                {
                    // update
                    vm.OldMsgFilterStr = msgFilter;

                    if (!string.IsNullOrEmpty(msgFilter))
                    {
                        vm.MetaLogItems.Filter = item => (item as MetaLogModel).Msg.Contains(msgFilter);

                        // if (!Regex.IsMatch(msg, msgFilter))
                        // {
                        //     return;
                        // }
                    }
                    else
                    {
                        vm.MetaLogItems.Filter = _ => true;
                    }
                }
            }
            
            var metaLogInfos = msg.Split(" ", 3);
            if (metaLogInfos.Length >= 3)
            {
                var metaLog = new MetaLogModel
                {
                    Time = metaLogInfos[0],
                    LogLevel = metaLogInfos[1],
                    Msg = metaLogInfos[2],
                };
                ViewModel?.AddLog(metaLog);
            }
            else
            {
                TxtMsg4ClashN.AppendText($"Error: Can't split Clash Meta Log: {msg}{Environment.NewLine}");
            }

            var count = ViewModel?.MetaLogCount();
            if (count > ViewModel?.LineCount)
            {
                var diff = count - ViewModel?.LineCount;
                for (var i = 0; i < diff; i++)
                {
                    ViewModel?.RemoveTop();
                }
            }

            if (ViewModel?.ScrollToEnd == true)
            {
                if (ListMetaLogs.Items.Count > 0)
                {
                    ListMetaLogs.ScrollIntoView(ListMetaLogs.Items[ListMetaLogs.Items.Count - 1]);
                }
            }
        }
    }

    private void ClearMsg()
    {
        Dispatcher.Invoke((Action)(() => { ViewModel?.MetaLogClear(); }));
        Dispatcher.Invoke((Action)(() => { TxtMsg4ClashN.Clear(); }));
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        ClearMsg();
    }
}