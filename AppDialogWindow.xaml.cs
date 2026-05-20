using System.Windows;

namespace CVDesktopEditor
{
    public partial class AppDialogWindow : Window
    {
        private AppDialogWindow(string title, string message, string icon, bool showCancel)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;
            TxtIcon.Text = icon;
            BtnCancel.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;
        }

        public static void ShowInfo(Window owner, string title, string message)
        {
            Show(owner, title, message, "i", false);
        }

        public static void ShowWarning(Window owner, string title, string message)
        {
            Show(owner, title, message, "!", false);
        }

        public static void ShowError(Window owner, string title, string message)
        {
            Show(owner, title, message, "x", false);
        }

        public static bool Confirm(Window owner, string title, string message)
        {
            var dialog = new AppDialogWindow(title, message, "?", true)
            {
                Owner = owner
            };

            return dialog.ShowDialog() == true;
        }

        private static void Show(Window owner, string title, string message, string icon, bool showCancel)
        {
            var dialog = new AppDialogWindow(title, message, icon, showCancel)
            {
                Owner = owner
            };

            dialog.ShowDialog();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnWindowClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
