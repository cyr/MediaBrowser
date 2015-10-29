﻿using MediaBrowser.Controller;
using MediaBrowser.Model.Logging;
using System;
using System.Diagnostics;

namespace MediaBrowser.Server.Startup.Common.Browser
{
    /// <summary>
    /// Class BrowserLauncher
    /// </summary>
    public static class BrowserLauncher
    {
        /// <summary>
        /// Opens the dashboard page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public static void OpenDashboardPage(string page, IServerApplicationHost appHost, ILogger logger)
        {
            var url = appHost.GetLocalApiUrl("localhost") + "/web/" + page;

            OpenUrl(url, logger);
        }

        /// <summary>
        /// Opens the community.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public static void OpenCommunity(ILogger logger)
        {
            OpenUrl("http://emby.media/community", logger);
        }

        /// <summary>
        /// Opens the web client.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public static void OpenWebClient(IServerApplicationHost appHost, ILogger logger)
        {
            OpenDashboardPage("index.html", appHost, logger);
        }

        /// <summary>
        /// Opens the dashboard.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public static void OpenDashboard(IServerApplicationHost appHost, ILogger logger)
        {
            OpenDashboardPage("dashboard.html", appHost, logger);
        }

        /// <summary>
        /// Opens the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="logger">The logger.</param>
        private static void OpenUrl(string url, ILogger logger)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = url
                },

                EnableRaisingEvents = true,
            };

            process.Exited += ProcessExited;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error launching url: {0}", ex, url);

                Console.WriteLine("Error launching url: {0}", ex.Message);
                Console.WriteLine(ex.Message);

//#if !__MonoCS__
//                System.Windows.Forms.MessageBox.Show("There was an error launching your web browser. Please check your default browser settings.");
//#endif
            }
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void ProcessExited(object sender, EventArgs e)
        {
            ((Process)sender).Dispose();
        }
    }
}
