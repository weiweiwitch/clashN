﻿using System.IO;
using System.Web;
using ClashN.Mode;
using ClashN.Tool;

namespace ClashN.Handler;

/// <summary>
/// 本软件配置文件处理类
/// </summary>
internal static class ConfigProc
{
    private static readonly object ObjLock = new();

    private const string ConfigRes = Global.ConfigFileName;

    #region ConfigHandler

    /// <summary>
    /// 载入配置文件
    /// </summary>
    /// <returns></returns>
    public static int LoadConfig()
    {
        //载入配置文件
        Config config;
        var result = Utils.LoadResource(Utils.GetConfigPath(ConfigRes));
        if (string.IsNullOrEmpty(result))
        {
            if (File.Exists(Utils.GetConfigPath(ConfigRes)))
            {
                Utils.SaveLog("LoadConfig Exception");
                return -1;
            }

            // File is not exist
            config = new Config
            {
                LogLevel = "warning",
                EnableStatistics = true,
                MixedPort = 7890,
                ApiPort = 9090,
                PacPort = 7990
            };
        }
        else
        {
            // 转成Json
            config = Utils.FromJson<Config>(result);
        }
        
        if (string.IsNullOrEmpty(config.ConstItem.SpeedTestUrl))
        {
            config.ConstItem.SpeedTestUrl = Global.SpeedTestUrl;
        }

        if (string.IsNullOrEmpty(config.ConstItem.SpeedPingTestUrl))
        {
            config.ConstItem.SpeedPingTestUrl = Global.SpeedPingTestUrl;
        }

        if (string.IsNullOrEmpty(config.ConstItem.DefIeProxyExceptions))
        {
            config.ConstItem.DefIeProxyExceptions = Global.IEProxyExceptions;
        }

        if (config.ProfileItems.Count <= 0)
        {
            Global.reloadCore = false;
        }
        else
        {
            Global.reloadCore = true;

            foreach (var profileItem in config.ProfileItems)
            {
                if (string.IsNullOrEmpty(profileItem.IndexId))
                {
                    profileItem.IndexId = Utils.GetGUID(false);
                }
            }
        }

        LazyConfig.Instance.SetConfig(config);

        return 0;
    }

    /// <summary>
    /// 保参数
    /// </summary>
    /// <returns></returns>
    public static int SaveConfig(bool reload = true)
    {
        Global.reloadCore = reload;

        var config = LazyConfig.Instance.Config;
        ToJsonFile(config);

        return 0;
    }

    /// <summary>
    /// 存储文件
    /// </summary>
    /// <param name="config"></param>
    private static void ToJsonFile(Config config)
    {
        lock (ObjLock)
        {
            try
            {
                //save temp file
                var resPath = Utils.GetConfigPath(ConfigRes);
                var tempPath = $"{resPath}_temp";
                if (Utils.ToJsonFile(config, tempPath) != 0)
                {
                    return;
                }

                if (File.Exists(resPath))
                {
                    File.Delete(resPath);
                }

                //rename
                File.Move(tempPath, resPath);
            }
            catch (Exception ex)
            {
                Utils.SaveLog("ToJsonFile", ex);
            }
        }
    }

    #endregion ConfigHandler

    #region Profile

    /// <summary>
    /// 移除配置文件
    /// </summary>
    /// <param name="config"></param>
    /// <param name="indexs"></param>
    /// <returns></returns>
    public static int RemoveProfile(Config config, List<ProfileItem> indexs)
    {
        foreach (var item in indexs)
        {
            var index = config.FindIndexId(item.IndexId);
            if (index >= 0)
            {
                RemoveProfileItem(config, index);
            }
        }

        ToJsonFile(config);

        return 0;
    }

    /// <summary>
    /// 克隆配置文件
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static int CopyProfile(List<ProfileItem> index)
    {
        var config = LazyConfig.Instance.Config;
        foreach (var item in index)
        {
            ProfileItem profileItem = Utils.DeepCopy(item);
            profileItem.IndexId = string.Empty;
            profileItem.Remarks = $"{item.Remarks}-clone";

            if (string.IsNullOrEmpty(profileItem.Address) || !File.Exists(Utils.GetConfigPath(profileItem.Address)))
            {
                profileItem.Address = string.Empty;
                AddProfileCommon(profileItem);
            }
            else
            {
                var fileName = Utils.GetConfigPath(profileItem.Address);
                profileItem.Address = string.Empty;
                AddProfileViaPath(profileItem, fileName);
            }
        }

        ToJsonFile(config);

        return 0;
    }

    /// <summary>
    /// 设置活动配置文件
    /// </summary>
    /// <param name="config"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public static int SetDefaultProfile(Config config, ProfileItem item)
    {
        if (item == null)
        {
            return -1;
        }

        config.IndexId = item.IndexId;
        Global.reloadCore = true;

        ToJsonFile(config);

        return 0;
    }

