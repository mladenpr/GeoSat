using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GeoSat.Core.Cache;

namespace GeoSat.Core.Imagery
{
    /// <summary>
    /// Downloads satellite imagery tiles from Sentinel Hub WMTS service.
    /// Handles OAuth2 authentication and tile caching.
    /// </summary>
    public class WmtsTileFetcher : ITileFetcher
    {
        private readonly SentinelHubConfig _config;
        private readonly HttpClient _http;
        private readonly DiskTileCache _cache;
        private string _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public WmtsTileFetcher(SentinelHubConfig config, DiskTileCache cache = null)
        {
            _config = config;
            _http = new HttpClient();
            _cache = cache ?? new DiskTileCache();
        }

        /// <summary>
        /// Fetch a single tile. Returns the JPEG bytes.
        /// Uses disk cache to avoid redundant API calls.
        /// </summary>
        public async Task<byte[]> FetchTileAsync(int zoom, int x, int y, CancellationToken ct = default(CancellationToken))
        {
            if (_cache.TryGetTile(zoom, x, y, out var cached))
                return cached;

            await EnsureAuthenticatedAsync(ct);

            // Standard WMTS KVP request
            var url = $"{_config.WmtsBaseUrl}" +
                $"?SERVICE=WMTS" +
                $"&REQUEST=GetTile" +
                $"&VERSION=1.0.0" +
                $"&LAYER={_config.LayerName}" +
                $"&STYLE=default" +
                $"&FORMAT=image/jpeg" +
                $"&TILEMATRIXSET=PopularWebMercator256" +
                $"&TILEMATRIX={zoom}" +
                $"&TILEROW={y}" +
                $"&TILECOL={x}" +
                $"&TIME={DateTime.UtcNow:yyyy-MM-dd}/{DateTime.UtcNow:yyyy-MM-dd}";

            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsByteArrayAsync();
            _cache.StoreTile(zoom, x, y, data);
            return data;
        }

        /// <summary>
        /// Fetch all tiles for a given range. Reports progress via callback.
        /// Returns a dictionary of (tileX, tileY) -> JPEG bytes.
        /// </summary>
        public async Task<Dictionary<(int X, int Y), byte[]>> FetchTileRangeAsync(
            TileRange range,
            IProgress<(int Done, int Total)> progress = null,
            CancellationToken ct = default(CancellationToken))
        {
            var tiles = new Dictionary<(int X, int Y), byte[]>();
            var done = 0;
            var total = range.TotalTiles;

            // Fetch tiles with limited concurrency to be respectful to the API
            var semaphore = new SemaphoreSlim(4);
            var tasks = range.EnumerateTiles().Select(async tile =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var data = await FetchTileAsync(range.Zoom, tile.X, tile.Y, ct);
                    lock (tiles)
                    {
                        tiles[(tile.X, tile.Y)] = data;
                        done++;
                        progress?.Report((done, total));
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return tiles;
        }

        private async Task EnsureAuthenticatedAsync(CancellationToken ct)
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
                return;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
            });

            var response = await _http.PostAsync(_config.TokenUrl, content, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Refresh 60s early

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        public void Dispose() => _http.Dispose();
    }
}
