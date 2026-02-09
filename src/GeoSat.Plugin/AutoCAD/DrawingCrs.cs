using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using GeoSat.Core.Coordinates;

namespace GeoSat.Plugin.AutoCAD
{
    /// <summary>
    /// Stores and retrieves the CRS setting for a drawing using AutoCAD's
    /// custom properties (Summary Info). This persists with the .dwg file.
    /// </summary>
    public static class DrawingCrs
    {
        private const string CrsPropertyName = "GeoSat_CRS";

        /// <summary>Save the selected CRS code to the drawing's custom properties.</summary>
        public static void Save(Document doc, CrsEntry crs)
        {
            using (doc.LockDocument())
            {
                var db = doc.Database;
                var info = new DatabaseSummaryInfoBuilder(db.SummaryInfo);
                info.CustomPropertyTable[CrsPropertyName] = crs.Code;
                db.SummaryInfo = info.ToDatabaseSummaryInfo();
            }
        }

        /// <summary>Try to load a previously saved CRS from the drawing. Returns null if not set.</summary>
        public static CrsEntry Load(Document doc)
        {
            var db = doc.Database;
            var props = db.SummaryInfo.CustomProperties;

            while (props.MoveNext())
            {
                var entry = props.Entry;
                if (entry.Key?.ToString() == CrsPropertyName)
                {
                    var code = entry.Value?.ToString();
                    return CrsRegistry.All.FirstOrDefault(c => c.Code == code);
                }
            }
            return null;
        }
    }
}
