using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;

namespace GeoSat.Core.Coordinates
{
    /// <summary>
    /// Registry of commonly used coordinate reference systems for CAD drawings.
    /// Users pick one of these to tell the plugin what CRS their drawing uses.
    /// </summary>
    public static class CrsRegistry
    {
        public static readonly CrsEntry Wgs84LatLon = new CrsEntry(
            "EPSG:4326", "WGS 84 (Lat/Lon)",
            GeographicCoordinateSystem.WGS84);

        public static readonly CrsEntry WebMercator = new CrsEntry(
            "EPSG:3857", "Web Mercator",
            ProjectedCoordinateSystem.WebMercator);

        // UTM Zones — add the ones your team commonly uses.
        // These cover most of Europe; extend as needed.
        public static readonly CrsEntry Utm32N = new CrsEntry(
            "EPSG:32632", "UTM Zone 32N",
            ProjectedCoordinateSystem.WGS84_UTM(32, true));

        public static readonly CrsEntry Utm33N = new CrsEntry(
            "EPSG:32633", "UTM Zone 33N",
            ProjectedCoordinateSystem.WGS84_UTM(33, true));

        public static readonly CrsEntry Utm34N = new CrsEntry(
            "EPSG:32634", "UTM Zone 34N",
            ProjectedCoordinateSystem.WGS84_UTM(34, true));

        public static readonly CrsEntry Utm35N = new CrsEntry(
            "EPSG:32635", "UTM Zone 35N",
            ProjectedCoordinateSystem.WGS84_UTM(35, true));

        /// <summary>All registered CRS entries the user can pick from.</summary>
        public static IReadOnlyList<CrsEntry> All { get; } = new List<CrsEntry>
        {
            Wgs84LatLon,
            WebMercator,
            Utm32N,
            Utm33N,
            Utm34N,
            Utm35N,
        };

        /// <summary>Create a UTM zone entry on the fly.</summary>
        public static CrsEntry UtmZone(int zone, bool north)
        {
            return new CrsEntry(
                $"EPSG:{(north ? 32600 : 32700) + zone}",
                $"UTM Zone {zone}{(north ? 'N' : 'S')}",
                ProjectedCoordinateSystem.WGS84_UTM(zone, north));
        }
    }

    public class CrsEntry
    {
        public CrsEntry(string code, string displayName, CoordinateSystem coordinateSystem)
        {
            Code = code;
            DisplayName = displayName;
            CoordinateSystem = coordinateSystem;
        }

        public string Code { get; }
        public string DisplayName { get; }
        public CoordinateSystem CoordinateSystem { get; }

        public override string ToString() => $"{Code} — {DisplayName}";

        public override bool Equals(object obj)
        {
            if (obj is CrsEntry other)
            {
                return Code == other.Code &&
                       DisplayName == other.DisplayName &&
                       CoordinateSystem == other.CoordinateSystem;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (Code != null ? Code.GetHashCode() : 0);
                hash = hash * 31 + (DisplayName != null ? DisplayName.GetHashCode() : 0);
                hash = hash * 31 + (CoordinateSystem != null ? CoordinateSystem.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
