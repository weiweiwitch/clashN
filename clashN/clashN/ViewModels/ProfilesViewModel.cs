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
    public ReactiveCommand<Unit, Unit> SetDefaultProfileCmd { get; }
    public ReactiveCommand<Unit, Unit> ClearStatisticCmd { get; }
    public ReactiveCommand<Unit, Unit> ProfileReloadCmd { get; }
    public ReactiveCommand<Unit, Unit> ProfileQrcodeCmd { get; }

    [Reactive] public ProfileItemModel SelectedSource { get; set; }

    public ProfilesViewModel()
    {
        SelectedSource = new();

        RefreshProfiles();

        var canEditRemove = this.WhenAnyValue(
            x => x.SelectedSource,
            selectedSource => selectedSource != null && !string.IsNullOrEmpty(selectedSource.IndexId));

        //Profile
        EditLocalFileCmd = ReactiveCommand.Create(() => { EditLocalFile(); }, canEditRemove);

        EditProfileCmd = ReactiveCommand.Create(() => { EditProfile(false); }, canEditRemove);

        AddProfileCmd = ReactiveCommand.Create(() => { EditProfile(true); });
        AddProfileViaScanCmd = ReactiveCommand.CreateFromTask(() => { return ScanScreenTaskAsync(); });
        AddProfileViaClipboardCmd = ReactiveCommand.Create(() => { AddProfilesViaClipboard(false); });

        ExportProfileCmd = ReactiveCommand.Create(() => { ExportProfile2Clipboard(); }, canEditRemove);

        //Subscription
        SubUpdateCmd = ReactiveCommand.Create(() => { UpdateSubscriptionProcess(false, false); });
        SubUpdateSelectedCmd = ReactiveCommand.Create(() => { UpdateSubscriptionProcess(false, true); }, canEditRemove);
        SubUpdateViaProxyCmd = ReactiveCommand.Create(() => { UpdateSubscriptionProcess(true, false); });
        SubUpdateSelectedViaProxyCmd =
            ReactiveCommand.Create(() => { UpdateSubscriptionProcess(true, true); }, canEditRemove);

        //Profile other
        RemoveProfileCmd = ReactiveCommand.Create(() => { RemoveProfile(); }, canEditRemove);
        CloneProfileCmd = ReactiveCommand.Create(() => { CloneProfile(); }, canEditRemove);
        SetDefaultProfileCmd = ReactiveCommand.Create(() => { SetDefaultProfile(); }, canEditRemove);

        ClearStatisticCmd = ReactiveCommand.Create(() =>
        {
            ConfigProc.ClearAllServerStatistics();
            RefreshProfiles();
        });
        ProfileReloadCmd = ReactiveCommand.Create(() => { RefreshProfiles(); });
        ProfileQrcodeCmd = ReactiveCommand.Create(() => { ProfileQrcode(); }, canEditRemove);
    }

    private void EditLocalFile()
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

    public void EditProfile(bool blNew)
    {
        ProfileItem item;
        if (blNew)
        {
            item = new()
            {
                CoreType = CoreKind.ClashMeta
            };
        }
        else
        {
            item = LazyConfig.Instance.Config.GetProfileItem(SelectedSource.IndexId);
            if (item is null)
            {
                return;
            }
        }

        ProfileEditWindow dialog = new ProfileEditWindow(item)
        {
            Owner = App.Current.MainWindow,
        };

        if (dialog.ShowDialog() == true)
        {
            RefreshProfiles();
        }
    }

    public async Task ScanScreenTaskAsync()
    {
        MainWindowViewModel.ShowHideWindow(false);

        var result = await Task.Run(() => { return Utils.ScanScreen(); });

        MainWindowViewModel.ShowHideWindow(true);

        if (string.IsNullOrEmpty(result))
        {
            NoticeHandler.Instance.Enqueue(ResUI.NoValidQRcodeFound);
        }
        else
        {
            int ret = ConfigProc.AddBatchProfiles(result, "", "");
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

        var ret = ConfigProc.AddBatchProfiles(clipboardData, "", "");
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
        var item = LazyConfig.Instance.Config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        var content = ConfigProc.GetProfileContent(item);
        if (string.IsNullOrEmpty(content))
        {
            content = item.Url;
        }

        Utils.SetClipboardData(content);

        NoticeHandler.Instance.Enqueue(ResUI.BatchExportSuccessfully);
    }

    public void UpdateSubscriptionProcess(bool blProxy, bool blSelected)
    {
        List<ProfileItem> profileItems = null;
        if (blSelected)
        {
            var item = LazyConfig.Instance.Config.GetProfileItem(SelectedSource.IndexId);
            profileItems = new List<ProfileItem>() { item };
        }

        new UpdateHandle().UpdateSubscriptionProcess(blProxy, profileItems, UpdateUi);
        return;

        void UpdateUi(bool success, string msg)
        {
            NoticeHandler.SendMessage4ClashN(msg);

            if (success)
            {
                RefreshProfiles();
            }
        }
    }

    public void RemoveProfile()
    {
        var item = LazyConfig.Instance.Config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        if (UI.ShowYesNo(ResUI.RemoveProfile) == DialogResult.No)
        {
            return;
        }

        ConfigProc.RemoveProfile(LazyConfig.Instance.Config, new List<ProfileItem>() { item });

        NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);

        RefreshProfiles();

        Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
    }

    private void CloneProfile()
    {
        var item = LazyConfig.Instance.Config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        if (ConfigProc.CopyProfile(new List<ProfileItem>() { item }) == 0)
        {
            NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
            RefreshProfiles();
        }
    }

    public void SetDefaultProfile()
    {
        if (string.IsNullOrEmpty(SelectedSource?.IndexId))
        {
            return;
        }

        var config = LazyConfig.Instance.Config;
        if (SelectedSource?.IndexId == config.IndexId)
        {
            return;
        }

        var item = config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectProfile);
            return;
        }

        if (ConfigProc.SetDefaultProfile(config, item) == 0)
        {
            NoticeHandler.SendMessage4ClashN(ResUI.OperationSuccess);
            RefreshProfiles();

            Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
        }
    }

    public void RefreshProfiles()
    {
        Utils.SaveLogDebug($"ProfilesViewModel:RefreshProfiles - Start");
        
        var config = LazyConfig.Instance.Config;
        ConfigProc.SetDefaultProfile(config, config.ProfileItems);

        var lstModel = new List<ProfileItemModel>();
        foreach (var item in config.ProfileItems.OrderBy(it => it.Sort))
        {
            var model = Utils.FromJson<ProfileItemModel>(Utils.ToJson(item));
            model.IsActive = config.IsActiveNode(item);
            lstModel.Add(model);
        }

        Application.Current.Dispatcher.Invoke((Action)(() =>
        {
            _profileItems.Clear();
            _profileItems.AddRange(lstModel);
        }));
    }

    public void MoveProfile(int startIndex, ProfileItemModel targetItem)
    {
        var targetIndex = _profileItems.IndexOf(targetItem);
        if (startIndex >= 0 && targetIndex >= 0 && startIndex != targetIndex)
        {
            if (ConfigProc.MoveProfile(startIndex, MovementTarget.Position, targetIndex) == 0)
            {
                RefreshProfiles();
            }
        }
    }

    public async void ProfileQrcode()
    {
        var item = LazyConfig.Instance.Config.GetProfileItem(SelectedSource.IndexId);
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