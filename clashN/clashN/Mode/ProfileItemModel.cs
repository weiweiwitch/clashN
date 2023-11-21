using ClashN.Tool;

namespace ClashN.Mode;

public class ProfileItemModel : ProfileItem
{
    public bool IsActive { get; set; }
    public bool HasUrl => !string.IsNullOrEmpty(Url);
    public bool HasAddress => !string.IsNullOrEmpty(Address);

    public string StrUpdateTime
    {
        get
        {
            if (UpdateTime <= 0)
            {
                return string.Empty;
            }
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTime.AddSeconds(UpdateTime).ToLocalTime().ToString("MM-dd HH:mm");
        }
    }

    public string TrafficUsed => Utils.HumanFy(UploadRemote + DownloadRemote);
    public string TrafficTotal => TotalRemote <= 0 ? "∞" : Utils.HumanFy(TotalRemote);

    public string StrExpireTime
    {
        get
        {
            if (ExpireRemote <= 0)
            {
                return string.Empty;
            }
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTime.AddSeconds(ExpireRemote).ToLocalTime().ToString("yyyy-MM-dd");
        }
    }
}