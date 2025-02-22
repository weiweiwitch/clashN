using ClashN.Handler;
using ClashN.Mode;
using ClashN.Resx;
using ClashN.Views;
using DynamicData;
using DynamicData.Binding;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System.IO;
using System.Reactive;
using System.Windows.Forms;
using ClashN.Tool;
using Application = System.Windows.Application;

namespace ClashN.ViewModels;

public class ProfilesViewModel : ReactiveObject
{
    private IObservableCollection<ProfileItemModel>
        _profileItems = new ObservableCollectionExtended<ProfileItemModel>();

    public IObservableCollection<ProfileItemModel> ProfileItems => _profileItems;

    [Reactive] public ProfileItemModel? SelectedSource { get; set; }

    public ReactiveCommand<Unit, Unit> EditLocalFileCmd { get; }
    public ReactiveCommand<Unit, Unit> EditProfileCmd { get; }
    public ReactiveCommand<Unit, Unit> AddProfileCmd { get; }
    public ReactiveCommand<Unit, Unit> AddProfileViaScanCmd { get; }
    public ReactiveCommand<Unit, Unit> AddProfileViaClipboardCmd { get; }
    public ReactiveCommand<Unit, Unit> ExportProfileCmd { get; }

    public ReactiveCommand<Unit, Unit> SubUpdateCmd { get; }
    public ReactiveCommand<Unit, Unit> SubUpdateSelectedCmd { get; }
    public ReactiveCommand<Unit, Unit> SubUpdateViaProxyCmd { get; }
    public ReactiveCommand<Unit, Unit> SubUpdateSelectedViaProxyCmd { get; }

    public ReactiveCommand<Unit, Unit> RemoveProfileCmd { get; }
    public ReactiveCommand<Unit, Unit> CloneProfileCmd { get; }
    public ReactiveCommand<Unit, Unit> ActiveSpecialProfileCmd { get; }
    public ReactiveCommand<Unit, Unit> ClearStatisticCmd { get; }
    public ReactiveCommand<Unit, Unit> ProfileReloadCmd { get; }
    public ReactiveCommand<Unit, Unit> ProfileQrcodeCmd { get; }

    public ProfilesViewModel()
    {
        SelectedSource = new ProfileItemModel();

        var canEditRemove = this.WhenAnyValue(
            x => x.SelectedSource,
            selectedSource => selectedSource != null && !string.IsNullOrEmpty(selectedSource.IndexId));

        //Profile
        EditLocalFileCmd = ReactiveCommand.Create(EditLocalFile, canEditRemove);
        EditProfileCmd = ReactiveCommand.Create(EditProfile, canEditRemove);
        AddProfileCmd = ReactiveCommand.Create(AddProfile);
        AddProfileViaScanCmd = ReactiveCommand.Create(ScanScreenTaskAsync);
        AddProfileViaClipboardCmd = ReactiveCommand.Create(() => { AddProfilesViaClipboard(false); });

        ExportProfileCmd = ReactiveCommand.Create(ExportProfile2Clipboard, canEditRemove);

        //Subscription
        SubUpdateCmd = ReactiveCommand.Create(() => { UpdateSubscriptionProcess(false, false); });
        SubUpdateSelectedCmd = ReactiveCommand.Create(() => { UpdateSubscriptionProcess(false, true); }, canEditRemove);
        SubUpdateViaProxyCmd = ReactiveCommand.Create(() => { UpdateSubscriptionProcess(true, false); });
        SubUpdateSelectedViaProxyCmd =
            ReactiveCommand.Create(() => { UpdateSubscriptionProcess(true, true); }, canEditRemove);

        //Profile other
        RemoveProfileCmd = ReactiveCommand.Create(RemoveProfile, canEditRemove);
        CloneProfileCmd = ReactiveCommand.Create(CloneProfile, canEditRemove);
        ActiveSpecialProfileCmd = ReactiveCommand.Create(ActiveSpecialProfile, canEditRemove);

        ClearStatisticCmd = ReactiveCommand.Create(ClearStatistic);
        ProfileReloadCmd = ReactiveCommand.Create(RefreshProfiles);
        ProfileQrcodeCmd = ReactiveCommand.Create(ProfileQrcode, canEditRemove);

        // Logic
        RefreshProfiles();

        Utils.SaveLogDebug("ProfilesViewModel:ProfilesViewModel - Finished");
    }

    private void EditLocalFile()
    {
        var address = SelectedSource?.Address;
        if (string.IsNullOrEmpty(address))
        {
            NoticeHandler.Instance.Enqueue(ResUI.FillProfileAddressCustom);
            return;
        }

        address = Path.Combine(Utils.GetConfigPath(), address);
        if (!File.Exists(address))
        {
            NoticeHandler.Instance.Enqueue(ResUI.FailedReadConfiguration);
            return;
        }

        Utils.ViewOrEditConfigFileOutside(address);
    }

    private void AddProfile()
    {
        var item = new ProfileItem
        {
            CoreType = CoreKind.ClashMeta
        };

        var dialog = new ProfileEditWindow(item)
        {
            Owner = Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() == true)
        {
            RefreshProfiles();
        }
    }

    private void EditProfile()
    {
        if (SelectedSource == null)
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        var item = config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        var dialog = new ProfileEditWindow(item)
        {
            Owner = Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() == true)
        {
            RefreshProfiles();
        }
    }

