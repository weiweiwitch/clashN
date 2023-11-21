namespace ClashN.Mode;

[Serializable]
public class CoreInfo
{
    public CoreKind CoreType { get; set; }

    public List<string> CoreExes { get; set; }

    public string Arguments { get; set; }

    public string CoreUrl { get; set; }

    public string CoreLatestUrl { get; set; }

    public string CoreDownloadUrl32 { get; set; }

    public string CoreDownloadUrl64 { get; set; }

    public string Match { get; set; }
}