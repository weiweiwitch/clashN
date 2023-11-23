using System.Drawing;

namespace ClashN.Mode;

/// <summary>
/// 本软件配置文件实体类
/// </summary>
[Serializable]
public class Config
{
    #region property

    public int MixedPort { get; set; } = 7888;

    public int HttpPort { get; set; } = 7890;

    public int SocksPort { get; set; } = 7891;

    public int ApiPort { get; set; } = 9090;

    public string LogLevel { get; set; }

    public bool EnableIpv6 { get; set; }

    public string IndexId { get; set; }

    public SysProxyType SysProxyType { get; set; }

    public ERuleMode RuleMode { get; set; }

    public bool AllowLANConn { get; set; }

    public bool AutoRun { get; set; }

    public bool EnableStatistics { get; set; }

    public string SystemProxyExceptions { get; set; }
    public string SystemProxyAdvancedProtocol { get; set; }

    public int AutoUpdateSubInterval { get; set; } = 10;
    public int AutoDelayTestInterval { get; set; } = 10;

    public bool EnableSecurityProtocolTls13 { get; set; }

    public bool EnableMixinContent { get; set; }

    public int PacPort { get; set; }

    public bool AutoHideStartup { get; set; }

    public bool EnableTun { get; set; }

    #endregion property

    #region other entities

    public List<ProfileItem> ProfileItems { get; } = new();

    public UIItem UiItem { get; set; } = new();

    public ConstItem ConstItem { get; set; } = new();

    public List<KeyShortcut> GlobalHotkeys { get; } = new();

    #endregion other entities

    #region function

    public int FindIndexId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return -1;
        }

        return ProfileItems.FindIndex(it => it.IndexId == id);
    }

    public ProfileItem? GetProfileItem(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return ProfileItems.FirstOrDefault(it => it.IndexId == id);
    }

    public bool IsActiveNode(ProfileItem item)
    {
        if (!string.IsNullOrEmpty(item.IndexId) && item.IndexId == IndexId)
        {
            return true;
        }

        return false;
    }

    #endregion function
}