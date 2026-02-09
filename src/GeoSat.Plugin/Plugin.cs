using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(GeoSat.Plugin.Plugin))]
[assembly: CommandClass(typeof(GeoSat.Plugin.Commands.GeoSatCommand))]
[assembly: CommandClass(typeof(GeoSat.Plugin.Commands.SettingsCommand))]

namespace GeoSat.Plugin
{
    /// <summary>
    /// Entry point for the GeoSat AutoCAD plugin.
    /// AutoCAD calls Initialize() when the DLL is loaded via NETLOAD.
    /// </summary>
    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            var editor = Autodesk.AutoCAD.ApplicationServices.Core.Application
                .DocumentManager.MdiActiveDocument?.Editor;

            editor?.WriteMessage("\n[GeoSat] Plugin loaded. Commands: GEOSAT, GEOSATSET\n");
        }

        public void Terminate()
        {
            // Clean up resources if needed
        }
    }
}
