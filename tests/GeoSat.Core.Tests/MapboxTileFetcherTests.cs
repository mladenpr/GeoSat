using GeoSat.Core.Imagery;
using Xunit;

namespace GeoSat.Core.Tests
{
    public class MapboxTileFetcherTests
    {
        [Fact]
        public void BuildTileUrl_ContainsCorrectComponents()
        {
            var config = new MapboxConfig
            {
                AccessToken = "pk.test_token_123",
                TilesetId = "mapbox.satellite",
            };

            using (var fetcher = new MapboxTileFetcher(config))
            {
                var url = fetcher.BuildTileUrl(zoom: 14, x: 8529, y: 5765);

                Assert.Contains("api.mapbox.com/v4/mapbox.satellite", url);
                Assert.Contains("/14/8529/5765", url);
                Assert.Contains(".jpg90", url);
                Assert.Contains("access_token=pk.test_token_123", url);
            }
        }

        [Fact]
        public void BuildTileUrl_CustomTileset_UsesIt()
        {
            var config = new MapboxConfig
            {
                AccessToken = "pk.test",
                TilesetId = "mapbox.streets-satellite",
            };

            using (var fetcher = new MapboxTileFetcher(config))
            {
                var url = fetcher.BuildTileUrl(zoom: 10, x: 100, y: 200);

                Assert.Contains("mapbox.streets-satellite", url);
            }
        }

        [Fact]
        public void BuildTileUrl_ZoomZero_ValidUrl()
        {
            var config = new MapboxConfig
            {
                AccessToken = "pk.test",
            };

            using (var fetcher = new MapboxTileFetcher(config))
            {
                var url = fetcher.BuildTileUrl(zoom: 0, x: 0, y: 0);

                Assert.Contains("/0/0/0.jpg90", url);
            }
        }
    }
}
