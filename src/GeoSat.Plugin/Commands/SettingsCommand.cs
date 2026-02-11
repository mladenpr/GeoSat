using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GeoSat.Core.Coordinates;
using GeoSat.Plugin.AutoCAD;

namespace GeoSat.Plugin.Commands
{
    /// <summary>
    /// GEOSATSET command — configure API credentials, output directory, and drawing CRS.
    /// For MVP, uses command-line prompts. Can be replaced with WPF dialog later.
    /// </summary>
    public class SettingsCommand
    {
        [CommandMethod("GEOSATSET", CommandFlags.Session)]
        public void Configure()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var editor = doc.Editor;

            var settings = SettingsStore.Load();

            editor.WriteMessage("\n=== GeoSat Settings ===\n");

            // --- CRS for this drawing ---
            var currentCrs = DrawingCrs.Load(doc);
            editor.WriteMessage($"\nCurrent drawing CRS: {(currentCrs != null ? currentCrs.ToString() : "not set")}\n");

            var options = CrsRegistry.All;
            editor.WriteMessage("Select coordinate system:\n");
            for (int i = 0; i < options.Count; i++)
                editor.WriteMessage($"  {i + 1}. {options[i]}\n");

            var crsOpt = new PromptIntegerOptions($"\nEnter choice [1-{options.Count}] or 0 to keep current: ")
            {
                LowerLimit = 0,
                UpperLimit = options.Count,
            };
            var crsResult = editor.GetInteger(crsOpt);
            if (crsResult.Status == PromptStatus.OK && crsResult.Value > 0)
            {
                var newCrs = options[crsResult.Value - 1];
                DrawingCrs.Save(doc, newCrs);
                editor.WriteMessage($"\n[GeoSat] CRS set to {newCrs}.\n");
            }

            // --- Imagery provider ---
            var currentProvider = settings.Provider == ImageryProvider.Mapbox ? "Mapbox" : "Sentinel Hub";
            editor.WriteMessage($"\nCurrent imagery provider: {currentProvider}\n");
            editor.WriteMessage("  1. Mapbox Satellite\n");
            editor.WriteMessage("  2. Sentinel Hub (Copernicus)\n");

            var provOpt = new PromptIntegerOptions("\nSelect provider [1-2] or 0 to keep current: ")
            {
                LowerLimit = 0,
                UpperLimit = 2,
            };
            var provResult = editor.GetInteger(provOpt);
            if (provResult.Status == PromptStatus.OK && provResult.Value > 0)
            {
                settings.Provider = provResult.Value == 1 ? ImageryProvider.Mapbox : ImageryProvider.Sentinel;
            }

            // --- Provider-specific credentials ---
            if (settings.Provider == ImageryProvider.Mapbox)
            {
                var tokenOpt = new PromptStringOptions($"\nMapbox Access Token [{Mask(settings.MapboxAccessToken)}]: ")
                {
                    AllowSpaces = false,
                };
                var tokenResult = editor.GetString(tokenOpt);
                if (tokenResult.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(tokenResult.StringResult))
                    settings.MapboxAccessToken = tokenResult.StringResult;
            }
            else
            {
                // Sentinel Hub credentials
                var clientIdOpt = new PromptStringOptions($"\nSentinel Hub Client ID [{Mask(settings.ClientId)}]: ")
                {
                    AllowSpaces = false,
                };
                var clientIdResult = editor.GetString(clientIdOpt);
                if (clientIdResult.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(clientIdResult.StringResult))
                    settings.ClientId = clientIdResult.StringResult;

                var secretOpt = new PromptStringOptions($"\nSentinel Hub Client Secret [{Mask(settings.ClientSecret)}]: ")
                {
                    AllowSpaces = false,
                };
                var secretResult = editor.GetString(secretOpt);
                if (secretResult.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(secretResult.StringResult))
                    settings.ClientSecret = secretResult.StringResult;

                var instanceOpt = new PromptStringOptions($"\nSentinel Hub Instance ID [{Mask(settings.InstanceId)}]: ")
                {
                    AllowSpaces = false,
                };
                var instanceResult = editor.GetString(instanceOpt);
                if (instanceResult.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(instanceResult.StringResult))
                    settings.InstanceId = instanceResult.StringResult;
            }

            // Output directory
            var outDirOpt = new PromptStringOptions($"\nOutput directory [{settings.OutputDirectory}]: ")
            {
                AllowSpaces = true,
            };
            var outDirResult = editor.GetString(outDirOpt);
            if (outDirResult.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(outDirResult.StringResult))
                settings.OutputDirectory = outDirResult.StringResult;

            SettingsStore.Save(settings);
            editor.WriteMessage("\n[GeoSat] Settings saved.\n");
        }

        private static string Mask(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "not set";
            return value.Substring(0, Math.Min(4, value.Length)) + "****";
        }
    }
}
