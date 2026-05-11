using System.IO;
using System.Windows;
using System.Windows.Threading;
using CVDesktopEditor.Services;
using PdfSharp.Fonts;

namespace CVDesktopEditor
{
    public partial class App : Application
    {
        private SplashWindow? _splashWindow;

        public App()
        {
            if (GlobalFontSettings.FontResolver is null)
            {
                GlobalFontSettings.FontResolver = new WindowsFontResolver();
            }

            AppLogger.Info("Application starting.");
            var integrityReport = new AppIntegrityService().CheckRuntimeFiles();
            AppLogger.Info($"Integrity check healthy: {integrityReport.IsHealthy}. Checked files: {integrityReport.CheckedFiles.Count}.");
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _splashWindow = new SplashWindow();
            _splashWindow.Show();

            await InitializeApplicationAsync();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            _splashWindow.Close();
            _splashWindow = null;
        }

        private async Task InitializeApplicationAsync()
        {
            UpdateSplash("Cargando configuracion...");
            _ = new AppConfigurationService().Load();
            await Task.Delay(250);

            UpdateSplash("Validando integridad...");
            var integrityReport = new AppIntegrityService().CheckRuntimeFiles();
            AppLogger.Info($"Startup integrity check healthy: {integrityReport.IsHealthy}.");
            await Task.Delay(250);

            UpdateSplash("Preparando espacio de trabajo...");
            Directory.CreateDirectory(AppLogger.LogFolder);
            await Task.Delay(250);
        }

        private void UpdateSplash(string status)
        {
            _splashWindow?.SetStatus(status);
            AppLogger.Info(status);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogger.Error(e.Exception, "Unhandled UI exception.");
            MessageBox.Show(
                "Ocurrio un error inesperado. El detalle fue guardado en los logs locales.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                AppLogger.Error(ex, "Unhandled application domain exception.");
            else
                AppLogger.Warn($"Unhandled non-exception object: {e.ExceptionObject}");
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            AppLogger.Error(e.Exception, "Unobserved task exception.");
            e.SetObserved();
        }
    }
}
