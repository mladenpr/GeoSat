using System.Linq;
using GeoSat.Core.Coordinates;
using GeoSat.Core.Imagery;
using Xunit;

namespace GeoSat.Core.Tests
{
    public class TileCalculatorTests
    {
        [Fact]
        public void GetTileRange_CoversBelgrade_ReturnsValidRange()
        {
            // Belgrade, Serbia: roughly 20.40-20.50 E, 44.78-44.83 N
            var bbox = new BoundingBox(20.40, 44.78, 20.50, 44.83);
            var range = TileCalculator.GetTileRange(bbox, zoom: 13);

            Assert.True(range.CountX > 0);
            Assert.True(range.CountY > 0);
            Assert.True(range.TotalTiles > 0);
            Assert.True(range.TotalTiles < 100); // Sanity: shouldn't need hundreds of tiles
        }

        [Fact]
        public void GetTileRange_SinglePoint_ReturnsOneTile()
        {
            var bbox = new BoundingBox(20.45, 44.80, 20.45, 44.80);
            var range = TileCalculator.GetTileRange(bbox, zoom: 10);

            Assert.Equal(1, range.TotalTiles);
        }

        [Fact]
        public void ChooseZoomLevel_10mTarget_ReturnsReasonableZoom()
        {
            // At ~45 degrees latitude, zoom 13-14 gives roughly 10m/pixel
            var zoom = TileCalculator.ChooseZoomLevel(latitude: 45.0, targetMetersPerPixel: 10.0);

            Assert.InRange(zoom, 12, 15);
        }

        [Fact]
        public void ChooseZoomLevel_EquatorVsHighLat_EquatorNeedsHigherZoom()
        {
            // At equator, a higher zoom is needed for the same resolution
            var zoomEquator = TileCalculator.ChooseZoomLevel(latitude: 0, targetMetersPerPixel: 10.0);
            var zoomArctic = TileCalculator.ChooseZoomLevel(latitude: 70, targetMetersPerPixel: 10.0);

            Assert.True(zoomEquator >= zoomArctic);
        }

        [Fact]
        public void TileRangeBounds_EnclosesOriginalBbox()
        {
            var bbox = new BoundingBox(20.40, 44.78, 20.50, 44.83);
            var range = TileCalculator.GetTileRange(bbox, zoom: 13);
            var tileBounds = TileCalculator.TileRangeBounds(range);

            // The tile range should fully enclose the original bbox
            Assert.True(tileBounds.MinLon <= bbox.MinLon);
            Assert.True(tileBounds.MinLat <= bbox.MinLat);
            Assert.True(tileBounds.MaxLon >= bbox.MaxLon);
            Assert.True(tileBounds.MaxLat >= bbox.MaxLat);
        }

        [Fact]
        public void EnumerateTiles_CountMatchesTotalTiles()
        {
            var bbox = new BoundingBox(20.40, 44.78, 20.50, 44.83);
            var range = TileCalculator.GetTileRange(bbox, zoom: 13);

            var enumerated = range.EnumerateTiles().ToList();
            Assert.Equal(range.TotalTiles, enumerated.Count);
        }
    }
}
