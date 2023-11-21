using ClashN.Mode;
using System.Runtime.Intrinsics.X86;
using static ClashN.Mode.ClashProxies;

namespace ClashN.Handler;

public sealed class LazyConfig
{
    private static readonly Lazy<LazyConfig> _instance = new(() => new LazyConfig());
    public static LazyConfig Instance => _instance.Value;

    private readonly List<CoreInfo> _coreInfos = new();
    private Dictionary<string, ProxiesItem> _proxies;

    public void SetConfig(Config config)
    {
        Config = config;
    }

    public Config Config { get; private set; }

    public void SetProxies(Dictionary<string, ProxiesItem> proxies)
    {
        _proxies = proxies;
    }

    public Dictionary<string, ProxiesItem> GetProxies()
    {
        return _proxies;
    }

    public Dictionary<string, object> ProfileContent { get; set; }

    public static CoreKind GetCoreType(ProfileItem profileItem)
    {
        if (profileItem != null && profileItem.CoreType != null)
        {
            return (CoreKind)profileItem.CoreType;
        }

        return CoreKind.Clash;
    }

    public CoreInfo GetCoreInfo(CoreKind coreType)
    {
        if (_coreInfos.Count == 0)
        {
            InitCoreInfo();
        }

        return _coreInfos.Where(t => t.CoreType == coreType).FirstOrDefault();
    }

    private void InitCoreInfo()
    {
        _coreInfos.Add(new CoreInfo
        {
            CoreType = CoreKind.ClashN,
            CoreUrl = Global.NUrl,
            CoreLatestUrl = Global.NUrl + "/latest",
            CoreDownloadUrl32 = Global.NUrl + "/download/{0}/clashN-32.zip",
            CoreDownloadUrl64 = Global.NUrl + "/download/{0}/clashN.zip",
        });

        _coreInfos.Add(new CoreInfo
        {
            CoreType = CoreKind.Clash,
            CoreExes = new List<string>
                { "clash-windows-amd64-v3", "clash-windows-amd64", "clash-windows-386", "clash" },
            Arguments = "-f config.yaml",
            CoreUrl = Global.ClashCoreUrl,
            CoreLatestUrl = Global.ClashCoreUrl + "/latest",
            CoreDownloadUrl32 = Global.ClashCoreUrl + "/download/{0}/clash-windows-386-{0}.zip",
            CoreDownloadUrl64 = Global.ClashCoreUrl + "/download/{0}/clash-windows-amd64-{0}.zip",
            Match = "Clash"
        });

        _coreInfos.Add(new CoreInfo
        {
            CoreType = CoreKind.ClashMeta,
            CoreExes = new List<string>
            {
                $"Clash.Meta-windows-amd64{(Avx2.X64.IsSupported ? "" : "-compatible")}",
                "Clash.Meta-windows-amd64-compatible",
                "Clash.Meta-windows-amd64",
                "Clash.Meta-windows-386",
                "Clash.Meta",
                "clash"
            },
            Arguments = "-f config.yaml",
            CoreUrl = Global.ClashMetaCoreUrl,
            CoreLatestUrl = Global.ClashMetaCoreUrl + "/latest",
            CoreDownloadUrl32 = Global.ClashMetaCoreUrl + "/download/{0}/Clash.Meta-windows-386-{0}.zip",
            CoreDownloadUrl64 = Global.ClashMetaCoreUrl + "/download/{0}/Clash.Meta-windows-amd64" +
                                (Avx2.X64.IsSupported ? "" : "-compatible") + "-{0}.zip",
            Match = "Clash Meta"
        });

        _coreInfos.Add(new CoreInfo
        {
            CoreType = CoreKind.ClashPremium,
            CoreExes = new List<string>
                { "clash-windows-amd64-v3", "clash-windows-amd64", "clash-windows-386", "clash" },
            Arguments = "-f config.yaml",
            CoreUrl = Global.ClashCoreUrl,
            CoreLatestUrl = Global.ClashCoreUrl + "/latest",
            CoreDownloadUrl32 = Global.ClashCoreUrl + "/download/{0}/clash-windows-386-{0}.zip",
            CoreDownloadUrl64 = Global.ClashCoreUrl + "/download/{0}/clash-windows-amd64-{0}.zip",
            Match = "Clash"
        });
    }
}