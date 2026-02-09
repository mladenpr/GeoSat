using System;
using System.IO;

namespace GeoSat.Core.Cache
{
    /// <summary>
    /// Caches downloaded tiles on disk so repeated fetches of the same area
    /// don't hit the API again. Tiles are stored as: {cacheDir}/{zoom}/{x}/{y}.jpg
    /// </summary>
    public class DiskTileCache
    {
        private readonly string _cacheDir;

        public DiskTileCache(string cacheDir = null)
        {
            _cacheDir = cacheDir
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GeoSat", "TileCache");
            Directory.CreateDirectory(_cacheDir);
        }

        public string GetTilePath(int zoom, int x, int y) =>
            Path.Combine(_cacheDir, zoom.ToString(), x.ToString(), $"{y}.jpg");

        public bool TryGetTile(int zoom, int x, int y, out byte[] data)
        {
            var path = GetTilePath(zoom, x, y);
            if (File.Exists(path))
            {
                data = File.ReadAllBytes(path);
                return true;
            }
            data = Array.Empty<byte>();
            return false;
        }

        public void StoreTile(int zoom, int x, int y, byte[] data)
        {
            var path = GetTilePath(zoom, x, y);
            var dir = Path.GetDirectoryName(path);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(path, data);
        }

        public void ClearCache()
        {
            if (Directory.Exists(_cacheDir))
                Directory.Delete(_cacheDir, recursive: true);
            Directory.CreateDirectory(_cacheDir);
        }
    }
}
