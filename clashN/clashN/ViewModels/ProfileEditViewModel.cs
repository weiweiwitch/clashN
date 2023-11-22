using System.IO;
using System.Reactive;
using System.Windows.Forms;
using ClashN.Handler;
using ClashN.Mode;
using ClashN.Resx;
using ClashN.Tool;
using ClashN.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Helpers;
using Splat;

namespace ClashN.ViewModels;

public class ProfileEditViewModel : ReactiveValidationObject
{
    private ProfileEditWindow _view;

    [Reactive] public ProfileItem SelectedSource { get; set; }

    [Reactive] public string CoreType { get; set; }

    public ReactiveCommand<Unit, Unit> BrowseProfileCmd { get; }
    public ReactiveCommand<Unit, Unit> EditProfileCmd { get; }
    public ReactiveCommand<Unit, Unit> SaveProfileCmd { get; }

    public ProfileEditViewModel(ProfileItem profileItem, ProfileEditWindow view)
    {
        if (string.IsNullOrEmpty(profileItem.IndexId))
        {
            SelectedSource = profileItem;
        }
        else
        {
            SelectedSource = Utils.DeepCopy(profileItem);
        }

        _view = view;
        CoreType = (SelectedSource.CoreType ?? CoreKind.Clash).ToString();

        BrowseProfileCmd = ReactiveCommand.Create(() => { BrowseProfile(); });

        EditProfileCmd = ReactiveCommand.Create(() => { EditProfile(); });

        SaveProfileCmd = ReactiveCommand.Create(() => { SaveProfile(); });

        Utils.SetDarkBorder(view, LazyConfig.Instance.Config.UiItem.ColorModeDark);
    }

    private void SaveProfile()
    {
        var remarks = SelectedSource.Remarks;
        if (string.IsNullOrEmpty(remarks))
        {
            NoticeHandler.Instance.Enqueue(ResUI.PleaseFillRemarks);
            return;
        }

        if (string.IsNullOrEmpty(CoreType))
        {
            SelectedSource.CoreType = null;
        }
        else
        {
            SelectedSource.CoreType = (CoreKind)Enum.Parse(typeof(CoreKind), CoreType);
        }

        var item = LazyConfig.Instance.Config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            item = SelectedSource;
        }
        else
        {
            item.Remarks = SelectedSource.Remarks;
            item.Url = SelectedSource.Url;
            item.Address = SelectedSource.Address;
            item.UserAgent = SelectedSource.UserAgent;
            item.CoreType = SelectedSource.CoreType;
            item.Enabled = SelectedSource.Enabled;
            item.EnableConvert = SelectedSource.EnableConvert;
        }

        if (ConfigProc.EditProfile(item) == 0)
        {
            Locator.Current.GetService<ProfilesViewModel>()?.RefreshProfiles();
            NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
            _view?.Close();
        }
        else
        {
            NoticeHandler.Instance.Enqueue(ResUI.OperationFailed);
        }
    }

    private void BrowseProfile()
    {
        var fileDialog = new OpenFileDialog
        {
            Multiselect = false,
            Filter = "YAML|*.yaml;*.yml|All|*.*"
        };

        var parent = App.Current.MainWindow.WpfWindow2WinFormWin32Window();
        if (fileDialog.ShowDialog(parent) != DialogResult.OK)
        {
            return;
        }

        if (UI.ShowYesNo(ResUI.MsgSureContinue) == DialogResult.No)
        {
            return;
        }

        var fileName = fileDialog.FileName;
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var item = LazyConfig.Instance.Config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            item = SelectedSource;
        }

        if (ConfigProc.AddProfileViaPath(item, fileName) == 0)
        {
            NoticeHandler.Instance.Enqueue(ResUI.SuccessfullyImportedCustomProfile);
            Locator.Current.GetService<ProfilesViewModel>()?.RefreshProfiles();
            _view?.Close();
        }
        else
        {
            NoticeHandler.Instance.Enqueue(ResUI.FailedImportedCustomProfile);
        }
    }

    private void EditProfile()
    {
        var address = SelectedSource.Address;
        if (string.IsNullOrEmpty(address))
        {
            NoticeHandler.Instance.Enqueue(ResUI.FillProfileAddressCustom);
            return;
        }

        address = Path.Combine(Utils.GetConfigPath(), address);
        if (File.Exists(address))
        {
            Utils.ProcessStart(address);
        }
        else
        {
            NoticeHandler.Instance.Enqueue(ResUI.FailedReadConfiguration);
        }
    }
}