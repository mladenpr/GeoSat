using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GeoSat.Core;
using GeoSat.Core.Coordinates;
using GeoSat.Plugin.AutoCAD;

namespace GeoSat.Plugin.Commands
{
    /// <summary>
    /// Main GEOSAT command — the full pipeline:
    /// 1. Ensure CRS is set for the drawing
    /// 2. User picks two corners
    /// 3. Fetch satellite imagery
    /// 4. Insert georeferenced raster into drawing
    /// </summary>
    public class GeoSatCommand
    {
        [CommandMethod("GEOSAT", CommandFlags.Session)]
        public async void Execute()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var editor = doc.Editor;

            try
            {
                // --- Step 1: Resolve CRS ---
                var crs = DrawingCrs.Load(doc);
                if (crs == null)
                {
                    editor.WriteMessage("\n[GeoSat] No coordinate system set for this drawing.");
                    crs = PromptForCrs(editor);
                    if (crs == null)
                    {
                        editor.WriteMessage("\n[GeoSat] Cancelled.\n");
                        return;
                    }
                    DrawingCrs.Save(doc, crs);
                    editor.WriteMessage($"\n[GeoSat] CRS set to {crs}.\n");
                }
                else
                {
                    editor.WriteMessage($"\n[GeoSat] Using CRS: {crs}\n");
                }

                // --- Step 2: Load settings and validate ---
                var settings = SettingsStore.Load();
                var config = SettingsStore.ToSentinelConfig(settings);
                if (config == null)
                {
                    editor.WriteMessage("\n[GeoSat] API credentials not configured. Run GEOSATSET first.\n");
                    return;
                }

                // --- Step 3: Pick area ---
                var corners = AreaSelector.PickTwoCorners(editor);
                if (corners == null)
                {
                    editor.WriteMessage("\n[GeoSat] Cancelled.\n");
                    return;
                }

                var (c1, c2) = corners.Value;
                editor.WriteMessage(
                    $"\n[GeoSat] Area: ({c1.X:F2}, {c1.Y:F2}) to ({c2.X:F2}, {c2.Y:F2})\n");

                // --- Step 4: Fetch imagery ---
                editor.WriteMessage("\n[GeoSat] Fetching satellite imagery...\n");

                var progress = new Progress<(int Done, int Total)>(p =>
                    editor.WriteMessage($"\r[GeoSat] Downloading tiles: {p.Done}/{p.Total}"));

                using (var engine = new GeoSatEngine(crs, config))
                {
                    var result = await engine.FetchImageryAsync(
                        c1.X, c1.Y, c2.X, c2.Y,
                        settings.OutputDirectory,
                        progress);

                    editor.WriteMessage(
                        $"\n[GeoSat] Downloaded {result.TileCount} tiles at zoom {result.ZoomLevel}. " +
                        $"Image: {result.ImageWidthPx}x{result.ImageHeightPx}px\n");

                    // --- Step 5: Insert into drawing ---
                    editor.WriteMessage("[GeoSat] Inserting image into drawing...\n");

                    using (doc.LockDocument())
                    {
                        RasterInserter.Insert(doc, result);
                    }

                    editor.WriteMessage($"[GeoSat] Done! Image placed at ({result.InsertionPointX:F2}, {result.InsertionPointY:F2})\n");
                    editor.WriteMessage($"[GeoSat] Image file: {result.ImagePath}\n");
                }

                // Zoom to the inserted image
                editor.Command("_.ZOOM", "_E");
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage($"\n[GeoSat] Error: {ex.Message}\n");
            }
        }

        private static CrsEntry PromptForCrs(Editor editor)
        {
            editor.WriteMessage("\nSelect coordinate system for this drawing:\n");
            var options = CrsRegistry.All;
            for (int i = 0; i < options.Count; i++)
            {
                editor.WriteMessage($"  {i + 1}. {options[i]}\n");
            }

            var opts = new PromptIntegerOptions($"\nEnter choice [1-{options.Count}]: ")
            {
                LowerLimit = 1,
                UpperLimit = options.Count,
            };

            var result = editor.GetInteger(opts);
            if (result.Status != PromptStatus.OK)
                return null;

            return options[result.Value - 1];
        }
    }
}
