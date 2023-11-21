using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ClashN.Base;
using ClashN.Mode;
using ClashN.Resx;
using ClashN.Tool;
using Splat;

namespace ClashN.Handler;

internal class UpdateHandle
{
    private Action<bool, string> _updateFunc;
    private Config _config;

    public event EventHandler<ResultEventArgs> AbsoluteCompleted;

    public class ResultEventArgs : EventArgs
    {
        public bool Success;
        public string Msg;

        public ResultEventArgs(bool success, string msg)
        {
            Success = success;
            Msg = msg;
        }
    }

    public void CheckUpdateGuiN(Config config, Action<bool, string> update)
    {
        _config = config;
        _updateFunc = update;
        var url = string.Empty;

        DownloadHandle downloadHandle = null;
        if (downloadHandle == null)
        {
            downloadHandle = new DownloadHandle();

            downloadHandle.UpdateCompleted += (sender2, args) =>
            {
                if (args.Success)
                {
                    _updateFunc(false, ResUI.MsgDownloadCoreSuccessfully);

                    try
                    {
                        var fileName = Utils.GetTempPath(Utils.GetDownloadFileName(url));
                        fileName = Utils.UrlEncode(fileName);
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "clashUpgrade.exe",
                                Arguments = "\"" + fileName + "\"",
                                WorkingDirectory = Utils.StartupPath()
                            }
                        };
                        process.Start();
                        if (process.Id > 0)
                        {
                            _updateFunc(true, "");
                        }
                    }
                    catch (Exception ex)
                    {
                        _updateFunc(false, ex.Message);
                    }
                }
                else
                {
                    _updateFunc(false, args.Msg);
                }
            };
            downloadHandle.Error += (sender2, args) => { _updateFunc(false, args.GetException().Message); };
        }

        AbsoluteCompleted += (sender2, args) =>
        {
            if (args.Success)
            {
                _updateFunc(false, string.Format(ResUI.MsgParsingSuccessfully, "ClashN"));

                url = args.Msg;
                AskToDownload(downloadHandle, url, true);
            }
            else
            {
                Locator.Current.GetService<NoticeHandler>()?.Enqueue(args.Msg);
                _updateFunc(false, args.Msg);
            }
        };
        _updateFunc(false, string.Format(ResUI.MsgStartUpdating, "ClashN"));
        CheckUpdateAsync(CoreKind.ClashN);
    }

    public void CheckUpdateCore(CoreKind type, Config config, Action<bool, string> update)
    {
        _config = config;
        _updateFunc = update;
        var url = string.Empty;

        DownloadHandle downloadHandle = null;
        if (downloadHandle == null)
        {
            downloadHandle = new DownloadHandle();
            downloadHandle.UpdateCompleted += (sender2, args) =>
            {
                if (args.Success)
                {
                    _updateFunc(false, ResUI.MsgDownloadCoreSuccessfully);
                    _updateFunc(false, ResUI.MsgUnpacking);

                    try
                    {
                        _updateFunc(true, url);
                    }
                    catch (Exception ex)
                    {
                        _updateFunc(false, ex.Message);
                    }
                }
                else
                {
                    _updateFunc(false, args.Msg);
                }
            };
            downloadHandle.Error += (sender2, args) => { _updateFunc(true, args.GetException().Message); };
        }

        AbsoluteCompleted += (sender2, args) =>
        {
            if (args.Success)
            {
                _updateFunc(false, string.Format(ResUI.MsgParsingSuccessfully, "Core"));
                url = args.Msg;
                AskToDownload(downloadHandle, url, true);
            }
            else
            {
                Locator.Current.GetService<NoticeHandler>()?.Enqueue(args.Msg);
                _updateFunc(false, args.Msg);
            }
        };
        _updateFunc(false, string.Format(ResUI.MsgStartUpdating, "Core"));
        CheckUpdateAsync(type);
    }

    public void UpdateSubscriptionProcess(Config config, bool blProxy, List<ProfileItem> profileItems,
        Action<bool, string> update)
    {
        _config = config;
        _updateFunc = update;

        _updateFunc(false, ResUI.MsgUpdateSubscriptionStart);

        if (config.ProfileItems == null || config.ProfileItems.Count <= 0)
        {
            _updateFunc(false, ResUI.MsgNoValidSubscription);
            return;
        }

        Task.Run(async () =>
        {
            //Turn off system proxy
            //bool bSysProxyType = false;
            //if (!blProxy && config.SysProxyType == SysProxyType.ForcedChange)
            //{
            //    bSysProxyType = true;
            //    config.SysProxyType = SysProxyType.ForcedClear;
            //    SysProxyHandle.UpdateSysProxy(config, false);
            //    Thread.Sleep(3000);
            //}

            if (profileItems == null)
            {
                profileItems = config.ProfileItems;
            }

            foreach (var item in profileItems)
            {
                var indexId = item.indexId.TrimEx();
                var url = item.url.TrimEx();
                var userAgent = item.userAgent.TrimEx();
                var groupId = item.groupId.TrimEx();
                var hashCode = $"{item.remarks}->";
                if (item.enabled == false || string.IsNullOrEmpty(indexId) || string.IsNullOrEmpty(url))
                {
                    _updateFunc(false, $"{hashCode}{ResUI.MsgSkipSubscriptionUpdate}");
                    continue;
                }

                _updateFunc(false, $"{hashCode}{ResUI.MsgStartGettingSubscriptions}");

                if (item.enableConvert)
                {
                    if (string.IsNullOrEmpty(config.ConstItem.subConvertUrl))
                    {
                        config.ConstItem.subConvertUrl = Global.SubConvertUrls[0];
                    }

                    url = string.Format(config.ConstItem.subConvertUrl, Utils.UrlEncode(url));
                    if (!url.Contains("config="))
                    {
                        url += $"&config={Global.SubConvertConfig[0]}";
                    }
                }

                var downloadHandle = new DownloadHandle();
                downloadHandle.Error += (sender2, args) =>
                {
                    _updateFunc(false, $"{hashCode}{args.GetException().Message}");
                };
                var result = await downloadHandle.DownloadStringAsync(url, blProxy, userAgent) ??
                             throw new Exception();
                if (blProxy && string.IsNullOrEmpty(result.Item1))
                {
                    result = await downloadHandle.DownloadStringAsync(url, false, userAgent) ??
                             throw new Exception();
                }

                if (string.IsNullOrEmpty(result.Item1))
                {
                    _updateFunc(false, $"{hashCode}{ResUI.MsgSubscriptionDecodingFailed}");
                }
                else
                {
                    _updateFunc(false, $"{hashCode}{ResUI.MsgGetSubscriptionSuccessfully}");
                    if (result.Item1.Length < 99)
                    {
                        _updateFunc(false, $"{hashCode}{result}");
                    }

                    var ret = ConfigProc.AddBatchProfiles(ref config, result.Item1, indexId, groupId);
                    if (ret == 0)
                    {
                        item.updateTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

                        //get remote info
                        try
                        {
                            if (result.Item2 != null && result.Item2 is HttpResponseHeaders)
                            {
                                var userinfo = ((HttpResponseHeaders)result.Item2)
                                    .Where(t => t.Key.ToLower() == "subscription-userinfo")
                                    .Select(t => t.Value)
                                    .FirstOrDefault()?
                                    .FirstOrDefault();

                                Dictionary<string, string>? dicInfo = userinfo?.Split(';')
                                    .Select(value => value.Split('='))
                                    .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());

                                if (dicInfo != null)
                                {
                                    item.uploadRemote = ParseRemoteInfo(dicInfo, "upload");
                                    item.downloadRemote = ParseRemoteInfo(dicInfo, "download");
                                    item.totalRemote = ParseRemoteInfo(dicInfo, "total");
                                    item.expireRemote = dicInfo.ContainsKey("expire")
                                        ? Convert.ToInt64(dicInfo?["expire"])
                                        : 0;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _updateFunc(false, ex.Message);
                        }

                        _updateFunc(false, $"{hashCode}{ResUI.MsgUpdateSubscriptionEnd}");
                    }
                    else
                    {
                        _updateFunc(false, $"{hashCode}{ResUI.MsgFailedImportSubscription}");
                    }
                }

                _updateFunc(false, $"-------------------------------------------------------");
            }

            //restore system proxy
            //if (bSysProxyType)
            //{
            //    config.SysProxyType = SysProxyType.ForcedChange;
            //    SysProxyHandle.UpdateSysProxy(config, false);
            //}
            _updateFunc(true, $"{ResUI.MsgUpdateSubscriptionEnd}");
        });
    }

    private ulong ParseRemoteInfo(Dictionary<string, string> dicInfo, string key)
    {
        return dicInfo.ContainsKey(key) ? Convert.ToUInt64(dicInfo?[key]) : 0;
    }

    public void UpdateGeoFile(string geoName, Config config, Action<bool, string> update)
    {
        _config = config;
        _updateFunc = update;
        var url = string.Format(Global.GeoUrl, geoName);

        DownloadHandle downloadHandle = null;
        if (downloadHandle == null)
        {
            downloadHandle = new DownloadHandle();

            downloadHandle.UpdateCompleted += (sender2, args) =>
            {
                if (args.Success)
                {
                    _updateFunc(false, string.Format(ResUI.MsgDownloadGeoFileSuccessfully, geoName));

                    try
                    {
                        string fileName = Utils.GetPath(Utils.GetDownloadFileName(url));
                        if (File.Exists(fileName))
                        {
                            string targetPath = Utils.GetPath($"{geoName}.dat");
                            if (File.Exists(targetPath))
                            {
                                File.Delete(targetPath);
                            }

                            File.Move(fileName, targetPath);
                            //_updateFunc(true, "");
                        }
                    }
                    catch (Exception ex)
                    {
                        _updateFunc(false, ex.Message);
                    }
                }
                else
                {
                    _updateFunc(false, args.Msg);
                }
            };
            downloadHandle.Error += (sender2, args) => { _updateFunc(false, args.GetException().Message); };
        }

        AskToDownload(downloadHandle, url, false);
    }

    #region private

    private async void CheckUpdateAsync(CoreKind type)
    {
        try
        {
            var coreInfo = LazyConfig.Instance.GetCoreInfo(type);
            string url = coreInfo.CoreLatestUrl;

            var result = await (new DownloadHandle()).UrlRedirectAsync(url, true);
            if (!string.IsNullOrEmpty(result))
            {
                ResponseHandler(type, result);
            }
            else
            {
                Utils.SaveLog("StatusCode error: " + url);
                return;
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
            _updateFunc(false, ex.Message);
            if (ex.InnerException != null)
            {
                _updateFunc(false, ex.InnerException.Message);
            }
        }
    }

    /// <summary>
    /// 获取Core版本
    /// </summary>
    private string GetCoreVersion(CoreKind type)
    {
        try
        {
            var coreInfo = LazyConfig.Instance.GetCoreInfo(type);
            var filePath = string.Empty;
            foreach (var name in coreInfo.CoreExes)
            {
                var vName = $"{name}.exe";
                vName = Utils.GetBinPath(vName, coreInfo.CoreType);
                if (File.Exists(vName))
                {
                    filePath = vName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                var msg = string.Format(ResUI.NotFoundCore, @"");
                return "";
            }

            var p = new Process();
            p.StartInfo.FileName = filePath;
            p.StartInfo.Arguments = "-v";
            p.StartInfo.WorkingDirectory = Utils.StartupPath();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.Start();
            p.WaitForExit(5000);
            var echo = p.StandardOutput.ReadToEnd();
            var version = Regex.Match(echo, $"v[0-9.]+").Groups[0].Value;
            return version;
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
            _updateFunc(false, ex.Message);
            return "";
        }
    }

    private void ResponseHandler(CoreKind type, string redirectUrl)
    {
        try
        {
            var version = redirectUrl.Substring(redirectUrl.LastIndexOf("/", StringComparison.Ordinal) + 1);
            var coreInfo = LazyConfig.Instance.GetCoreInfo(type);

            string curVersion;
            string message;
            string url;
            if (type == CoreKind.Clash)
            {
                curVersion = GetCoreVersion(type);
                message = string.Format(ResUI.IsLatestCore, curVersion);
                if (Environment.Is64BitProcess)
                {
                    url = string.Format(coreInfo.CoreDownloadUrl64, version);
                }
                else
                {
                    url = string.Format(coreInfo.CoreDownloadUrl32, version);
                }
            }
            else if (type == CoreKind.ClashMeta)
            {
                curVersion = GetCoreVersion(type);
                message = string.Format(ResUI.IsLatestCore, curVersion);
                if (Environment.Is64BitProcess)
                {
                    url = string.Format(coreInfo.CoreDownloadUrl64, version);
                }
                else
                {
                    url = string.Format(coreInfo.CoreDownloadUrl32, version);
                }
            }
            else if (type == CoreKind.ClashN)
            {
                curVersion = FileVersionInfo.GetVersionInfo(Utils.GetExePath()).FileVersion.ToString();
                message = string.Format(ResUI.IsLatestN, curVersion);
                url = string.Format(coreInfo.CoreDownloadUrl64, version);
            }
            else
            {
                throw new ArgumentException("Type");
            }

            if (curVersion == version)
            {
                AbsoluteCompleted?.Invoke(this, new ResultEventArgs(false, message));
                return;
            }

            AbsoluteCompleted?.Invoke(this, new ResultEventArgs(true, url));
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
            _updateFunc(false, ex.Message);
        }
    }

    private void AskToDownload(DownloadHandle downloadHandle, string url, bool blAsk)
    {
        var blDownload = false;
        if (blAsk)
        {
            if (UI.ShowYesNo(string.Format(ResUI.DownloadYesNo, url)) == DialogResult.Yes)
            {
                blDownload = true;
            }
        }
        else
        {
            blDownload = true;
        }

        if (blDownload)
        {
            downloadHandle.DownloadFileAsync(url, true, 600);
        }
    }

    private int HttpProxyTest()
    {
        var statistics = new SpeedtestHandler(ref _config);
        return statistics.RunAvailabilityCheck();
    }

    #endregion private
}