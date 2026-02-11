using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeoSat.Core.Imagery
{
    /// <summary>
    /// Abstraction for fetching map tiles from any slippy-map-compatible provider.
    /// </summary>
    public interface ITileFetcher : IDisposable
    {
        Task<byte[]> FetchTileAsync(int zoom, int x, int y, CancellationToken ct = default(CancellationToken));

        Task<Dictionary<(int X, int Y), byte[]>> FetchTileRangeAsync(
            TileRange range,
            IProgress<(int Done, int Total)> progress = null,
            CancellationToken ct = default(CancellationToken));
    }
}
