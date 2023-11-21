using ClashN.Mode;
using System.Net.WebSockets;
using System.Text;
using ClashN.Tool;

namespace ClashN.Handler;

internal class StatisticsHandler
{
    //private ServerStatistics serverStatistics_;
    private bool _exitFlag;

    private ClientWebSocket _webSocket = null;
    private string _url = string.Empty;

    private readonly Action<ulong, ulong> _cbStatisticUpdateFunc;

    private bool Enable { get; set; }

    //private List<ProfileStatItem> Statistic
    //{
    //    get
    //    {
    //        return serverStatistics_.profileStat;
    //    }
    //}

    public StatisticsHandler(Action<ulong, ulong> cbStatisticUpdate)
    {
        Enable = LazyConfig.Instance.Config.EnableStatistics;
        _cbStatisticUpdateFunc = cbStatisticUpdate;
        _exitFlag = false;

        Task.Run(() => Run());
    }

    private async void Init()
    {
        Utils.SaveLog("StatisticsHandler:Init");

        Thread.Sleep(5000);

        try
        {
            _url = $"ws://{Global.Loopback}:{LazyConfig.Instance.Config.ApiPort}/traffic";

            if (_webSocket == null)
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(_url), CancellationToken.None);

                Utils.SaveLogDebug("StatisticsHandler:Init - ConnectAsync Finished");
            }
        }
        catch
        {
        }
    }

    public void Close()
    {
        try
        {
            _exitFlag = true;
            if (_webSocket != null)
            {
                _webSocket.Abort();
                _webSocket = null;
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    private async void Run()
    {
        Init();

        while (!_exitFlag)
        {
            try
            {
                if (Enable)
                {
                    if (_webSocket.State == WebSocketState.Aborted || _webSocket.State == WebSocketState.Closed)
                    {
                        _webSocket.Abort();
                        _webSocket = null;
                        Init();
                    }

                    if (_webSocket.State != WebSocketState.Open)
                    {
                        continue;
                    }

                    var buffer = new byte[1024];
                    var res = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    while (!res.CloseStatus.HasValue)
                    {
                        var result = Encoding.UTF8.GetString(buffer, 0, res.Count);
                        if (!string.IsNullOrEmpty(result))
                        {
                            var config = LazyConfig.Instance.Config;
                            var serverStatItem = config.GetProfileItem(config.IndexId);
                            ParseOutput(result, out var up, out var down);
                            if (up + down > 0)
                            {
                                serverStatItem.UploadRemote += up;
                                serverStatItem.DownloadRemote += down;
                            }

                            _cbStatisticUpdateFunc(up, down);
                        }

                        res = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    }
                }
            }
            catch(Exception e)
            {
                Utils.SaveLog("StatisticsHandler:Run - WebSocket ReceiveAsync Failed", e);
            }
            finally
            {
                Thread.Sleep(1000);
            }
        }
    }

    //public void LoadFromFile()
    //{
    //    try
    //    {
    //        string result = Utils.LoadResource(Utils.GetConfigPath(Global.StatisticLogOverall));
    //        if (!string.IsNullOrEmpty(result))
    //        {
    //            serverStatistics_ = Utils.FromJson<ServerStatistics>(result);
    //        }

    //        if (serverStatistics_ == null)
    //        {
    //            serverStatistics_ = new ServerStatistics();
    //        }
    //        if (serverStatistics_.profileStat == null)
    //        {
    //            serverStatistics_.profileStat = new List<ProfileStatItem>();
    //        }

    //        long ticks = DateTime.Now.Date.Ticks;
    //        foreach (ProfileStatItem item in serverStatistics_.profileStat)
    //        {
    //            if (item.dateNow != ticks)
    //            {
    //                item.todayUp = 0;
    //                item.todayDown = 0;
    //                item.dateNow = ticks;
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Utils.SaveLog(ex.Message, ex);
    //    }
    //}

    //public void SaveToFile()
    //{
    //    try
    //    {
    //        Utils.ToJsonFile(serverStatistics_, Utils.GetConfigPath(Global.StatisticLogOverall));
    //    }
    //    catch (Exception ex)
    //    {
    //        Utils.SaveLog(ex.Message, ex);
    //    }
    //}

    //public void ClearAllServerStatistics()
    //{
    //    if (serverStatistics_ != null)
    //    {
    //        foreach (var item in serverStatistics_.profileStat)
    //        {
    //            item.todayUp = 0;
    //            item.todayDown = 0;
    //            item.totalUp = 0;
    //            item.totalDown = 0;
    //            // update ui display to zero
    //            updateFunc_(0, 0);
    //        }

    //        // update statistic json file
    //        //SaveToFile();
    //    }
    //}

    //public List<ProfileStatItem> GetStatistic()
    //{
    //    return Statistic;
    //}

    //private ProfileStatItem GetServerStatItem(string itemId)
    //{
    //    long ticks = DateTime.Now.Date.Ticks;
    //    int cur = Statistic.FindIndex(item => item.indexId == itemId);
    //    if (cur < 0)
    //    {
    //        Statistic.Add(new ProfileStatItem
    //        {
    //            indexId = itemId,
    //            totalUp = 0,
    //            totalDown = 0,
    //            todayUp = 0,
    //            todayDown = 0,
    //            dateNow = ticks
    //        });
    //        cur = Statistic.Count - 1;
    //    }
    //    if (Statistic[cur].dateNow != ticks)
    //    {
    //        Statistic[cur].todayUp = 0;
    //        Statistic[cur].todayDown = 0;
    //        Statistic[cur].dateNow = ticks;
    //    }
    //    return Statistic[cur];
    //}

    private void ParseOutput(string source, out ulong up, out ulong down)
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
        catch (Exception)
        {
            //Utils.SaveLog(ex.Message, ex);
        }
    }
}