    public static int SetDefaultProfile(Config config, List<ProfileItem> lstProfile)
    {
        if (lstProfile.Exists(t => t.IndexId == config.IndexId))
        {
            return 0;
        }

        if (config.ProfileItems.Exists(t => t.IndexId == config.IndexId))
        {
            return 0;
        }

        if (lstProfile.Count > 0)
        {
            return SetDefaultProfile(config, lstProfile[0]);
        }

        if (config.ProfileItems.Count > 0)
        {
            return SetDefaultProfile(config, config.ProfileItems[0]);
        }

        return -1;
    }

    public static ProfileItem? GetDefaultProfile(ref Config config)
    {
        if (config.ProfileItems.Count <= 0)
        {
            return null;
        }

        var index = config.FindIndexId(config.IndexId);
        if (index < 0)
        {
            SetDefaultProfile(config, config.ProfileItems[0]);
            return config.ProfileItems[0];
        }

        return config.ProfileItems[index];
    }

    /// <summary>
    /// 移动配置文件
    /// </summary>
    /// <param name="index"></param>
    /// <param name="eMove"></param>
    /// <returns></returns>
    public static int MoveProfile(int index, MovementTarget eMove, int pos = -1)
    {
        var config = LazyConfig.Instance.Config;
        var lstProfile = config.ProfileItems.OrderBy(it => it.Sort).ToList();
        var count = lstProfile.Count;
        if (index < 0 || index > lstProfile.Count - 1)
        {
            return -1;
        }

        for (int i = 0; i < lstProfile.Count; i++)
        {
            lstProfile[i].Sort = (i + 1) * 10;
        }

        switch (eMove)
        {
            case MovementTarget.Top:
            {
                if (index == 0)
                {
                    return 0;
                }

                lstProfile[index].Sort = lstProfile[0].Sort - 1;

                break;
            }
            case MovementTarget.Up:
            {
                if (index == 0)
                {
                    return 0;
                }

                lstProfile[index].Sort = lstProfile[index - 1].Sort - 1;

                break;
            }

            case MovementTarget.Down:
            {
                if (index == count - 1)
                {
                    return 0;
                }

                lstProfile[index].Sort = lstProfile[index + 1].Sort + 1;

                break;
            }
            case MovementTarget.Bottom:
            {
                if (index == count - 1)
                {
                    return 0;
                }

                lstProfile[index].Sort = lstProfile[lstProfile.Count - 1].Sort + 1;

                break;
            }
            case MovementTarget.Position:
                lstProfile[index].Sort = pos * 10 + 1;
                break;
        }

        ToJsonFile(config);

        return 0;
    }

    private static int AddProfileViaContent(ProfileItem profileItem, string content)
    {
        var config = LazyConfig.Instance.Config;
        if (string.IsNullOrEmpty(content))
        {
            return -1;
        }

        var newFileName = profileItem.Address;
        if (string.IsNullOrEmpty(newFileName))
        {
            const string ext = ".yaml";
            newFileName = $"{Utils.GetGUID()}{ext}";
            profileItem.Address = newFileName;
        }

        if (string.IsNullOrEmpty(profileItem.Remarks))
        {
            profileItem.Remarks = "clash_local_file";
        }

        try
        {
            File.WriteAllText(Path.Combine(Utils.GetConfigPath(), newFileName), content);
        }
        catch
        {
            return -1;
        }

        if (string.IsNullOrEmpty(profileItem.Remarks))
        {
            profileItem.Remarks = $"import custom@{DateTime.Now.ToShortDateString()}";
        }

        profileItem.Enabled = true;
        AddProfileCommon(profileItem);

        ToJsonFile(config);

        return 0;
    }

    public static int AddProfileViaPath(ProfileItem profileItem, string fileName)
    {
        var config = LazyConfig.Instance.Config;
        if (!File.Exists(fileName))
        {
            return -1;
        }

        var ext = Path.GetExtension(fileName);
        var newFileName = $"{Utils.GetGUID()}{ext}";

        try
        {
            File.Copy(fileName, Path.Combine(Utils.GetConfigPath(), newFileName));
            if (!string.IsNullOrEmpty(profileItem.Address))
            {
                File.Delete(Path.Combine(Utils.GetConfigPath(), profileItem.Address));
            }
        }
        catch
        {
            return -1;
        }

        profileItem.Address = newFileName;
        if (string.IsNullOrEmpty(profileItem.Remarks))
        {
            profileItem.Remarks = $"import custom@{DateTime.Now.ToShortDateString()}";
        }

        AddProfileCommon(profileItem);

        ToJsonFile(config);

        return 0;
    }

    public static int EditProfile(ProfileItem profileItem)
    {
        var config = LazyConfig.Instance.Config;
        if (!string.IsNullOrEmpty(profileItem.IndexId) && config.IndexId == profileItem.IndexId)
        {
            Global.reloadCore = true;
        }

        AddProfileCommon(profileItem);

        //TODO auto update via url
        //if (!string.IsNullOrEmpty(profileItem.url))
        //{
        //    var httpClient = new HttpClient();
        //    string result = httpClient.GetStringAsync(profileItem.url).Result;
        //    httpClient.Dispose();
        //    int ret = AddBatchProfiles(ref config, result, profileItem.indexId, profileItem.groupId);
        //}

        ToJsonFile(config);

        return 0;
    }

