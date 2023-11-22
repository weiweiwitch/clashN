using ClashN.ViewModels;
using ReactiveUI;

namespace ClashN.Handler;

public class NoticeHandler
{
    private static readonly Lazy<NoticeHandler> _instance = new(() => new NoticeHandler());

    public static NoticeHandler Instance => _instance.Value;

    private Action<object> _enterMessageQueue;

    public void ConfigMessageQueue(Action<object> enterMessageQueue)
    {
        _enterMessageQueue = enterMessageQueue;
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
        _enterMessageQueue(content);
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