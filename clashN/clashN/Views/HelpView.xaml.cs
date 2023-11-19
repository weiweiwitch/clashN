using ClashN.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;

namespace ClashN.Views
{
    /// <summary>
    /// Interaction logic for HelpView.xaml
    /// </summary>
    public partial class HelpView
    {
        public HelpView()
        {
            InitializeComponent();
            ViewModel = new HelpViewModel();

            this.WhenActivated(disposables =>
            {
                this.BindCommand(ViewModel, vm => vm.CheckUpdateCmd, v => v.BtnCheckUpdateN).DisposeWith(disposables);
                this.BindCommand(ViewModel, vm => vm.CheckUpdateClashCoreCmd, v => v.BtnCheckUpdateClashCore).DisposeWith(disposables);
                this.BindCommand(ViewModel, vm => vm.CheckUpdateClashMetaCoreCmd, v => v.BtnCheckUpdateClashMetaCore).DisposeWith(disposables);
            });
        }

        private void BtnAbout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Utils.ProcessStart(Global.AboutUrl);
        }
    }
}