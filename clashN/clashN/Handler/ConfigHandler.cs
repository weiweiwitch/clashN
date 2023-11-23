using System.IO;
using System.Web;
using ClashN.Mode;
using ClashN.Tool;

namespace ClashN.Handler;

/// <summary>
/// 本软件配置文件处理类
/// </summary>
internal static class ConfigHandler
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
            Global.ReloadCore = false;
        }
        else
        {
            Global.ReloadCore = true;

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
        Global.ReloadCore = reload;

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
    /// <param name="indexs"></param>
    /// <returns></returns>
    public static int RemoveProfile(List<ProfileItem> indexs)
    {
        var config = LazyConfig.Instance.Config;
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
    /// <param name="item"></param>
    /// <returns></returns>
    public static int ActiveSpecialProfile(ProfileItem item)
    {
        var config = LazyConfig.Instance.Config;
        config.IndexId = item.IndexId;
        
        Global.ReloadCore = true;

        ToJsonFile(config);

        return 0;
    }

    public static void ChooseOneActiveProfile()
    {
        var config = LazyConfig.Instance.Config;
        var lstProfile = config.ProfileItems;
        if (lstProfile.Exists(t => t.IndexId == config.IndexId))
        {
            return;
        }

        if (lstProfile.Count > 0)
        {
            ActiveSpecialProfile(lstProfile[0]);
        }
    }

    public static ProfileItem? GetActiveProfile()
    {
        var config = LazyConfig.Instance.Config;
        var profileItems = config.ProfileItems;
        if (profileItems.Count <= 0)
        {
            return null;
        }
        
        var index = config.FindIndexId(config.IndexId);
        if (index < 0)
        {
            ActiveSpecialProfile(profileItems[0]);
            return profileItems[0];
        }

        return profileItems[index];
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

    public static void AddOrModifyProfile(ProfileItem profileItem)
    {
        var config = LazyConfig.Instance.Config;
        var item = config.GetProfileItem(profileItem.IndexId);
        if (item is null)
        {
            AddProfileCommon(profileItem);
        }
        else
        {
            // override
            item.Remarks = profileItem.Remarks;
            item.Url = profileItem.Url;
            item.Address = profileItem.Address;
            item.UserAgent = profileItem.UserAgent;
            item.CoreType = profileItem.CoreType;
            item.Enabled = profileItem.Enabled;
            item.EnableConvert = profileItem.EnableConvert;

            EditProfile(item);
        }
    }
    
    public static void EditProfile(ProfileItem profileItem)
    {
        var config = LazyConfig.Instance.Config;
        if (!string.IsNullOrEmpty(profileItem.IndexId) && config.IndexId == profileItem.IndexId)
        {
            // current profile has been modified, reload core
            Global.ReloadCore = true;
        }

        AddProfileCommon(profileItem);

        ToJsonFile(config);
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

    private static void AddProfileCommon(ProfileItem profileItem)
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
    }

    private static void RemoveProfileItem(Config config, int index)
    {
        try
        {
            var configPath = Utils.GetConfigPath(config.ProfileItems[index].Address);
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog("RemoveProfileItem", ex);
        }

        config.ProfileItems.RemoveAt(index);
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

             EditProfile(item);

             return 0;
        }

        //maybe clashProtocol
        if (string.IsNullOrEmpty(indexId) && clipboardData.StartsWith(Global.ClashProtocol))
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

                    EditProfile(item);
                    
                    return 0;
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
        if (((clipboardData.Contains("port") && clipboardData.Contains("socks-port")) ||
             clipboardData.Contains("mixed-port"))
            && clipboardData.Contains("proxies")
            && clipboardData.Contains("rules"))
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