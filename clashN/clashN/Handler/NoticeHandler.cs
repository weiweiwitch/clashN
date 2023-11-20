﻿using ClashN.ViewModels;
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

    public void SendMessage(string msg)
    {
        MessageBus.Current.SendMessage(msg, LogType.Log4Clash.ToString());
    }

    public void SendMessage4ClashN(string msg)
    {
        MessageBus.Current.SendMessage(msg, LogType.Log4ClashN.ToString());
    }

    public void SendMessageWithTime(string msg)
    {
        msg = $"{DateTime.Now} {msg}";
        MessageBus.Current.SendMessage(msg, LogType.Log4Clash.ToString());
    }

    public void SendMessage4ClashNWithTime(string msg)
    {
        msg = $"{DateTime.Now} {msg}";
        MessageBus.Current.SendMessage(msg, LogType.Log4ClashN.ToString());
    }
}