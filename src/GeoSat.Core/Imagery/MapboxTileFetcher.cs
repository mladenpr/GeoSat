using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GeoSat.Core.Cache;

namespace GeoSat.Core.Imagery
{
    /// <summary>
    /// Downloads satellite imagery tiles from Mapbox Raster Tiles API.
    /// No OAuth required — uses a simple access token in the query string.
    /// </summary>
    public class MapboxTileFetcher : ITileFetcher
    {
        private readonly MapboxConfig _config;
        private readonly HttpClient _http;
        private readonly DiskTileCache _cache;

        public MapboxTileFetcher(MapboxConfig config, DiskTileCache cache = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _http = new HttpClient();
            _cache = cache ?? new DiskTileCache();
        }

        public async Task<byte[]> FetchTileAsync(int zoom, int x, int y, CancellationToken ct = default(CancellationToken))
        {
            if (_cache.TryGetTile(zoom, x, y, out var cached))
                return cached;

            var url = BuildTileUrl(zoom, x, y);

            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsByteArrayAsync();
            _cache.StoreTile(zoom, x, y, data);
            return data;
        }

        public async Task<Dictionary<(int X, int Y), byte[]>> FetchTileRangeAsync(
            TileRange range,
            IProgress<(int Done, int Total)> progress = null,
            CancellationToken ct = default(CancellationToken))
        {
            var tiles = new Dictionary<(int X, int Y), byte[]>();
            var done = 0;
            var total = range.TotalTiles;

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

        internal string BuildTileUrl(int zoom, int x, int y)
        {
            // Mapbox Raster Tiles v4 API — returns 256x256 JPEG tiles
            return $"https://api.mapbox.com/v4/{_config.TilesetId}/{zoom}/{x}/{y}.jpg90" +
                   $"?access_token={_config.AccessToken}";
        }

        public void Dispose() => _http.Dispose();
    }
}
