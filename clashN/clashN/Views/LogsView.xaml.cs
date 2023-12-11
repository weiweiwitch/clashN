using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Threading;
using ClashN.Handler;
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
            // 根据当前激活的配置类型的不同，使用不同的方式解析日志。
            var activeProfile = ConfigHandler.GetActiveProfile();
            var metaLogInfos = new string[3];
            if (activeProfile?.CoreType == CoreKind.Mihomo)
            {
                var logInfos = msg.Split(" ", 2);
                metaLogInfos[0]= logInfos[0];
                metaLogInfos[1]= "unknown";
                metaLogInfos[2]= logInfos[1];
            }
            else
            {
                var logInfos = msg.Split(" ", 3);
                var time = logInfos[0].Split("=");
                metaLogInfos[0]= time.Length >= 2 ? time[1].Substring(1, time[1].Length - 2) : "Unknown";
                
                var logLv = logInfos[1].Split("=");
                metaLogInfos[1]= logLv.Length >= 2 ? logLv[1] : "Unknown";
                
                var logMsg = logInfos[2].Split("=");
                metaLogInfos[2]= logMsg.Length >= 2 ? logMsg[1].Substring(1, logMsg[1].Length - 2) : "Unknown";
            }
            
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