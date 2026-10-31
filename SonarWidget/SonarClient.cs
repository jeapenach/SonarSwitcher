using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace SonarWidget
{
    /// <summary>Dispositivo de audio de Windows tal como lo expone Sonar.</summary>
    public sealed class DeviceItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsCapture { get; set; }
        public override string ToString() => Name;
    }

    /// <summary>
    /// Cliente de la API local (no oficial) de SteelSeries GG / Sonar.
    /// 1) GG responde por HTTPS (certificado autofirmado) en 127.0.0.1:{puerto GG}
    /// 2) /subApps entrega la direccion HTTP de Sonar (el puerto cambia al reiniciar GG)
    /// </summary>
    public sealed class SonarClient
    {
        public static readonly string[] OutputChannels = { "game", "chat", "media", "aux" };
        public const string MicChannel = "mic";

        private readonly HttpClient _http;
        private string _sonarBase;

        public SonarClient()
        {
            var filter = new HttpBaseProtocolFilter();
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            _http = new HttpClient(filter);
        }

        /// <summary>Puerto de GG (por defecto 6327). Se guarda en la configuracion local de la app.</summary>
        public int GgPort
        {
            get
            {
                try
                {
                    var v = ApplicationData.Current.LocalSettings.Values["GgPort"];
                    if (v is int i && i > 0 && i < 65536) return i;
                }
                catch { }
                return 6327;
            }
            set
            {
                try { ApplicationData.Current.LocalSettings.Values["GgPort"] = value; } catch { }
                _sonarBase = null; // forzar redescubrimiento
            }
        }

        public async Task ConnectAsync()
        {
            var json = await GetTextAsync(new Uri($"https://127.0.0.1:{GgPort}/subApps"));
            var root = JsonObject.Parse(json);
            var sonar = root.GetNamedObject("subApps").GetNamedObject("sonar");
            if (!sonar.GetNamedBoolean("isRunning", false))
                throw new InvalidOperationException("Sonar no esta en ejecucion en SteelSeries GG.");
            var meta = sonar.GetNamedValue("metadata");
            if (meta.ValueType != JsonValueType.Object)
                throw new InvalidOperationException("Sonar no publico su direccion todavia.");
            var addr = meta.GetObject().GetNamedString("webServerAddress", "");
            if (string.IsNullOrWhiteSpace(addr))
                throw new InvalidOperationException("Sonar no publico su direccion todavia.");
            _sonarBase = addr.TrimEnd('/');
        }

        /// <summary>Ejecuta la operacion; si falla, redescubre el puerto de Sonar y reintenta una vez.</summary>
        private async Task<T> WithRetry<T>(Func<Task<T>> op)
        {
            if (_sonarBase == null) await ConnectAsync();
            try { return await op(); }
            catch (Exception)
            {
                await ConnectAsync();
                return await op();
            }
        }

        public Task<List<DeviceItem>> GetDevicesAsync() => WithRetry(async () =>
        {
            var arr = JsonArray.Parse(await GetTextAsync(new Uri(_sonarBase + "/audioDevices")));
            var list = new List<DeviceItem>();
            foreach (var v in arr)
            {
                var o = v.GetObject();
                if (o.GetNamedBoolean("isVad", false)) continue;                 // ocultar dispositivos virtuales de Sonar
                if (o.GetNamedString("state", "") != "active") continue;
                list.Add(new DeviceItem
                {
                    Id = o.GetNamedString("id"),
                    Name = o.GetNamedString("friendlyName"),
                    IsCapture = o.GetNamedString("dataFlow", "render") == "capture"
                });
            }
            return list;
        });

        /// <summary>canal ("game","chat","media","aux","mic") -> id de dispositivo</summary>
        public Task<Dictionary<string, string>> GetRedirectionsAsync() => WithRetry(async () =>
        {
            var arr = JsonArray.Parse(await GetTextAsync(new Uri(_sonarBase + "/classicRedirections")));
            var map = new Dictionary<string, string>();
            foreach (var v in arr)
            {
                var o = v.GetObject();
                map[o.GetNamedString("id")] = o.GetNamedString("deviceId", "");
            }
            return map;
        });

        public Task<bool> SetRedirectionAsync(string channel, string deviceId) => WithRetry(async () =>
        {
            var url = $"{_sonarBase}/classicRedirections/{channel}/deviceId/{Uri.EscapeDataString(deviceId)}";
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8)))
            using (var content = new HttpStringContent(""))
            using (var resp = await _http.PutAsync(new Uri(url), content).AsTask(cts.Token))
            {
                if (!resp.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Sonar respondio {(int)resp.StatusCode} al cambiar '{channel}'.");
                return true;
            }
        });

        private async Task<string> GetTextAsync(Uri uri)
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
            {
                return await _http.GetStringAsync(uri).AsTask(cts.Token);
            }
        }
    }
}
