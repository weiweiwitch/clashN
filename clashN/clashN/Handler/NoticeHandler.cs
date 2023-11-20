using ClashN.ViewModels;
using MaterialDesignThemes.Wpf;
using ReactiveUI;

namespace ClashN.Handler;

public class NoticeHandler
{
    private readonly ISnackbarMessageQueue _snackbarMessageQueue;

    public NoticeHandler(ISnackbarMessageQueue snackbarMessageQueue)
    {
        _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));

        //_snackbarMessageQueue = snackbarMessageQueue;
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
        MessageBus.Current.SendMessage(msg, LogType.Log4ClashN.ToString());
    }

    public static void SendMessage4ClashNWithTime(string msg)
    {
        msg = $"{DateTime.Now} {msg}";
        MessageBus.Current.SendMessage(msg, LogType.Log4ClashN.ToString());
    }
}