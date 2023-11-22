using ClashN.Handler;
using ClashN.Mode;
using ClashN.Resx;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClashN.Tool;
using Forms = System.Windows.Forms;

namespace ClashN.Views;

/// <summary>
/// GlobalHotkeySettingWindow.xaml 的交互逻辑
/// </summary>
public partial class GlobalHotkeySettingWindow
{
    private List<KeyShortcut> lstKey;

    public GlobalHotkeySettingWindow()
    {
        InitializeComponent();

        var config = LazyConfig.Instance.Config;

        foreach (GlobalHotkeyAction it in Enum.GetValues(typeof(GlobalHotkeyAction)))
        {
            if (config.GlobalHotkeys.FindIndex(t => t.GlobalHotkey == it) >= 0)
            {
                continue;
            }

            config.GlobalHotkeys.Add(new KeyShortcut()
            {
                GlobalHotkey = it,
                Alt = false,
                Control = false,
                Shift = false,
                KeyCode = null
            });
        }

        lstKey = Utils.DeepCopy(config.GlobalHotkeys);

        TxtGlobalHotkey0.KeyDown += TxtGlobalHotkey_KeyDown;
        TxtGlobalHotkey1.KeyDown += TxtGlobalHotkey_KeyDown;
        TxtGlobalHotkey2.KeyDown += TxtGlobalHotkey_KeyDown;
        TxtGlobalHotkey3.KeyDown += TxtGlobalHotkey_KeyDown;
        TxtGlobalHotkey4.KeyDown += TxtGlobalHotkey_KeyDown;

        BindingData(-1);

        Utils.SetDarkBorder(this, config.UiItem.ColorModeDark);
    }

    private void TxtGlobalHotkey_KeyDown(object sender, KeyEventArgs e)
    {
        var txt = ((TextBox)sender);
        var index = Utils.ToInt(txt.Name.Substring(txt.Name.Length - 1, 1));

        if (e.Key == Key.System)
            return;
        var formsKey = (Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);

        lstKey[index] = new KeyShortcut()
        {
            KeyCode = formsKey,
            Alt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt),
            Control = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl),
            Shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift),
        };

        BindingData(index);
    }

    private void BindingData(int index)
    {
        for (int k = 0; k < lstKey.Count; k++)
        {
            if (index >= 0 && index != k)
            {
                continue;
            }

            var item = lstKey[k];
            var keys = string.Empty;

            if (item.Control)
            {
                keys += $"{Forms.Keys.Control.ToString()} + ";
            }

            if (item.Alt)
            {
                keys += $"{Forms.Keys.Alt.ToString()} + ";
            }

            if (item.Shift)
            {
                keys += $"{Forms.Keys.Shift.ToString()} + ";
            }

            if (item.KeyCode != null)
            {
                keys += $"{item.KeyCode.ToString()}";
            }

            SetText($"txtGlobalHotkey{k}", keys);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var config = LazyConfig.Instance.Config;
        config.GlobalHotkeys.Clear();
        config.GlobalHotkeys.AddRange(lstKey);

        if (ConfigHandler.SaveConfig(false) == 0)
        {
            Close();
        }
        else
        {
            UI.ShowWarning(ResUI.OperationFailed);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        lstKey.Clear();
        foreach (GlobalHotkeyAction it in Enum.GetValues(typeof(GlobalHotkeyAction)))
        {
            if (lstKey.FindIndex(t => t.GlobalHotkey == it) >= 0)
            {
                continue;
            }

            lstKey.Add(new KeyShortcut()
            {
                GlobalHotkey = it,
                Alt = false,
                Control = false,
                Shift = false,
                KeyCode = null
            });
        }

        BindingData(-1);
    }

    private void SetText(string name, string txt)
    {
        foreach (UIElement element in GridText.Children)
        {
            if (element is TextBox)
            {
                if (((TextBox)element).Name == name)
                {
                    ((TextBox)element).Text = txt;
                }
            }
        }
    }

    private void GlobalHotkeySettingWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.Close();
        }
    }
}