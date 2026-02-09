using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GeoSat.Core.Cache;
using GeoSat.Core.Coordinates;
using GeoSat.Core.Imagery;
using GeoSat.Core.WorldFile;

namespace GeoSat.Core
{
    /// <summary>
    /// The main orchestrator: takes a bounding box in drawing coordinates,
    /// fetches satellite imagery, stitches it, generates a world file,
    /// and returns the paths to the image + world file ready for AutoCAD insertion.
    /// </summary>
    public class GeoSatEngine : IDisposable
    {
        private readonly CrsTransformer _crs;
        private readonly WmtsTileFetcher _fetcher;

        public GeoSatEngine(CrsEntry drawingCrs, SentinelHubConfig config, DiskTileCache cache = null)
        {
            _crs = new CrsTransformer(drawingCrs.CoordinateSystem);
            _fetcher = new WmtsTileFetcher(config, cache);
        }

        /// <summary>
        /// Full pipeline: from two drawing-CRS corners to a georeferenced image on disk.
        /// </summary>
        /// <param name="x1">First corner X in drawing coordinates.</param>
        /// <param name="y1">First corner Y in drawing coordinates.</param>
        /// <param name="x2">Second corner X in drawing coordinates.</param>
        /// <param name="y2">Second corner Y in drawing coordinates.</param>
        /// <param name="outputDir">Directory where image + world file are saved.</param>
        /// <param name="progress">Optional progress callback.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result containing file paths and placement info for AutoCAD.</returns>
        public async Task<GeoSatResult> FetchImageryAsync(
            double x1, double y1,
            double x2, double y2,
            string outputDir,
            IProgress<(int Done, int Total)> progress = null,
            CancellationToken ct = default(CancellationToken))
        {
            // 1. Transform drawing coords to WGS84
            var bbox = _crs.DrawingBboxToWgs84(x1, y1, x2, y2);

            // 2. Calculate zoom level (~10m for Sentinel-2 free tier)
            var zoom = TileCalculator.ChooseZoomLevel(bbox.CenterLat, targetMetersPerPixel: 10.0);

            // 3. Determine which tiles we need
            var tileRange = TileCalculator.GetTileRange(bbox, zoom);

            // 4. Fetch all tiles
            var tiles = await _fetcher.FetchTileRangeAsync(tileRange, progress, ct);

            // 5. Stitch tiles into one mosaic
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var imagePath = Path.Combine(outputDir, $"geosat_{timestamp}.jpg");
            var (imgWidth, imgHeight) = MosaicStitcher.StitchAndSave(tiles, tileRange, imagePath);

            // 6. The mosaic covers the full tile range (slightly larger than the user's bbox).
            //    Compute the tile range's extent back in drawing coordinates for the world file.
            var tileRangeBbox = TileCalculator.TileRangeBounds(tileRange);
            var (topLeftX, topLeftY) = _crs.Wgs84ToDrawing(tileRangeBbox.MinLon, tileRangeBbox.MaxLat);
            var (botRightX, botRightY) = _crs.Wgs84ToDrawing(tileRangeBbox.MaxLon, tileRangeBbox.MinLat);

            // 7. Generate world file
            var worldFileContent = WorldFileGenerator.Generate(
                topLeftX, topLeftY, botRightX, botRightY, imgWidth, imgHeight);
            var worldFileExt = WorldFileGenerator.GetWorldFileExtension(".jpg");
            var worldFilePath = Path.ChangeExtension(imagePath, worldFileExt);
            File.WriteAllText(worldFilePath, worldFileContent);

            return new GeoSatResult
            {
                ImagePath = imagePath,
                WorldFilePath = worldFilePath,
                InsertionPointX = topLeftX,
                InsertionPointY = topLeftY,
                ImageWidthDrawingUnits = botRightX - topLeftX,
                ImageHeightDrawingUnits = topLeftY - botRightY,
                ImageWidthPx = imgWidth,
                ImageHeightPx = imgHeight,
                TileCount = tileRange.TotalTiles,
                ZoomLevel = zoom,
            };
        }

        public void Dispose() => _fetcher.Dispose();
    }

    public class GeoSatResult
    {
        public string ImagePath { get; set; } = "";
        public string WorldFilePath { get; set; } = "";

        /// <summary>Top-left corner in drawing CRS — where AutoCAD should insert the image.</summary>
        public double InsertionPointX { get; set; }
        public double InsertionPointY { get; set; }

        /// <summary>Image extent in drawing units (for scaling the raster reference).</summary>
        public double ImageWidthDrawingUnits { get; set; }
        public double ImageHeightDrawingUnits { get; set; }

        public int ImageWidthPx { get; set; }
        public int ImageHeightPx { get; set; }
        public int TileCount { get; set; }
        public int ZoomLevel { get; set; }
    }
}
