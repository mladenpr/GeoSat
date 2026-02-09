using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace GeoSat.Core.Coordinates
{
    /// <summary>
    /// Transforms coordinates between the drawing's CRS and WGS84 (lat/lon).
    /// All imagery APIs expect lat/lon, but CAD drawings use projected coordinates.
    /// </summary>
    public class CrsTransformer
    {
        private readonly IMathTransform _drawingToWgs84;
        private readonly IMathTransform _wgs84ToDrawing;

        public CrsTransformer(CoordinateSystem drawingCrs)
        {
            var factory = new CoordinateTransformationFactory();

            var toWgs84 = factory.CreateFromCoordinateSystems(
                drawingCrs, GeographicCoordinateSystem.WGS84);

            _drawingToWgs84 = toWgs84.MathTransform;
            _wgs84ToDrawing = toWgs84.MathTransform.Inverse();
        }

        /// <summary>
        /// Convert drawing coordinates (e.g. UTM easting/northing) to lat/lon.
        /// Input:  (x, y) in drawing CRS
        /// Output: (longitude, latitude) in WGS84
        /// </summary>
        public (double Lon, double Lat) DrawingToWgs84(double x, double y)
        {
            var result = _drawingToWgs84.Transform(x, y);
            return (result.x, result.y);
        }

        /// <summary>
        /// Convert lat/lon back to drawing coordinates.
        /// Input:  (longitude, latitude) in WGS84
        /// Output: (x, y) in drawing CRS
        /// </summary>
        public (double X, double Y) Wgs84ToDrawing(double lon, double lat)
        {
            var result = _wgs84ToDrawing.Transform(lon, lat);
            return (result.x, result.y);
        }

        /// <summary>
        /// Transform the two-corner bounding box from drawing CRS to WGS84.
        /// Returns (minLon, minLat, maxLon, maxLat).
        /// </summary>
        public BoundingBox DrawingBboxToWgs84(double x1, double y1, double x2, double y2)
        {
            var (lon1, lat1) = DrawingToWgs84(x1, y1);
            var (lon2, lat2) = DrawingToWgs84(x2, y2);

            return new BoundingBox(
                Math.Min(lon1, lon2),
                Math.Min(lat1, lat2),
                Math.Max(lon1, lon2),
                Math.Max(lat1, lat2));
        }
    }

    public class BoundingBox
    {
        public BoundingBox(double minLon, double minLat, double maxLon, double maxLat)
        {
            MinLon = minLon;
            MinLat = minLat;
            MaxLon = maxLon;
            MaxLat = maxLat;
        }

        public double MinLon { get; }
        public double MinLat { get; }
        public double MaxLon { get; }
        public double MaxLat { get; }

        public double Width => MaxLon - MinLon;
        public double Height => MaxLat - MinLat;
        public double CenterLon => (MinLon + MaxLon) / 2.0;
        public double CenterLat => (MinLat + MaxLat) / 2.0;

        public override bool Equals(object obj)
        {
            if (obj is BoundingBox other)
            {
                return MinLon == other.MinLon &&
                       MinLat == other.MinLat &&
                       MaxLon == other.MaxLon &&
                       MaxLat == other.MaxLat;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + MinLon.GetHashCode();
                hash = hash * 31 + MinLat.GetHashCode();
                hash = hash * 31 + MaxLon.GetHashCode();
                hash = hash * 31 + MaxLat.GetHashCode();
                return hash;
            }
        }
    }
}
