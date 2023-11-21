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
    private static Config _config;
    
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
        _config = LazyConfig.Instance.Config;

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
            ConfigProc.ClearAllServerStatistics(ref _config);
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
            item = _config.GetProfileItem(SelectedSource.IndexId);
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
            int ret = ConfigProc.AddBatchProfiles(ref _config, result, "", "");
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

        var ret = ConfigProc.AddBatchProfiles(ref _config, clipboardData, "", "");
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
        var item = _config.GetProfileItem(SelectedSource.IndexId);
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
            var item = _config.GetProfileItem(SelectedSource.IndexId);
            profileItems = new List<ProfileItem>() { item };
        }

        new UpdateHandle().UpdateSubscriptionProcess(_config, blProxy, profileItems, UpdateUi);
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
        var item = _config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        if (UI.ShowYesNo(ResUI.RemoveProfile) == DialogResult.No)
        {
            return;
        }

        ConfigProc.RemoveProfile(_config, new List<ProfileItem>() { item });

        NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);

        RefreshProfiles();

        Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
    }

    private void CloneProfile()
    {
        var item = _config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            return;
        }

        if (ConfigProc.CopyProfile(ref _config, new List<ProfileItem>() { item }) == 0)
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

        if (SelectedSource?.IndexId == _config.IndexId)
        {
            return;
        }

        var item = _config.GetProfileItem(SelectedSource.IndexId);
        if (item is null)
        {
            NoticeHandler.Instance.Enqueue(ResUI.PleaseSelectProfile);
            return;
        }

        if (ConfigProc.SetDefaultProfile(ref _config, item) == 0)
        {
            NoticeHandler.SendMessage4ClashN(ResUI.OperationSuccess);
            RefreshProfiles();

            Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
        }
    }

    public void RefreshProfiles()
    {
        ConfigProc.SetDefaultProfile(_config, _config.ProfileItems);

        var lstModel = new List<ProfileItemModel>();
        foreach (var item in _config.ProfileItems.OrderBy(it => it.Sort))
        {
            var model = Utils.FromJson<ProfileItemModel>(Utils.ToJson(item));
            model.IsActive = _config.IsActiveNode(item);
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
            if (ConfigProc.MoveProfile(ref _config, startIndex, MovementTarget.Position, targetIndex) == 0)
            {
                RefreshProfiles();
            }
        }
    }

    public async void ProfileQrcode()
    {
        var item = _config.GetProfileItem(SelectedSource.IndexId);
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