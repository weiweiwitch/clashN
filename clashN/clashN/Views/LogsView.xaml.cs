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

        MessageBus.Current.Listen<string>("MsgView").Subscribe(x => DelegateAppendText(x));

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.MsgFilter, v => v.TxtFilter.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.ScrollToEnd, v => v.TogScrollToEnd.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AutoRefresh, v => v.TogAutoRefresh.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.LineCount, v => v.CmbLineCount.Text).DisposeWith(disposables);
        });
    }

    private void DelegateAppendText(string msg)
    {
        Dispatcher.BeginInvoke(new Action<string>(AppendText), DispatcherPriority.Send, msg);
    }

    private void AppendText(string msg)
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

        ShowMsg(msg);
    }

    private void ShowMsg(string msg)
    {
        if (TxtMsg.LineCount > ViewModel?.LineCount)
        {
            ClearMsg();
        }

        TxtMsg.AppendText(msg);

        if (!msg.EndsWith(Environment.NewLine))
        {
            TxtMsg.AppendText(Environment.NewLine);
        }

        if (ViewModel?.ScrollToEnd == true)
        {
            TxtMsg.ScrollToEnd();
        }
    }

    public void ClearMsg()
    {
        Dispatcher.Invoke((Action)(() => { TxtMsg.Clear(); }));
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        ClearMsg();
    }
}