    public static int SortProfiles(ref Config config, ref List<ProfileItem> lstProfile, EProfileColName name, bool asc)
    {
        if (lstProfile.Count <= 0)
        {
            return -1;
        }

        var propertyName = string.Empty;
        switch (name)
        {
            case EProfileColName.remarks:
            case EProfileColName.url:
            case EProfileColName.testResult:
            case EProfileColName.updateTime:
                propertyName = name.ToString();
                break;

            default:
                return -1;
        }

        var items = lstProfile.AsQueryable();

        if (asc)
        {
            lstProfile = items.OrderBy(propertyName).ToList();
        }
        else
        {
            lstProfile = items.OrderByDescending(propertyName).ToList();
        }

        for (var i = 0; i < lstProfile.Count; i++)
        {
            lstProfile[i].Sort = (i + 1) * 10;
        }

        ToJsonFile(config);
        return 0;
    }

    private static int AddProfileCommon(ProfileItem profileItem)
    {
        if (string.IsNullOrEmpty(profileItem.IndexId))
        {
            profileItem.IndexId = Utils.GetGUID(false);
        }

        if (profileItem.CoreType is null)
        {
            profileItem.CoreType = CoreKind.ClashMeta;
        }

        var config = LazyConfig.Instance.Config;
        if (!config.ProfileItems.Exists(it => it.IndexId == profileItem.IndexId))
        {
            var maxSort = config.ProfileItems.Any() ? config.ProfileItems.Max(t => t.Sort) : 0;
            profileItem.Sort = maxSort++;

            config.ProfileItems.Add(profileItem);
        }

        return 0;
    }

    private static int RemoveProfileItem(Config config, int index)
    {
        try
        {
            if (File.Exists(Utils.GetConfigPath(config.ProfileItems[index].Address)))
            {
                File.Delete(Utils.GetConfigPath(config.ProfileItems[index].Address));
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog("RemoveProfileItem", ex);
        }

        config.ProfileItems.RemoveAt(index);

        return 0;
    }

    public static string GetProfileContent(ProfileItem item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(item.Address))
        {
            return string.Empty;
        }

        var content = File.ReadAllText(Utils.GetConfigPath(item.Address));

        return content;
    }

    public static int AddBatchProfiles(string clipboardData, string indexId, string groupId)
    {
        if (string.IsNullOrEmpty(clipboardData))
        {
            return -1;
        }

        //maybe url
        if (string.IsNullOrEmpty(indexId) && (clipboardData.StartsWith(Global.HttpsProtocol) ||
                                              clipboardData.StartsWith(Global.HttpProtocol)))
        {
            var item = new ProfileItem()
            {
                GroupId = groupId,
                Url = clipboardData,
                CoreType = CoreKind.ClashMeta,
                Address = string.Empty,
                Enabled = true,
                Remarks = "clash_subscription"
            };

            return EditProfile(item);
        }

        //maybe clashProtocol
        if (string.IsNullOrEmpty(indexId) && (clipboardData.StartsWith(Global.ClashProtocol)))
        {
            var url = new Uri(clipboardData);
            if (url.Host == "install-config")
            {
                var query = HttpUtility.ParseQueryString(url.Query);

                if (!string.IsNullOrEmpty(query["url"] ?? ""))
                {
                    var item = new ProfileItem
                    {
                        GroupId = groupId,
                        Url = query["url"] ?? string.Empty,
                        CoreType = CoreKind.ClashMeta,
                        Address = string.Empty,
                        Enabled = true,
                        Remarks = "clash_subscription"
                    };

                    return EditProfile(item);
                }
            }
        }

        //maybe file
        if (File.Exists(clipboardData))
        {
            var item = new ProfileItem
            {
                GroupId = groupId,
                Url = "",
                CoreType = CoreKind.ClashMeta,
                Address = string.Empty,
                Enabled = false,
                Remarks = "clash_local_file"
            };
            return AddProfileViaPath(item, clipboardData);
        }

        //Is Clash configuration
        if (((clipboardData.IndexOf("port") >= 0 && clipboardData.IndexOf("socks-port") >= 0)
             || clipboardData.IndexOf("mixed-port") >= 0)
            && clipboardData.IndexOf("proxies") >= 0
            && clipboardData.IndexOf("rules") >= 0)
        {
        }
        else
        {
            return -1;
        }

        ProfileItem? profileItem = null;
        if (!string.IsNullOrEmpty(indexId))
            profileItem = LazyConfig.Instance.Config.GetProfileItem(indexId);

        if (profileItem == null)
        {
            profileItem = new ProfileItem();
        }

        profileItem.GroupId = groupId;

        if (AddProfileViaContent(profileItem, clipboardData) == 0)
        {
            return 0;
        }
        else
        {
            return -1;
        }
    }

    public static void ClearAllServerStatistics()
    {
        var config = LazyConfig.Instance.Config;
        foreach (var item in config.ProfileItems)
        {
            item.UploadRemote = 0;
            item.DownloadRemote = 0;
        }

        ToJsonFile(config);
    }

    #endregion Profile
}