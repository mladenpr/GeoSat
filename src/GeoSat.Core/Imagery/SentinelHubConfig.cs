namespace GeoSat.Core.Imagery
{
    /// <summary>
    /// Configuration for connecting to Sentinel Hub via the Copernicus Data Space.
    /// Users get these values by registering a free account at https://dataspace.copernicus.eu
    /// and creating an OAuth client + configuration instance in the dashboard.
    /// </summary>
    public class SentinelHubConfig
    {
        /// <summary>OAuth2 client ID from the Copernicus Data Space dashboard.</summary>
        public string ClientId { get; set; }

        /// <summary>OAuth2 client secret from the Copernicus Data Space dashboard.</summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// The configuration instance ID created in the Sentinel Hub dashboard.
        /// Determines which layers (e.g. TRUE-COLOR-S2L2A) are available.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>WMTS layer name — matches the layer ID in your Sentinel Hub configuration instance.</summary>
        public string LayerName { get; set; } = "TRUE_COLOR";

        /// <summary>OAuth token endpoint for Copernicus Data Space.</summary>
        public string TokenUrl { get; set; } =
            "https://identity.dataspace.copernicus.eu/auth/realms/CDSE/protocol/openid-connect/token";

        /// <summary>Base URL for the WMTS service.</summary>
        public string WmtsBaseUrl => $"https://sh.dataspace.copernicus.eu/ogc/wmts/{InstanceId}";
    }
}
