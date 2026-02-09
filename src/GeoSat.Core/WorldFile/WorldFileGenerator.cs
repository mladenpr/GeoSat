using System;

namespace GeoSat.Core.WorldFile
{
    /// <summary>
    /// Generates World Files (.jgw for JPEG, .tfw for TIFF, .pgw for PNG).
    /// A world file contains 6 lines that define the affine transformation
    /// from pixel coordinates to real-world (drawing) coordinates:
    ///
    ///   Line 1: pixel width in map units (x-scale)
    ///   Line 2: rotation about y axis (usually 0)
    ///   Line 3: rotation about x axis (usually 0)
    ///   Line 4: pixel height in map units (y-scale, negative because Y goes down)
    ///   Line 5: x coordinate of the CENTER of the upper-left pixel
    ///   Line 6: y coordinate of the CENTER of the upper-left pixel
    /// </summary>
    public static class WorldFileGenerator
    {
        /// <summary>
        /// Generate world file content for a georeferenced image.
        /// </summary>
        /// <param name="topLeftX">X coordinate of the image's top-left corner in drawing CRS.</param>
        /// <param name="topLeftY">Y coordinate of the image's top-left corner in drawing CRS.</param>
        /// <param name="bottomRightX">X coordinate of the image's bottom-right corner in drawing CRS.</param>
        /// <param name="bottomRightY">Y coordinate of the image's bottom-right corner in drawing CRS.</param>
        /// <param name="imageWidthPx">Image width in pixels.</param>
        /// <param name="imageHeightPx">Image height in pixels.</param>
        public static string Generate(
            double topLeftX, double topLeftY,
            double bottomRightX, double bottomRightY,
            int imageWidthPx, int imageHeightPx)
        {
            var pixelWidth = (bottomRightX - topLeftX) / imageWidthPx;
            var pixelHeight = (bottomRightY - topLeftY) / imageHeightPx; // Negative since Y goes down

            // Center of the upper-left pixel (half a pixel inward)
            var centerX = topLeftX + pixelWidth / 2.0;
            var centerY = topLeftY + pixelHeight / 2.0;

            return string.Join(Environment.NewLine, new[]
            {
                pixelWidth.ToString("F10"),    // Line 1: x-scale
                "0.0000000000",                // Line 2: rotation (y)
                "0.0000000000",                // Line 3: rotation (x)
                pixelHeight.ToString("F10"),   // Line 4: y-scale (negative)
                centerX.ToString("F4"),        // Line 5: upper-left center X
                centerY.ToString("F4"),        // Line 6: upper-left center Y
            });
        }

        /// <summary>
        /// Get the world file extension for a given image format.
        /// Convention: take first and last letter of extension, insert 'w'.
        /// </summary>
        public static string GetWorldFileExtension(string imageExtension)
        {
            var ext = imageExtension.TrimStart('.');
            var lower = ext.ToLowerInvariant();
            if (lower == "jpg" || lower == "jpeg")
                return ".jgw";
            if (lower == "tif" || lower == "tiff")
                return ".tfw";
            if (lower == "png")
                return ".pgw";
            if (lower == "bmp")
                return ".bpw";
            // General rule
            return $".{ext[0]}w{ext[ext.Length - 1]}";
        }
    }
}
