using ClashN.ViewModels;
using MaterialDesignThemes.Wpf;
using ReactiveUI;

namespace ClashN.Handler;

public class NoticeHandler
{
    private static Lazy<NoticeHandler> _instance = new(() => new NoticeHandler());

    public static NoticeHandler Instance => _instance.Value;
    
    private ISnackbarMessageQueue _snackbarMessageQueue;

    public void ConfigMessageQueue(ISnackbarMessageQueue snackbarMessageQueue)
    {
        _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));
    }
    
    public void OnShowMsg(bool notify, LogType logType, string msg)
    {
        if (notify)
        {
            Enqueue(msg);
        }

        SendMessage(logType, msg);
    }

    public void Enqueue(object content)
    {
        _snackbarMessageQueue.Enqueue(content);
    }

    public static void SendMessage(LogType logType, string msg)
    {
        MessageBus.Current.SendMessage(msg, logType.ToString());
    }

    public static void SendMessage4ClashN(string msg)
    {
        msg = $"{DateTime.Now} {msg}";
        MessageBus.Current.SendMessage(msg, LogType.Log4ClashN.ToString());
    }
}