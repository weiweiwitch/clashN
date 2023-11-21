namespace ClashN.Mode;


[Serializable]
public class ProfileItem
{
    public ProfileItem()
    {
        IndexId = string.Empty;
        Sort = 0;
        Url = string.Empty;
        Enabled = true;
        Address = string.Empty;
        Remarks = string.Empty;
        TestResult = string.Empty;
        GroupId = string.Empty;
        EnableConvert = false;
    }

    #region function

    public string GetSummary()
    {
        var summary = $"{Remarks}";
        return summary;
    }

    #endregion function

    public string IndexId { get; set; }

    public int Sort { get; set; }

    public string Address { get; set; }

    public string Remarks { get; set; }

    public string TestResult { get; set; }

    public string GroupId { get; set; } = string.Empty;
    public CoreKind? CoreType { get; set; }

    public string Url { get; set; }

    public bool Enabled { get; set; } = true;

    public string UserAgent { get; set; } = string.Empty;

    public bool EnableConvert { get; set; }

    public long UpdateTime { get; set; }
    public ulong UploadRemote { get; set; }
    public ulong DownloadRemote { get; set; }
    public ulong TotalRemote { get; set; }
    public long ExpireRemote { get; set; }
}
