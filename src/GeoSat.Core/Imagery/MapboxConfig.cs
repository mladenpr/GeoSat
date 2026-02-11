namespace GeoSat.Core.Imagery
{
    /// <summary>
    /// Configuration for Mapbox Satellite tile access.
    /// Users get an access token from https://account.mapbox.com/access-tokens/
    /// </summary>
    public class MapboxConfig
    {
        /// <summary>Mapbox public or secret access token.</summary>
        public string AccessToken { get; set; }

        /// <summary>Mapbox tileset ID. Defaults to satellite imagery.</summary>
        public string TilesetId { get; set; } = "mapbox.satellite";
    }
}
