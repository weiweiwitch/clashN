namespace ClashN.Mode;


[Serializable]
public class ProfileItem
{
    public ProfileItem()
    {
        indexId = string.Empty;
        sort = 0;
        url = string.Empty;
        enabled = true;
        address = string.Empty;
        remarks = string.Empty;
        testResult = string.Empty;
        groupId = string.Empty;
        enableConvert = false;
    }

    #region function

    public string GetSummary()
    {
        var summary = string.Format("{0}", remarks);
        return summary;
    }

    #endregion function

    public string indexId { get; set; }

    public int sort { get; set; }

    public string address { get; set; }

    public string remarks { get; set; }

    public string testResult { get; set; }

    public string groupId { get; set; } = string.Empty;
    public CoreKind? coreType { get; set; }

    public string url { get; set; }

    public bool enabled { get; set; } = true;

    public string userAgent { get; set; } = string.Empty;

    public bool enableConvert { get; set; }

    public long updateTime { get; set; }
    public ulong uploadRemote { get; set; }
    public ulong downloadRemote { get; set; }
    public ulong totalRemote { get; set; }
    public long expireRemote { get; set; }
}
