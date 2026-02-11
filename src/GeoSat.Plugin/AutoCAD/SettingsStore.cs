using System;
using System.IO;
using System.Text.Json;
using GeoSat.Core.Cache;
using GeoSat.Core.Imagery;

namespace GeoSat.Plugin.AutoCAD
{
    /// <summary>
    /// Persists plugin settings (API credentials) to a JSON file in AppData.
    /// Credentials are stored per-user, not per-drawing.
    /// </summary>
    public static class SettingsStore
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GeoSat", "settings.json");

        public static GeoSatSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new GeoSatSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<GeoSatSettings>(json) ?? new GeoSatSettings();
        }

        public static void Save(GeoSatSettings settings)
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        public static SentinelHubConfig ToSentinelConfig(GeoSatSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ClientId) ||
                string.IsNullOrWhiteSpace(settings.ClientSecret) ||
                string.IsNullOrWhiteSpace(settings.InstanceId))
                return null;

            return new SentinelHubConfig
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret,
                InstanceId = settings.InstanceId,
            };
        }

        public static MapboxConfig ToMapboxConfig(GeoSatSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.MapboxAccessToken))
                return null;

            return new MapboxConfig
            {
                AccessToken = settings.MapboxAccessToken,
            };
        }

        /// <summary>
        /// Creates the appropriate ITileFetcher based on the configured provider.
        /// Returns null if required credentials are missing.
        /// </summary>
        public static ITileFetcher CreateTileFetcher(GeoSatSettings settings)
        {
            var cache = new DiskTileCache();

            switch (settings.Provider)
            {
                case ImageryProvider.Mapbox:
                    var mapboxConfig = ToMapboxConfig(settings);
                    if (mapboxConfig == null) return null;
                    return new MapboxTileFetcher(mapboxConfig, cache);

                case ImageryProvider.Sentinel:
                default:
                    var sentinelConfig = ToSentinelConfig(settings);
                    if (sentinelConfig == null) return null;
                    return new WmtsTileFetcher(sentinelConfig, cache);
            }
        }
    }

    public enum ImageryProvider
    {
        Sentinel = 0,
        Mapbox = 1,
    }

    public class GeoSatSettings
    {
        public ImageryProvider Provider { get; set; } = ImageryProvider.Mapbox;
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string InstanceId { get; set; } = "";
        public string MapboxAccessToken { get; set; } = "";
        public string OutputDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GeoSat");
    }
}
