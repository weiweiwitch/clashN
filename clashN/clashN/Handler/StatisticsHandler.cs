using ClashN.Mode;
using System.Net.WebSockets;
using System.Text;
using ClashN.Tool;

namespace ClashN.Handler;

internal class StatisticsHandler
{
    private bool _exitFlag;

    private ClientWebSocket? _webSocket = null;
    private string _url = string.Empty;

    private readonly Action<ulong, ulong> _cbStatisticUpdateFunc;

    private bool Enable { get; set; }

    public StatisticsHandler(Action<ulong, ulong> cbStatisticUpdate)
    {
        _cbStatisticUpdateFunc = cbStatisticUpdate;

        Enable = LazyConfig.Instance.Config.EnableStatistics;
        _exitFlag = false;

        Task.Run(Run);
    }


    public void Close()
    {
        Utils.SaveLog("StatisticsHandler:Close");

        try
        {
            _exitFlag = true;

            var webSocket = _webSocket;
            if (webSocket != null)
            {
                webSocket.Abort();

                _webSocket = null;
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    private async Task Run()
    {
        await Init();

        while (!_exitFlag)
        {
            var receiveRt = await ReceiveStatistic();
            if (!receiveRt)
            {
                if (_exitFlag)
                {
                    Utils.SaveLog("ReceiveStatistic failed. Because app existed");
                    break;
                }

                Utils.SaveLogWarn("ReceiveStatistic failed. Try reset and reconnect");

                Reset();

                await Task.Delay(5000).ConfigureAwait(true);

                var connectRt = await ConnectToBackend();
                while (!_exitFlag && !connectRt)
                {
                    Utils.SaveLogWarn("Connect to the backend failed when reset. Try again later");

                    await Task.Delay(5000).ConfigureAwait(true);

                    connectRt = await ConnectToBackend();
                }
            }

            await Task.Delay(1000).ConfigureAwait(true);
        }
    }

    private async Task Init()
    {
        Utils.SaveLog($"StatisticsHandler:Init - _exitFlag: {_exitFlag}");

        await Task.Delay(5000).ConfigureAwait(true);

        var connectRt = await ConnectToBackend();
        while (!_exitFlag && !connectRt)
        {
            Utils.SaveLogWarn("Connect to the backend failed when init. Try again later");

            await Task.Delay(5000).ConfigureAwait(true);

            connectRt = await ConnectToBackend();
        }
    }

    private void Reset()
    {
        var webSocket = _webSocket;
        if (webSocket == null)
        {
            return;
        }

        webSocket.Abort();

        _webSocket = null;
    }

    private async Task<bool> ConnectToBackend()
    {
        Utils.SaveLogDebug("StatisticsHandler:ConnectToBackend - Start to connect to the backend");

        try
        {
            _url = $"ws://{Global.Loopback}:{LazyConfig.Instance.Config.ApiPort}/traffic";

            var webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri(_url), CancellationToken.None).ConfigureAwait(true);

            if (webSocket.State != WebSocketState.Open)
            {
                Utils.SaveLogDebug(
                    $"StatisticsHandler:ConnectToBackend - WebSocket ConnectAsync failed. WebSocketState:{webSocket.State}");
                return false;
            }

            Utils.SaveLogDebug(
                $"StatisticsHandler:ConnectToBackend - ConnectAsync Finished. WebSocketState:{webSocket.State}");

            _webSocket = webSocket;
        }
        catch (Exception ex)
        {
            Utils.SaveLog("StatisticsHandler:ConnectToBackend - Create WebSocket and connect to the backend failed",
                ex);

            return false;
        }

        return true;
    }

    private async Task<bool> ReceiveStatistic()
    {
        if (!Enable)
        {
            return true;
        }

        try
        {
            var webSocket = _webSocket;
            if (webSocket == null)
            {
                Utils.SaveLogWarn(
                    "StatisticsHandler:ReceiveStatistic - EnableStatistics enabled, but webSocket is null.");
                return false;
            }

            if (webSocket.State == WebSocketState.Aborted || webSocket.State == WebSocketState.Closed)
            {
                return false;
            }

            if (webSocket.State != WebSocketState.Open)
            {
                return false;
            }

            var buffer = new byte[1024];
            var res = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(true);
            while (!res.CloseStatus.HasValue)
            {
                var result = Encoding.UTF8.GetString(buffer, 0, res.Count);
                if (!string.IsNullOrEmpty(result))
                {
                    ParseOutput(result, out var up, out var down);
                    if (up + down > 0)
                    {
                        var config = LazyConfig.Instance.Config;
                        var serverStatItem = config.GetProfileItem(config.IndexId);
                        if (serverStatItem != null)
                        {
                            serverStatItem.UploadRemote += up;
                            serverStatItem.DownloadRemote += down;
                        }
                    }

                    _cbStatisticUpdateFunc(up, down);
                }

                res = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(true);
            }
        }
        catch (Exception e)
        {
            Utils.SaveLog("StatisticsHandler:Run - WebSocket ReceiveAsync error", e);

            return false;
        }

        return true;
    }

    private static void ParseOutput(string source, out ulong up, out ulong down)
    {
        up = 0;
        down = 0;
        try
        {
            var trafficItem = Utils.FromJson<TrafficItem>(source);
            if (trafficItem != null)
            {
                up = trafficItem.up;
                down = trafficItem.down;
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog("Parse Statistic Output error", ex);
        }
    }
}