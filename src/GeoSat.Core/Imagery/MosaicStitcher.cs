using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GeoSat.Core.Imagery
{
    /// <summary>
    /// Stitches individual WMTS tiles into a single mosaic image.
    /// The result is one large JPEG that covers the full tile range.
    /// </summary>
    public static class MosaicStitcher
    {
        /// <summary>
        /// Stitch tiles into a single image and save to disk.
        /// </summary>
        /// <param name="tiles">Map of (tileX, tileY) -> JPEG bytes.</param>
        /// <param name="range">The tile range that was fetched.</param>
        /// <param name="outputPath">Where to save the stitched JPEG.</param>
        /// <param name="quality">JPEG quality (1-100).</param>
        /// <returns>The dimensions (width, height) of the output image in pixels.</returns>
        public static (int Width, int Height) StitchAndSave(
            Dictionary<(int X, int Y), byte[]> tiles,
            TileRange range,
            string outputPath,
            int quality = 90)
        {
            var width = range.CountX * TileCalculator.TileSize;
            var height = range.CountY * TileCalculator.TileSize;

            using (var mosaic = new Image<Rgb24>(width, height))
            {
                foreach (var kvp in tiles)
                {
                    var tileX = kvp.Key.X;
                    var tileY = kvp.Key.Y;
                    var data = kvp.Value;

                    var offsetX = (tileX - range.MinTileX) * TileCalculator.TileSize;
                    var offsetY = (tileY - range.MinTileY) * TileCalculator.TileSize;

                    using (var tileImage = Image.Load<Rgb24>(data))
                    {
                        mosaic.Mutate(ctx => ctx.DrawImage(tileImage, new Point(offsetX, offsetY), 1f));
                    }
                }

                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                mosaic.Save(outputPath, new JpegEncoder { Quality = quality });
                return (width, height);
            }
        }
    }
}
