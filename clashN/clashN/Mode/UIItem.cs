using System.Drawing;

namespace ClashN.Mode;


[Serializable]
public class UIItem
{
    public Point MainLocation { get; set; }

    public double MainWidth { get; set; }
    public double MainHeight { get; set; }

    public bool ColorModeDark { get; set; }
    public string? ColorPrimaryName { get; set; }
    public string CurrentFontFamily { get; set; } = string.Empty;
    public int CurrentFontSize { get; set; }

    public int ProxiesSorting { get; set; }
    public bool ProxiesAutoRefresh { get; set; }

    public bool ConnectionsAutoRefresh { get; set; }
}