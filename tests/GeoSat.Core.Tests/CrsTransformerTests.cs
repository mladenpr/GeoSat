using GeoSat.Core.Coordinates;
using Xunit;

namespace GeoSat.Core.Tests
{
    public class CrsTransformerTests
    {
        [Fact]
        public void Utm33N_BelgradeCoords_RoundtripsCorrectly()
        {
            var transformer = new CrsTransformer(CrsRegistry.Utm33N.CoordinateSystem);

            // Belgrade approximate UTM 34N coords (we use 33N for testing, so values differ)
            // Known: Belgrade is roughly 20.46 E, 44.82 N
            var (lon, lat) = transformer.DrawingToWgs84(500000, 4965000);

            // Should be somewhere in Europe
            Assert.InRange(lon, 10, 30);
            Assert.InRange(lat, 40, 55);

            // Roundtrip
            var (x, y) = transformer.Wgs84ToDrawing(lon, lat);
            Assert.Equal(500000, x, precision: 1);
            Assert.Equal(4965000, y, precision: 1);
        }

        [Fact]
        public void DrawingBboxToWgs84_ReturnsOrdered()
        {
            var transformer = new CrsTransformer(CrsRegistry.Utm33N.CoordinateSystem);

            var bbox = transformer.DrawingBboxToWgs84(490000, 4960000, 510000, 4970000);

            Assert.True(bbox.MinLon < bbox.MaxLon);
            Assert.True(bbox.MinLat < bbox.MaxLat);
            Assert.True(bbox.Width > 0);
            Assert.True(bbox.Height > 0);
        }

        [Fact]
        public void WebMercator_ToWgs84_KnownPoint()
        {
            var transformer = new CrsTransformer(CrsRegistry.WebMercator.CoordinateSystem);

            // Web Mercator coords for (0, 0) in WGS84 = (0, 0) in Web Mercator
            var (lon, lat) = transformer.DrawingToWgs84(0, 0);
            Assert.Equal(0, lon, precision: 3);
            Assert.Equal(0, lat, precision: 3);
        }
    }
}
