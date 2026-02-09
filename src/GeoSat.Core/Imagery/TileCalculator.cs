using System;
using System.Collections.Generic;
using GeoSat.Core.Coordinates;

namespace GeoSat.Core.Imagery
{
    /// <summary>
    /// Calculates which WMTS tiles cover a given bounding box at a given zoom level.
    /// WMTS uses a global grid where the world is divided into 2^zoom x 2^zoom tiles.
    /// Each tile is 256x256 pixels. The tiles use Web Mercator (EPSG:3857) projection.
    /// </summary>
    public static class TileCalculator
    {
        public const int TileSize = 256;

        /// <summary>
        /// Convert a WGS84 bounding box to the range of tile indices needed.
        /// </summary>
        public static TileRange GetTileRange(BoundingBox bbox, int zoom)
        {
            var minTileX = LonToTileX(bbox.MinLon, zoom);
            var maxTileX = LonToTileX(bbox.MaxLon, zoom);
            var minTileY = LatToTileY(bbox.MaxLat, zoom); // Note: Y is flipped (north = lower index)
            var maxTileY = LatToTileY(bbox.MinLat, zoom);

            return new TileRange(minTileX, minTileY, maxTileX, maxTileY, zoom);
        }

        /// <summary>
        /// Choose a zoom level that gives approximately the target meters-per-pixel
        /// at the given latitude.
        /// </summary>
        public static int ChooseZoomLevel(double latitude, double targetMetersPerPixel = 10.0)
        {
            // At zoom 0, the entire world (circumference ~40,075 km) fits in 256 pixels.
            // Resolution at equator = 40075016.686 / (256 * 2^zoom)
            // At other latitudes, multiply by cos(lat).
            const double earthCircumference = 40_075_016.686;
            var cosLat = Math.Cos(latitude * Math.PI / 180.0);

            for (int z = 0; z <= 18; z++)
            {
                var metersPerPixel = earthCircumference * cosLat / (TileSize * Math.Pow(2, z));
                if (metersPerPixel <= targetMetersPerPixel)
                    return z;
            }

            return 18; // Max zoom
        }

        /// <summary>Get the WGS84 bounding box of a single tile.</summary>
        public static BoundingBox TileBounds(int tileX, int tileY, int zoom)
        {
            return new BoundingBox(
                TileXToLon(tileX, zoom),
                TileYToLat(tileY + 1, zoom),
                TileXToLon(tileX + 1, zoom),
                TileYToLat(tileY, zoom));
        }

        /// <summary>Get the WGS84 bounding box of an entire tile range.</summary>
        public static BoundingBox TileRangeBounds(TileRange range)
        {
            return new BoundingBox(
                TileXToLon(range.MinTileX, range.Zoom),
                TileYToLat(range.MaxTileY + 1, range.Zoom),
                TileXToLon(range.MaxTileX + 1, range.Zoom),
                TileYToLat(range.MinTileY, range.Zoom));
        }

        // --- Slippy map tile math (standard OSM/WMTS formula) ---

        private static int LonToTileX(double lon, int zoom)
        {
            return (int)Math.Floor((lon + 180.0) / 360.0 * (1 << zoom));
        }

        private static int LatToTileY(double lat, int zoom)
        {
            var latRad = lat * Math.PI / 180.0;
            return (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * (1 << zoom));
        }

        private static double TileXToLon(int tileX, int zoom)
        {
            return tileX / (double)(1 << zoom) * 360.0 - 180.0;
        }

        private static double TileYToLat(int tileY, int zoom)
        {
            var n = Math.PI - 2.0 * Math.PI * tileY / (1 << zoom);
            return 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
        }
    }

    /// <summary>
    /// A rectangular range of tile indices at a specific zoom level.
    /// </summary>
    public class TileRange
    {
        public TileRange(int minTileX, int minTileY, int maxTileX, int maxTileY, int zoom)
        {
            MinTileX = minTileX;
            MinTileY = minTileY;
            MaxTileX = maxTileX;
            MaxTileY = maxTileY;
            Zoom = zoom;
        }

        public int MinTileX { get; }
        public int MinTileY { get; }
        public int MaxTileX { get; }
        public int MaxTileY { get; }
        public int Zoom { get; }

        public int CountX => MaxTileX - MinTileX + 1;
        public int CountY => MaxTileY - MinTileY + 1;
        public int TotalTiles => CountX * CountY;

        /// <summary>Enumerate all (x, y) tile coordinates in this range.</summary>
        public IEnumerable<(int X, int Y)> EnumerateTiles()
        {
            for (int y = MinTileY; y <= MaxTileY; y++)
                for (int x = MinTileX; x <= MaxTileX; x++)
                    yield return (x, y);
        }

        public override bool Equals(object obj)
        {
            if (obj is TileRange other)
            {
                return MinTileX == other.MinTileX &&
                       MinTileY == other.MinTileY &&
                       MaxTileX == other.MaxTileX &&
                       MaxTileY == other.MaxTileY &&
                       Zoom == other.Zoom;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + MinTileX;
                hash = hash * 31 + MinTileY;
                hash = hash * 31 + MaxTileX;
                hash = hash * 31 + MaxTileY;
                hash = hash * 31 + Zoom;
                return hash;
            }
        }
    }
}
