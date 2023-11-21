using System.Drawing;

namespace ClashN.Mode;


[Serializable]
public class UIItem
{
    public Point mainLocation { get; set; }

    public double mainWidth { get; set; }
    public double mainHeight { get; set; }

    public bool colorModeDark { get; set; }
    public string? colorPrimaryName { get; set; }
    public string currentFontFamily { get; set; } = string.Empty;
    public int currentFontSize { get; set; }

    public int proxiesSorting { get; set; }
    public bool proxiesAutoRefresh { get; set; }

    public int connectionsSorting { get; set; }
    public bool connectionsAutoRefresh { get; set; }
}