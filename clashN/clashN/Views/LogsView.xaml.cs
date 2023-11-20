using ClashN.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

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

        var msgFilter = ViewModel?.MsgFilter;
        if (!string.IsNullOrEmpty(msgFilter))
        {
            if (!Regex.IsMatch(msg, msgFilter))
            {
                return;
            }
        }

        var compLog = logType == LogType.Log4Clash ? TxtMsg : TxtMsg4ClashN;
        if (compLog.LineCount > ViewModel?.LineCount)
        {
            ClearMsg();
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

    private void ClearMsg()
    {
        Dispatcher.Invoke((Action)(() => { TxtMsg.Clear(); }));
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        ClearMsg();
    }
}