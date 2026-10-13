using System.Diagnostics;
using System.Windows;
using CVDesktopEditor.Services;

namespace CVDesktopEditor
{
    public partial class LicenseActivationWindow : Window
    {
        private readonly AppConfigurationService _configurationService = new();
        private readonly LicenseService _licenseService = new();

        public LicenseStatus? ActivatedStatus { get; private set; }

        public LicenseActivationWindow()
        {
            InitializeComponent();
            WindowSizeService.KeepTaskbarVisible(this);
            WindowSizeService.FitToWorkArea(this);
            TxtStatus.Text = "Compra tu licencia en la web o escribe a jretanamendez@gmail.com.";
        }

        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Validando licencia...";

            var configuration = _configurationService.Load();

            ActivatedStatus = await _licenseService.ActivateOnlineLicenseAsync(
                TxtLicenseKey.Text,
                configuration.LicenseApiBaseUrl);

            TxtStatus.Text = ActivatedStatus.Message;
            if (ActivatedStatus.State == LicenseState.Active)
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnPurchase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(AppConfigurationService.PurchasePageUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to open purchase page.");
                TxtStatus.Text = $"Abre {AppConfigurationService.PurchasePageUrl} en tu navegador.";
            }
        }
    }
}
