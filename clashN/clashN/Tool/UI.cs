using System.Windows.Forms;

namespace ClashN.Tool;

internal class UI
{
    public static void Show(string msg)
    {
        MessageBox.Show(msg, "ClashN", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static void ShowWarning(string msg)
    {
        MessageBox.Show(msg, "ClashN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void ShowError(string msg)
    {
        MessageBox.Show(msg, "ClashN", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static DialogResult ShowYesNo(string msg)
    {
        return MessageBox.Show(msg, "ClashN", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    }
    
}