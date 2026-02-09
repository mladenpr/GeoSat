using System;
using System.IO;
using System.Reflection;
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
        private static readonly string PluginDir =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public void Initialize()
        {
            // AutoCAD is the host process and ignores plugin binding redirects.
            // Handle version mismatches (e.g. System.Runtime.CompilerServices.Unsafe
            // 4.0.4.1 requested but 6.0.0.0 ships) by loading from the plugin directory.
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            var editor = Autodesk.AutoCAD.ApplicationServices.Core.Application
                .DocumentManager.MdiActiveDocument?.Editor;

            editor?.WriteMessage("\n[GeoSat] Plugin loaded. Commands: GEOSAT, GEOSATSET\n");
        }

        public void Terminate()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var path = Path.Combine(PluginDir, assemblyName.Name + ".dll");

            if (File.Exists(path))
                return Assembly.LoadFrom(path);

            return null;
        }
    }
}
