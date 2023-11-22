using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using ClashN.Base;
using ClashN.Mode;
using ClashN.Properties;
using ClashN.Resx;
using ClashN.Tool;
using NHotkey;
using NHotkey.Wpf;
using static ClashN.Mode.ClashProxies;

namespace ClashN.Handler;

public sealed class MainFormHandler
{
    private static Lazy<MainFormHandler> _instance = new(() => new MainFormHandler());

    public static MainFormHandler Instance => _instance.Value;

    private DateTime _autoUpdateSubTime = DateTime.Now;


    public static void BackupGuiNConfig(bool auto = false)
    {
        var fileName = $"guiNConfig_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff")}.json";
        if (auto)
        {
            fileName = Utils.GetBackupPath(fileName);
        }
        else
        {
            var fileDialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = "guiNConfig|*.json",
                FilterIndex = 2,
                RestoreDirectory = true
            };

            var parent = App.Current.MainWindow.WpfWindow2WinFormWin32Window();
            if (fileDialog.ShowDialog(parent) != DialogResult.OK)
            {
                return;
            }

            fileName = fileDialog.FileName;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        var ret = Utils.ToJsonFile(config, fileName);
        if (!auto)
        {
            if (ret == 0)
            {
                NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
            }
            else
            {
                NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
            }
        }
    }

    public void OnTimer4UpdateTask(Action<bool, string> cbUpdateSubscriptionFinish)
    {
        Utils.SaveLog($"MainFormHandler:CreateUpdateTask - Update Task Run");

        var config = LazyConfig.Instance.Config;
        if (config.AutoUpdateSubInterval <= 0)
        {
            return;
        }

        var dtNow = DateTime.Now;
        var diffTime = dtNow - _autoUpdateSubTime;
        if (diffTime.Hours % config.AutoUpdateSubInterval != 0)
        {
            return;
        }

        // Update
        _autoUpdateSubTime = dtNow;

        // Run time update task
        Utils.SaveLog($"MainFormHandler:CreateUpdateTask - Update Task Runs In Timer Thread");

        UpdateHandle.UpdateSubscriptionProcess(true, new List<ProfileItem>(), (success, msg) =>
        {
            NoticeHandler.SendMessage4ClashN(msg);

            if (success)
            {
                Utils.SaveLog($"MainFormHandler:OnTimer4UpdateTask - UpdateSubscriptionProcess Finished: {msg}");
            }

            cbUpdateSubscriptionFinish(success, msg);
        });
    }

    public static void RegisterGlobalHotkey(EventHandler<HotkeyEventArgs> handler)
    {
        Utils.SaveLog($"MainFormHandler:RegisterGlobalHotkey");

        var config = LazyConfig.Instance.Config;
        foreach (var item in config.GlobalHotkeys)
        {
            if (item.KeyCode == null)
            {
                continue;
            }

            var modifiers = ModifierKeys.None;
            if (item.Control)
            {
                modifiers |= ModifierKeys.Control;
            }

            if (item.Alt)
            {
                modifiers |= ModifierKeys.Alt;
            }

            if (item.Shift)
            {
                modifiers |= ModifierKeys.Shift;
            }

            var gesture = new KeyGesture(KeyInterop.KeyFromVirtualKey((int)item.KeyCode), modifiers);
            try
            {
                HotkeyManager.Current.AddOrReplace(((int)item.GlobalHotkey).ToString(), gesture, handler);
                var msg = string.Format(ResUI.RegisterGlobalHotkeySuccessfully,
                    $"{item.GlobalHotkey.ToString()} = {Utils.ToJson(item)}");
                NoticeHandler.SendMessage4ClashN(msg);
            }
            catch (Exception ex)
            {
                var msg = string.Format(ResUI.RegisterGlobalHotkeyFailed,
                    $"{item.GlobalHotkey.ToString()} = {Utils.ToJson(item)}", ex.Message);
                NoticeHandler.SendMessage4ClashN(msg);
                Utils.SaveLog(msg);
            }
        }
    }

    public async void GetClashProxies(Action<ClashProxies, ClashProviders> update)
    {
        await GetClashProxiesAsync(update);
    }

    private async Task GetClashProxiesAsync(Action<ClashProxies, ClashProviders> update)
    {
        for (var i = 0; i < 5; i++)
        {
            var url = $"{GetApiUrl()}/proxies";
            var result = await HttpClientHelper.GetInstance().TryGetAsync(url);
            var clashProxies = Utils.FromJson<ClashProxies>(result);

            var url2 = $"{GetApiUrl()}/providers/proxies";
            var result2 = await HttpClientHelper.GetInstance().TryGetAsync(url2);
            var clashProviders = Utils.FromJson<ClashProviders>(result2);

            if (clashProxies != null || clashProviders != null)
            {
                update(clashProxies, clashProviders);
                return;
            }

            await Task.Delay(5000);
        }

        update(null, null);
    }

    public async void ClashProxiesDelayTest(bool blAll, List<ProxyModel> lstProxy, Action<ProxyModel?, string> update)
    {
        Utils.SaveLog("MainFormHandler:ClashProxiesDelayTest");

        if (blAll)
        {
            for (var i = 0; i < 5; i++)
            {
                if (LazyConfig.Instance.GetProxies() != null)
                {
                    break;
                }

                await Task.Delay(5000);
            }

            var proxies = LazyConfig.Instance.GetProxies();
            if (proxies == null)
            {
                return;
            }

            lstProxy = new List<ProxyModel>();
            foreach (var kv in proxies)
            {
                if (Global.NotAllowTestType.Contains(kv.Value.type.ToLower()))
                {
                    continue;
                }

                lstProxy.Add(new ProxyModel()
                {
                    name = kv.Value.name,
                    type = kv.Value.type.ToLower(),
                });
            }
        }

        if (lstProxy == null)
        {
            return;
        }

        var urlBase = $"{GetApiUrl()}/proxies";
        urlBase += @"/{0}/delay?timeout=10000&url=" + LazyConfig.Instance.Config.ConstItem.SpeedPingTestUrl;

        var tasks = new List<Task>();
        foreach (var it in lstProxy)
        {
            if (Global.NotAllowTestType.Contains(it.type.ToLower()))
            {
                continue;
            }

            var name = it.name;
            var url = string.Format(urlBase, name);
            tasks.Add(Task.Run(async () =>
            {
                var result = await HttpClientHelper.GetInstance().TryGetAsync(url);
                update(it, result);
            }));
        }

        Utils.SaveLog("MainFormHandler:ClashProxiesDelayTest - Start to wait all task");
        Task.WaitAll(tasks.ToArray());

        await Task.Delay(1000);

        update(null, "");
    }

    public static void InitRegister()
    {
        //URL Schemes
        Utils.RegWriteValue(Global.MyRegPathClasses, "", "URL:clash");
        Utils.RegWriteValue(Global.MyRegPathClasses, "URL Protocol", "");
        Utils.RegWriteValue($"{Global.MyRegPathClasses}\\shell\\open\\command", "",
            $"\"{Utils.GetExePath()}\" \"%1\"");
    }

    public static List<ProxiesItem> GetClashProxyGroups()
    {
        try
        {
            var fileContent = LazyConfig.Instance.ProfileContent;
            if (!fileContent.ContainsKey("proxy-groups"))
            {
                return null;
            }

            return Utils.FromJson<List<ProxiesItem>>(Utils.ToJson(fileContent["proxy-groups"]));
        }
        catch (Exception ex)
        {
            Utils.SaveLog("GetClashProxyGroups", ex);
            return null;
        }
    }

    public async void ClashSetActiveProxy(string name, string nameNode)
    {
        try
        {
            var url = $"{GetApiUrl()}/proxies/{name}";
            var headers = new Dictionary<string, string>();
            headers.Add("name", nameNode);
            await HttpClientHelper.GetInstance().PutAsync(url, headers);
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    public async void ClashConfigUpdate(Dictionary<string, string> headers)
    {
        var proxies = LazyConfig.Instance.GetProxies();
        if (proxies == null)
        {
            return;
        }

        var urlBase = $"{GetApiUrl()}/configs";

        await HttpClientHelper.GetInstance().PatchAsync(urlBase, headers);
    }

    public async void ClashConfigReload(string filePath)
    {
        ClashConnectionClose("");

        try
        {
            var url = $"{GetApiUrl()}/configs?force=true";
            var headers = new Dictionary<string, string>();
            headers.Add("path", filePath);
            await HttpClientHelper.GetInstance().PutAsync(url, headers);
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    public async void GetClashConnections(Config config, Action<ClashConnections> update)
    {
       await GetClashConnectionsAsync(config, update);
    }

    private async Task GetClashConnectionsAsync(Config config, Action<ClashConnections> update)
    {
        try
        {
            var url = $"{GetApiUrl()}/connections";
            var result = await HttpClientHelper.GetInstance().TryGetAsync(url);
            var clashConnections = Utils.FromJson<ClashConnections>(result);

            update(clashConnections);
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    public async void ClashConnectionClose(string id)
    {
        try
        {
            var url = $"{GetApiUrl()}/connections/{id}";
            await HttpClientHelper.GetInstance().DeleteAsync(url);
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    private string GetApiUrl()
    {
        return $"{Global.HttpProtocol}{Global.Loopback}:{LazyConfig.Instance.Config.ApiPort}";
    }
}