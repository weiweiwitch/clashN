using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
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
    
    
   
    
    
    public Icon GetNotifyIcon(Config config)
    {
        try
        {
            var index = (int)config.SysProxyType;

            //Load from local file
            var fileName = Utils.GetPath($"NotifyIcon{index + 1}.ico");
            if (File.Exists(fileName))
            {
                return new Icon(fileName);
            }

            switch (index)
            {
                case 0:
                    return Resources.NotifyIcon1;

                case 1:
                    return Resources.NotifyIcon2;

                case 2:
                    return Resources.NotifyIcon3;

                case 3:
                    return Resources.NotifyIcon2;
            }

            return Resources.NotifyIcon1;
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
            return Resources.NotifyIcon1;
        }
    }

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

    public void OnTimer4UpdateTask(ref DateTime autoUpdateSubTime, Config config, Action<bool, string> update)
    {
        Utils.SaveLog($"MainFormHandler:CreateUpdateTask - Update Task Run");

        var updateHandle = new UpdateHandle();
        var dtNow = DateTime.Now;

        if (config.AutoUpdateSubInterval <= 0)
        {
            return;
        }

        var diffTime = dtNow - autoUpdateSubTime;
        if (diffTime.Hours % config.AutoUpdateSubInterval != 0)
        {
            return;
        }

        // Update
        autoUpdateSubTime = dtNow;

        // Run time update task
        Task.Run(() =>
        {
            Utils.SaveLog($"MainFormHandler:CreateUpdateTask - Update Task Runs In Timer Thread");
            updateHandle.UpdateSubscriptionProcess(true, null, (success, msg) =>
            {
                update(success, msg);
                if (success)
                {
                    Utils.SaveLog("subscription" + msg);
                }
            });
        });
    }

    public static void RegisterGlobalHotkey(Config config, EventHandler<HotkeyEventArgs> handler)
    {
        Utils.SaveLog($"MainFormHandler:RegisterGlobalHotkey");
        
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

    public void GetClashProxies(Action<ClashProxies, ClashProviders> update)
    {
        Task.Run(() => GetClashProxiesAsync(update));
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

            Thread.Sleep(5000);
        }

        update(null, null);
    }

    public void ClashProxiesDelayTest(bool blAll, List<ProxyModel> lstProxy, Action<ProxyModel?, string> update)
    {
        Task.Run(() =>
        {
            if (blAll)
            {
                for (var i = 0; i < 5; i++)
                {
                    if (LazyConfig.Instance.GetProxies() != null)
                    {
                        break;
                    }

                    Thread.Sleep(5000);
                }

                var proxies = LazyConfig.Instance.GetProxies();
                if (proxies == null)
                {
                    return;
                }

                lstProxy = new List<ProxyModel>();
                foreach (KeyValuePair<string, ProxiesItem> kv in proxies)
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

            Task.WaitAll(tasks.ToArray());

            Thread.Sleep(1000);
            update(null, "");
        });
    }

    public static void InitRegister()
    {
        Task.Run(() =>
        {
            //URL Schemes
            Utils.RegWriteValue(Global.MyRegPathClasses, "", "URL:clash");
            Utils.RegWriteValue(Global.MyRegPathClasses, "URL Protocol", "");
            Utils.RegWriteValue($"{Global.MyRegPathClasses}\\shell\\open\\command", "",
                $"\"{Utils.GetExePath()}\" \"%1\"");
        });
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

    public void ClashConfigUpdate(Dictionary<string, string> headers)
    {
        Task.Run(async () =>
        {
            var proxies = LazyConfig.Instance.GetProxies();
            if (proxies == null)
            {
                return;
            }

            var urlBase = $"{GetApiUrl()}/configs";

            await HttpClientHelper.GetInstance().PatchAsync(urlBase, headers);
        });
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

    public void GetClashConnections(Config config, Action<ClashConnections> update)
    {
        Task.Run(() => GetClashConnectionsAsync(config, update));
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