using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CVDesktopEditor.Controls
{
    public partial class AppTitleBar : UserControl
    {
        public AppTitleBar()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                var window = Window.GetWindow(this);
                if (window == null)
                    return;

                TxtWindowTitle.Text = window.Title;
                BtnMaximize.Visibility = window.ResizeMode == ResizeMode.NoResize
                    ? Visibility.Collapsed : Visibility.Visible;
                BtnMinimize.Visibility = window.ResizeMode == ResizeMode.NoResize
                    ? Visibility.Collapsed : Visibility.Visible;
            };
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null || e.OriginalSource is Button)
                return;

            if (e.ClickCount == 2 && window.ResizeMode != ResizeMode.NoResize)
            {
                ToggleMaximize(window);
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
                window.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize(Window.GetWindow(this));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }

        private static void ToggleMaximize(Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;
        }
    }
}
