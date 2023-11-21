using ClashN.Mode;
using ClashN.ViewModels;
using ReactiveUI;
using Splat;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClashN.Views;

/// <summary>
/// Interaction logic for ProfilesView.xaml
/// </summary>
public partial class ProfilesView
{
    public ProfilesView()
    {
        InitializeComponent();

        ViewModel = new ProfilesViewModel();
        Locator.CurrentMutable.RegisterLazySingleton(() => ViewModel, typeof(ProfilesViewModel));

        LstProfiles.PreviewMouseDoubleClick += lstProfiles_PreviewMouseDoubleClick;
        LstProfiles.PreviewMouseLeftButtonDown += LstProfiles_PreviewMouseLeftButtonDown;
        LstProfiles.MouseMove += LstProfiles_MouseMove;
        LstProfiles.DragEnter += LstProfiles_DragEnter;
        LstProfiles.Drop += LstProfiles_Drop;

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ProfileItems, v => v.LstProfiles.ItemsSource).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedSource, v => v.LstProfiles.SelectedItem).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.EditLocalFileCmd, v => v.MenuEditLocalFile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.EditProfileCmd, v => v.MenuEditProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddProfileCmd, v => v.MenuAddProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddProfileViaScanCmd, v => v.MenuAddProfileViaScan)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddProfileViaClipboardCmd, v => v.MenuAddProfileViaClipboard)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ExportProfileCmd, v => v.MenuExportProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ProfileQrcodeCmd, v => v.MenuProfileQrcode).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SubUpdateCmd, v => v.MenuSubUpdate).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateSelectedCmd, v => v.MenuSubUpdateSelected)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateViaProxyCmd, v => v.MenuSubUpdateViaProxy)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateSelectedViaProxyCmd, v => v.MenuSubUpdateSelectedViaProxy)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RemoveProfileCmd, v => v.MenuRemoveProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.CloneProfileCmd, v => v.MenuCloneProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SetDefaultProfileCmd, v => v.MenuSetDefaultProfile)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.EditLocalFileCmd, v => v.MenuEditLocalFile).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.ClearStatisticCmd, v => v.MenuClearStatistic).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ProfileReloadCmd, v => v.MenuProfileReload).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.AddProfileCmd, v => v.BtnAddProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddProfileViaClipboardCmd, v => v.BtnAddProfileViaClipboard)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateViaProxyCmd, v => v.BtnSubUpdateViaProxy)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.EditProfileCmd, v => v.BtnEditProfile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SetDefaultProfileCmd, v => v.BtnSetDefaultProfile)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ProfileQrcodeCmd, v => v.BtnProfileQrcode).DisposeWith(disposables);
        });
    }

    private void ProfilesView_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (e.Key == Key.C)
            {
                ViewModel?.ExportProfile2Clipboard();
            }
            else if (e.Key == Key.V)
            {
                ViewModel?.AddProfilesViaClipboard(false);
            }
        }
        else
        {
            if (Keyboard.IsKeyDown(Key.F5))
            {
                ViewModel?.RefreshProfiles();
            }
            else if (Keyboard.IsKeyDown(Key.Delete))
            {
                ViewModel?.RemoveProfile();
            }
            else if (Keyboard.IsKeyDown(Key.Enter))
            {
                ViewModel?.SetDefaultProfile();
            }
        }
    }

    private void lstProfiles_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.SetDefaultProfile();
    }

    #region Drag and Drop

    private Point _startPoint = new Point();
    private int _startIndex = -1;
    private const string FormatData = "ProfileItemModel";

    /// <summary>
    /// Helper to search up the VisualTree
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="current"></param>
    /// <returns></returns>
    private static T? FindAnchestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T)
            {
                return (T)current;
            }

            current = VisualTreeHelper.GetParent(current);
        } while (current != null);

        return null;
    }

    private void LstProfiles_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Get current mouse position
        _startPoint = e.GetPosition(null);
    }

    private void LstProfiles_MouseMove(object sender, MouseEventArgs e)
    {
        // Get the current mouse position
        var mousePos = e.GetPosition(null);
        var diff = _startPoint - mousePos;

        if (e.LeftButton == MouseButtonState.Pressed &&
            (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            // Get the dragged ListViewItem
            var listView = sender as ListView;
            if (listView == null) return;
            var listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (listViewItem == null) return; // Abort
            // Find the data behind the ListViewItem
            var item = (ProfileItemModel)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
            if (item == null) return; // Abort
            // Initialize the drag & drop operation
            _startIndex = LstProfiles.SelectedIndex;
            var dragData = new DataObject(FormatData, item);
            DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Copy | DragDropEffects.Move);
        }
    }

    private void LstProfiles_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(FormatData) || sender != e.Source)
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void LstProfiles_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(FormatData) && sender == e.Source)
        {
            // Get the drop ListViewItem destination
            var listView = sender as ListView;
            if (listView == null) return;
            var listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (listViewItem == null)
            {
                // Abort
                e.Effects = DragDropEffects.None;
                return;
            }

            // Find the data behind the ListViewItem
            var item = (ProfileItemModel)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
            if (item == null) return;
            // Move item into observable collection
            // (this will be automatically reflected to lstView.ItemsSource)
            e.Effects = DragDropEffects.Move;

            ViewModel?.MoveProfile(_startIndex, item);

            _startIndex = -1;
        }
    }

    #endregion Drag and Drop
}