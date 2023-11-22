using System.Net.Http.Headers;
using ClashN.Base;
using ClashN.Mode;
using ClashN.Resx;
using ClashN.Tool;

namespace ClashN.Handler;

public class ResultEventArgs : EventArgs
{
    public readonly bool Success;
    public readonly string Msg;

    public ResultEventArgs(bool success, string msg)
    {
        Success = success;
        Msg = msg;
    }
}

internal class UpdateHandle
{
    public void UpdateSubscriptionProcess(bool blProxy, List<ProfileItem> profileItems, Action<bool, string> cbUpdateSubscription)
    {
        Utils.SaveLog("UpdateHandler:UpdateSubscriptionProcess - Start ");
        
        cbUpdateSubscription(false, ResUI.MsgUpdateSubscriptionStart);

        if (LazyConfig.Instance.Config.ProfileItems.Count == 0 || LazyConfig.Instance.Config.ProfileItems.Count == 0)
        {
            cbUpdateSubscription(false, ResUI.MsgNoValidSubscription);
            return;
        }

        Task.Run(async () =>
        {
            if (profileItems.Count == 0)
            {
                profileItems = LazyConfig.Instance.Config.ProfileItems;
            }

            Utils.SaveLogDebug($"UpdateHandler:UpdateSubscriptionProcess - profileItems num: {profileItems.Count} ");
            foreach (var item in profileItems)
            {
                var indexId = item.IndexId.TrimEx();
                var url = item.Url.TrimEx();
                var userAgent = item.UserAgent.TrimEx();
                var groupId = item.GroupId.TrimEx();
                var hashCode = $"{item.Remarks}->";
                if (item.Enabled == false || string.IsNullOrEmpty(indexId) || string.IsNullOrEmpty(url))
                {
                    cbUpdateSubscription(false, $"{hashCode}{ResUI.MsgSkipSubscriptionUpdate}");
                    continue;
                }

                cbUpdateSubscription(false, $"{hashCode}{ResUI.MsgStartGettingSubscriptions}");

                if (item.EnableConvert)
                {
                    if (string.IsNullOrEmpty(LazyConfig.Instance.Config.ConstItem.SubConvertUrl))
                    {
                        LazyConfig.Instance.Config.ConstItem.SubConvertUrl = Global.SubConvertUrls[0];
                    }

                    url = string.Format(LazyConfig.Instance.Config.ConstItem.SubConvertUrl, Utils.UrlEncode(url));
                    if (!url.Contains("config="))
                    {
                        url += $"&config={Global.SubConvertConfig[0]}";
                    }
                }

                var downloadHandle = new DownloadHandle();
                downloadHandle.Error += (_, args) =>
                {
                    cbUpdateSubscription(false, $"{hashCode}{args.GetException().Message}");
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
                    cbUpdateSubscription(false, $"{hashCode}{ResUI.MsgSubscriptionDecodingFailed}");
                }
                else
                {
                    cbUpdateSubscription(false, $"{hashCode}{ResUI.MsgGetSubscriptionSuccessfully}");
                    
                    if (result.Item1.Length < 99)
                    {
                        cbUpdateSubscription(false, $"{hashCode}{result}");
                    }

                    var ret = ConfigProc.AddBatchProfiles(result.Item1, indexId, groupId);
                    if (ret == 0)
                    {
                        item.UpdateTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

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

                                var dicInfo = userinfo?.Split(';')
                                    .Select(value => value.Split('='))
                                    .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());

                                if (dicInfo != null)
                                {
                                    item.UploadRemote = ParseRemoteInfo(dicInfo, "upload");
                                    item.DownloadRemote = ParseRemoteInfo(dicInfo, "download");
                                    item.TotalRemote = ParseRemoteInfo(dicInfo, "total");
                                    item.ExpireRemote = dicInfo.ContainsKey("expire")
                                        ? Convert.ToInt64(dicInfo?["expire"])
                                        : 0;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            cbUpdateSubscription(false, ex.Message);
                        }

                        cbUpdateSubscription(false, $"{hashCode}{ResUI.MsgUpdateSubscriptionEnd}");
                    }
                    else
                    {
                        cbUpdateSubscription(false, $"{hashCode}{ResUI.MsgFailedImportSubscription}");
                    }
                }

                cbUpdateSubscription(false, $"-------------------------------------------------------");
            }

            cbUpdateSubscription(true, $"{ResUI.MsgUpdateSubscriptionEnd}");
        });
    }
    
    private static ulong ParseRemoteInfo(Dictionary<string, string> dicInfo, string key)
    {
        return dicInfo.TryGetValue(key, out var value) ? Convert.ToUInt64(value) : 0;
    }

}