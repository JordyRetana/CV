using System.Windows;

namespace CVDesktopEditor
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string status)
        {
            TxtStatus.Text = status;
        }
    }
}
