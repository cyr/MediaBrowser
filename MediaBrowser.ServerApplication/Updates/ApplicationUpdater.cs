﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using System.Diagnostics;
using System.IO;

namespace MediaBrowser.ServerApplication.Updates
{
    /// <summary>
    /// Update the specified application using the specified archive
    /// </summary>
    public class ApplicationUpdater
    {
        private const string UpdaterExe = "Mediabrowser.Updater.exe";
        private const string UpdaterDll = "Mediabrowser.InstallUtil.dll";
        public void UpdateApplication(IApplicationPaths appPaths, string archive, ILogger logger, string restartServiceName)
        {
            // First see if there is a version file and read that in
            var version = "Unknown";
            if (File.Exists(archive + ".ver"))
            {
                version = File.ReadAllText(archive + ".ver");
            }

            var systemPath = appPaths.ProgramSystemPath;
            var tempPath = Path.GetTempPath();

            // Use our installer passing it the specific archive
            // We need to copy to a temp directory and execute it there
            var source = Path.Combine(systemPath, UpdaterExe);

            logger.Info("Copying updater to temporary location");
            var tempUpdater = Path.Combine(tempPath, UpdaterExe);
            File.Copy(source, tempUpdater, true);
            source = Path.Combine(systemPath, UpdaterDll);
            var tempUpdaterDll = Path.Combine(tempPath, UpdaterDll);

            logger.Info("Copying updater dependencies to temporary location");
            File.Copy(source, tempUpdaterDll, true);
            var product = "server";
            // Our updater needs SS and ionic
            source = Path.Combine(systemPath, "ServiceStack.Text.dll");
            File.Copy(source, Path.Combine(tempPath, "ServiceStack.Text.dll"), true);
            source = Path.Combine(systemPath, "SharpCompress.dll");
            File.Copy(source, Path.Combine(tempPath, "SharpCompress.dll"), true);

            logger.Info("Starting updater process.");

            // installpath = program data folder
            // startpath = executable to launch
            // systempath = folder containing installation
            var args = string.Format("product={0} archive=\"{1}\" caller={2} pismo=false version={3} service={4} installpath=\"{5}\" startpath=\"{6}\" systempath=\"{7}\"",
                    product, archive, Process.GetCurrentProcess().Id, version, restartServiceName ?? string.Empty, appPaths.ProgramDataPath, appPaths.ApplicationPath, systemPath);

            logger.Info("Args: {0}", args);
            Process.Start(tempUpdater, args);

            // That's it.  The installer will do the work once we exit
        }
    }
}
