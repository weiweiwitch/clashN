using ClashN.Handler;
using ClashN.Mode;
using ClashN.Resx;
using ClashN.Tool;
using ReactiveUI;
using Splat;
using System.Reactive;

namespace ClashN.ViewModels;

public class HelpViewModel : ReactiveObject
{
    private static Config _config;
    private NoticeHandler? _noticeHandler;

    public ReactiveCommand<Unit, Unit> CheckUpdateCmd { get; }
    public ReactiveCommand<Unit, Unit> CheckUpdateClashCoreCmd { get; }
    public ReactiveCommand<Unit, Unit> CheckUpdateClashMetaCoreCmd { get; }

    public HelpViewModel()
    {
        _config = LazyConfig.Instance.Config;
        _noticeHandler = Locator.Current.GetService<NoticeHandler>();

        CheckUpdateCmd = ReactiveCommand.Create(() => { CheckUpdateN(); });
        CheckUpdateClashCoreCmd = ReactiveCommand.Create(() => { CheckUpdateCore(CoreKind.Clash); });
        CheckUpdateClashMetaCoreCmd = ReactiveCommand.Create(() => { CheckUpdateCore(CoreKind.ClashMeta); });
    }

    private void CheckUpdateN()
    {
        new UpdateHandle().CheckUpdateGuiN(_config, UpdateUi);
        return;

        void UpdateUi(bool success, string msg)
        {
            _noticeHandler?.SendMessage4ClashN(msg);
            if (success)
            {
                Locator.Current.GetService<MainWindowViewModel>()?.MyAppExit(false);
            }
        }
    }

    private void CheckUpdateCore(CoreKind type)
    {
        new UpdateHandle().CheckUpdateCore(type, _config, UpdateUi);
        return;

        void UpdateUi(bool success, string msg)
        {
            _noticeHandler?.SendMessage4ClashN(msg);
            if (success)
            {
                Locator.Current.GetService<MainWindowViewModel>()?.CloseCore();

                var fileName = Utils.GetTempPath(Utils.GetDownloadFileName(msg));
                var toPath = Utils.GetBinPath("", type);
                if (FileManager.ZipExtractToFile(fileName, toPath, "") == false)
                {
                    Global.reloadCore = true;
                    _ = Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
                    _noticeHandler?.Enqueue(ResUI.MsgUpdateCoreCoreFailed);
                }
                else
                {
                    _noticeHandler?.Enqueue(ResUI.MsgUpdateCoreCoreSuccessfullyMore);

                    Global.reloadCore = true;
                    _ = Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
                    _noticeHandler?.Enqueue(ResUI.MsgUpdateCoreCoreSuccessfully);
                }
            }
        }
    }
}