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

    public ReactiveCommand<Unit, Unit> CheckUpdateCmd { get; }
    public ReactiveCommand<Unit, Unit> CheckUpdateClashCoreCmd { get; }
    public ReactiveCommand<Unit, Unit> CheckUpdateClashMetaCoreCmd { get; }

    public HelpViewModel()
    {
        CheckUpdateCmd = ReactiveCommand.Create(() => { CheckUpdateN(); });
        CheckUpdateClashCoreCmd = ReactiveCommand.Create(() => { CheckUpdateCore(CoreKind.Clash); });
        CheckUpdateClashMetaCoreCmd = ReactiveCommand.Create(() => { CheckUpdateCore(CoreKind.ClashMeta); });
    }

    private void CheckUpdateN()
    {
        new UpdateHandle().CheckUpdateGuiN(UpdateUi);
        return;

        void UpdateUi(bool success, string msg)
        {
            NoticeHandler.SendMessage4ClashN(msg);
            if (success)
            {
                Locator.Current.GetService<MainWindowViewModel>()?.MyAppExit(false);
            }
        }
    }

    private void CheckUpdateCore(CoreKind type)
    {
        new UpdateHandle().CheckUpdateCore(type, UpdateUi);
        return;

        void UpdateUi(bool success, string msg)
        {
            NoticeHandler.SendMessage4ClashN(msg);
            
            if (success)
            {
                Locator.Current.GetService<MainWindowViewModel>()?.CloseCore();

                var fileName = Utils.GetTempPath(Utils.GetDownloadFileName(msg));
                var toPath = Utils.GetBinPath("", type);
                if (FileManager.ZipExtractToFile(fileName, toPath, "") == false)
                {
                    Global.reloadCore = true;
                    _ = Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
                    
                    NoticeHandler.Instance.Enqueue(ResUI.MsgUpdateCoreCoreFailed);
                }
                else
                {
                    NoticeHandler.Instance.Enqueue(ResUI.MsgUpdateCoreCoreSuccessfullyMore);

                    Global.reloadCore = true;
                    _ = Locator.Current.GetService<MainWindowViewModel>()?.LoadCore();
                    
                    NoticeHandler.Instance.Enqueue(ResUI.MsgUpdateCoreCoreSuccessfully);
                }
            }
        }
    }
}