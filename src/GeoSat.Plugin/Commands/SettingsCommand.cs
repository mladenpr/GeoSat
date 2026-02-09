using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GeoSat.Plugin.AutoCAD;

namespace GeoSat.Plugin.Commands
{
    /// <summary>
    /// GEOSATSET command — configure API credentials and output directory.
    /// For MVP, uses command-line prompts. Can be replaced with WPF dialog later.
    /// </summary>
    public class SettingsCommand
    {
        [CommandMethod("GEOSATSET")]
        public void Configure()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var editor = doc.Editor;

            var settings = SettingsStore.Load();

            editor.WriteMessage("\n=== GeoSat Settings ===\n");

            // Client ID
            var clientIdOpt = new PromptStringOptions($"\nSentinel Hub Client ID [{Mask(settings.ClientId)}]: ")
            {
                AllowSpaces = false,
            };
            var clientIdResult = editor.GetString(clientIdOpt);
            if (clientIdResult.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(clientIdResult.StringResult))
                settings.ClientId = clientIdResult.StringResult;

            // Client Secret
            var secretOpt = new PromptStringOptions($"\nSentinel Hub Client Secret [{Mask(settings.ClientSecret)}]: ")
            {
                AllowSpaces = false,
            };
            var secretResult = editor.GetString(secretOpt);
            if (secretResult.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(secretResult.StringResult))
                settings.ClientSecret = secretResult.StringResult;

            // Instance ID
            var instanceOpt = new PromptStringOptions($"\nSentinel Hub Instance ID [{Mask(settings.InstanceId)}]: ")
            {
                AllowSpaces = false,
            };
            var instanceResult = editor.GetString(instanceOpt);
            if (instanceResult.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(instanceResult.StringResult))
                settings.InstanceId = instanceResult.StringResult;

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
