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
            TxtStatus.Text = "Pega la licencia que recibiste para activar la app.";
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
    }
}