    public void ScanScreenTaskAsync()
    {
        MainWindowViewModel.ShowHideWindow(false);

        var result = Utils.ScanScreen();

        MainWindowViewModel.ShowHideWindow(true);

        if (string.IsNullOrEmpty(result))
        {
            NoticeHandler.Instance.Enqueue(ResUI.NoValidQRcodeFound);
        }
        else
        {
            var ret = ConfigHandler.AddBatchProfiles(result, "", "");
            if (ret == 0)
            {
                RefreshProfiles();

                NoticeHandler.Instance.Enqueue(ResUI.SuccessfullyImportedProfileViaScan);
            }
        }
    }

    public void AddProfilesViaClipboard(bool bClear)
    {
        var clipboardData = Utils.GetClipboardData();
        if (string.IsNullOrEmpty(clipboardData))
        {
            return;
        }

        var ret = ConfigHandler.AddBatchProfiles(clipboardData, "", "");
        if (ret == 0)
        {
            if (bClear)
            {
                Utils.SetClipboardData(String.Empty);
            }

            RefreshProfiles();

            NoticeHandler.Instance.Enqueue(ResUI.SuccessfullyImportedProfileViaClipboard);
        }
    }

    public void ExportProfile2Clipboard()
    {
        if (SelectedSource == null)
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        var item = config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        var content = ConfigHandler.GetProfileContent(item);
        if (string.IsNullOrEmpty(content))
        {
            content = item.Url;
        }

        Utils.SetClipboardData(content);

        NoticeHandler.Instance.Enqueue(ResUI.BatchExportSuccessfully);
    }

    public void UpdateSubscriptionProcess(bool blProxy, bool blSelected)
    {
        var profileItems = new List<ProfileItem>();
        if (blSelected)
        {
            var config = LazyConfig.Instance.Config;
            if (SelectedSource != null)
            {
                var item = config.GetProfileItem(SelectedSource.IndexId);
                if (item != null)
                {
                    profileItems = new List<ProfileItem> { item };
                }
            }
        }

        UpdateHandle.UpdateSubscriptionProcess(blProxy, profileItems, (success, msg) =>
        {
            NoticeHandler.SendMessage4ClashN(msg);

            if (success)
            {
                Utils.SaveLog(
                    $"ProfilesViewModel:UpdateSubscriptionProcess - UpdateSubscriptionProcess Finished: {msg}");

                Application.Current.Dispatcher.Invoke((Action)RefreshProfiles);
            }
        });
    }

    public void RemoveProfile()
    {
        if (SelectedSource == null)
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        var item = config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        if (UI.ShowYesNo(ResUI.RemoveProfile) == DialogResult.No)
        {
            return;
        }

        ConfigHandler.RemoveProfile(new List<ProfileItem> { item });

        NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);

        RefreshProfiles();

        Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
    }

    private void CloneProfile()
    {
        if (SelectedSource == null)
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        var item = config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        if (ConfigHandler.CopyProfile(new List<ProfileItem> { item }) == 0)
        {
            NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);

            RefreshProfiles();
        }
    }

    public void ActiveSpecialProfile()
    {
        if (SelectedSource == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(SelectedSource.IndexId))
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        if (SelectedSource.IndexId == config.IndexId)
        {
            return;
        }

        var item = config.GetProfileItem(SelectedSource.IndexId);
        if (item == null)
        {
            NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectProfile);
            return;
        }

        if (ConfigHandler.ActiveSpecialProfile(item) == 0)
        {
            NoticeHandler.SendMessage4ClashN(ResUI.OperationSuccess);

            RefreshProfiles();

            Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
        }
    }

    private void ClearStatistic()
    {
        ConfigHandler.ClearAllServerStatistics();

        RefreshProfiles();
    }

    public void RefreshProfiles()
    {
        Utils.SaveLog("ProfilesViewModel:RefreshProfiles - Start");

        var config = LazyConfig.Instance.Config;
        ConfigHandler.ChooseOneActiveProfile();

        var lstModel = new List<ProfileItemModel>();
        foreach (var item in config.ProfileItems.OrderBy(it => it.Sort))
        {
            var model = Utils.FromJson<ProfileItemModel>(Utils.ToJson(item));
            model.IsActive = config.IsActiveNode(item);
            lstModel.Add(model);
        }

        _profileItems.Clear();
        _profileItems.AddRange(lstModel);

        Utils.SaveLogDebug($"ProfilesViewModel:RefreshProfiles - Finished");
    }

    public void MoveProfile(int startIndex, ProfileItemModel targetItem)
    {
        var targetIndex = _profileItems.IndexOf(targetItem);
        if (startIndex >= 0 && targetIndex >= 0 && startIndex != targetIndex)
        {
            if (ConfigHandler.MoveProfile(startIndex, MovementTarget.Position, targetIndex) == 0)
            {
                RefreshProfiles();
            }
        }
    }

    private async void ProfileQrcode()
    {
        if (SelectedSource == null)
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        var item = config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(item.Url))
        {
            return;
        }

        var img = QRCodeHelper.GetQRCode(item.Url);
        var dialog = new ProfileQrcodeView()
        {
            ImgQrcode = { Source = img },
            TxtContent = { Text = item.Url },
        };

        await DialogHost.Show(dialog, "RootDialog");
    }
}