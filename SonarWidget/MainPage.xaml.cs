using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Gaming.XboxGameBar;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SonarWidget
{
    public sealed partial class MainPage : Page
    {
        private XboxGameBarWidget _widget;               // null si se abre fuera de Game Bar
        private readonly SonarClient _sonar = new SonarClient();
        private bool _loading;                           // evita aplicar cambios mientras se rellenan los combos
        private List<ComboBox> _channelCombos;

        public MainPage()
        {
            InitializeComponent();
            _channelCombos = new List<ComboBox> { GameCombo, ChatCombo, MediaCombo, AuxCombo, MicCombo };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _widget = e.Parameter as XboxGameBarWidget;
            base.OnNavigatedTo(e);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PortBox.Text = _sonar.GgPort.ToString();
            await RefreshAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PortBox.Text, out var port) && port > 0 && port < 65536)
                _sonar.GgPort = port;
            await RefreshAsync();
        }

        private void Gear_Click(object sender, RoutedEventArgs e)
        {
            AdvancedPanel.Visibility = AdvancedPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private async System.Threading.Tasks.Task RefreshAsync()
        {
            _loading = true;
            RefreshButton.IsEnabled = false;
            try
            {
                StatusText.Text = "Conectando con Sonar...";
                await _sonar.ConnectAsync();
                var devices = await _sonar.GetDevicesAsync();
                var reds = await _sonar.GetRedirectionsAsync();

                var outputs = devices.Where(d => !d.IsCapture).ToList();
                var mics = devices.Where(d => d.IsCapture).ToList();

                AllCombo.ItemsSource = outputs;
                foreach (var cb in _channelCombos)
                {
                    var channel = (string)cb.Tag;
                    var list = channel == SonarClient.MicChannel ? mics : outputs;
                    cb.ItemsSource = list;
                    reds.TryGetValue(channel, out var currentId);
                    cb.SelectedItem = list.FirstOrDefault(d => d.Id == currentId);
                }
                SyncAllCombo(outputs, reds);
                StatusText.Text = "Listo.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "No se pudo hablar con Sonar: " + ex.Message +
                                  "\nVerifica que SteelSeries GG y Sonar estén abiertos y el puerto de GG.";
            }
            finally
            {
                _loading = false;
                RefreshButton.IsEnabled = true;
            }
        }

        // Si las 4 salidas apuntan al mismo dispositivo, el combo "Todas" lo refleja.
        private void SyncAllCombo(List<DeviceItem> outputs, Dictionary<string, string> reds)
        {
            var ids = SonarClient.OutputChannels.Select(c => reds.TryGetValue(c, out var id) ? id : null).Distinct().ToList();
            AllCombo.SelectedItem = ids.Count == 1 ? outputs.FirstOrDefault(d => d.Id == ids[0]) : null;
        }

        private async void Channel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var cb = (ComboBox)sender;
            if (!(cb.SelectedItem is DeviceItem dev)) return;
            await ApplyAsync(new[] { (string)cb.Tag }, dev);
        }

        private async void All_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (!(AllCombo.SelectedItem is DeviceItem dev)) return;
            await ApplyAsync(SonarClient.OutputChannels, dev);
        }

        private async System.Threading.Tasks.Task ApplyAsync(IEnumerable<string> channels, DeviceItem dev)
        {
            var list = channels.ToList();
            _loading = true;   // los combos se actualizan por codigo sin re-disparar cambios
            try
            {
                StatusText.Text = "Aplicando: " + dev.Name + "...";
                foreach (var ch in list)
                    await _sonar.SetRedirectionAsync(ch, dev.Id);

                // Verificar que Sonar realmente quedó con el valor pedido
                var reds = await _sonar.GetRedirectionsAsync();
                var failed = list.Where(ch => !reds.TryGetValue(ch, out var id) || id != dev.Id).ToList();

                foreach (var cb in _channelCombos.Where(c => list.Contains((string)c.Tag)))
                {
                    var src = ((IEnumerable<DeviceItem>)cb.ItemsSource);
                    reds.TryGetValue((string)cb.Tag, out var currentId);
                    cb.SelectedItem = src.FirstOrDefault(d => d.Id == currentId);
                }
                SyncAllCombo(((IEnumerable<DeviceItem>)AllCombo.ItemsSource).ToList(), reds);

                StatusText.Text = failed.Count == 0
                    ? "Listo: " + dev.Name
                    : "Sonar no aplicó el cambio en: " + string.Join(", ", failed);
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error al aplicar: " + ex.Message;
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
