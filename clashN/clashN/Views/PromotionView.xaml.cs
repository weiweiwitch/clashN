using ReactiveUI;
using System.Windows;
using ClashN.Tool;

namespace ClashN.Views
{
    /// <summary>
    /// Interaction logic for PromotionView.xaml
    /// </summary>
    public partial class PromotionView
    {
        public PromotionView()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
            });
        }

        private void BtnPromotion_Click(object sender, RoutedEventArgs e)
        {
            Utils.ProcessStart($"{Utils.Base64Decode(Global.PromotionUrl)}?t={DateTime.Now.Ticks}");
        }
    }
}