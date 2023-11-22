using ReactiveUI;
using System.Reactive.Linq;
using System.Windows.Threading;

namespace ClashN.Views;

/// <summary>
/// Interaction logic for MsgView.xaml
/// </summary>
public partial class MsgView
{
    public MsgView()
    {
        InitializeComponent();
        MessageBus.Current.Listen<string>("MsgView").Subscribe(x => DelegateAppendText(x));
    }

    private void DelegateAppendText(string msg)
    {
        Dispatcher.BeginInvoke(new Action<string>(AppendText), DispatcherPriority.Send, msg);
    }

    public void AppendText(string msg)
    {
        //if (!string.IsNullOrEmpty(MsgFilter))
        //{
        //    if (!Regex.IsMatch(text, MsgFilter))
        //    {
        //        return;
        //    }
        //}

        ShowMsg(msg);
    }

    private void ShowMsg(string msg)
    {
        if (TxtMsg.LineCount > 999)
        {
            ClearMsg();
        }
        TxtMsg.AppendText(msg);
        if (!msg.EndsWith(Environment.NewLine))
        {
            TxtMsg.AppendText(Environment.NewLine);
        }
        TxtMsg.ScrollToEnd();
    }

    public void ClearMsg()
    {
        Dispatcher.Invoke((Action)(() =>
        {
            TxtMsg.Clear();
        }));
    }
}