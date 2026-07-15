using Grpc.Core;
using Microsoft.Extensions.Configuration;
using PlateLocCommandExecutor.Services;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PlateLocCommandExecutor
{
    static class Program
    {
        private static readonly string programMutexName = "Global\\PlateLocCommandExecutor";

        /// <summary>
        /// Main entry point
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Single instance check.
            Mutex mutex = new Mutex(true, programMutexName, out bool wasMutexCreated);
            if (!wasMutexCreated)
            {
                var message = "Another instance is running. Exiting.";
                Log.Information(message);
                MessageBox.Show(message, "PlateLocCommandExecutor");
                mutex.Close();
                return;
            }

            try
            {
                // Log settings
                // Serilog is licensed under the Apache License.
                // The license information is displayed from the notification icon's context menu (NoticeToolStripMenuItem). 
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("application.json")
                    .Build();
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(configuration)
                    .CreateLogger();
                var appConfiguration = configuration.GetSection("PlateLocCommandExecutor");

                Log.Information("*** Configuration loaded. ***");

                // gRPC server setup
                Log.Information("launching Grpc server...");
                Server grpcServer = new Server();
                grpcServer.Services.Add(PlateLocCommandExecutorService.BindService(PlateLocCommandExecutorServiceImpl.GetService()));
                grpcServer.Ports.Add("localhost", int.Parse(appConfiguration["Port"]), ServerCredentials.Insecure);

                // Form setup for ActiveX control
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (var form = new PlateLocForm())
                {
                    PlateLocCommandExecutorServiceImpl.GetService().Initialize(form, appConfiguration);
                    grpcServer.Start();
                    Application.Run();
                    PlateLocCommandExecutorServiceImpl.GetService().Close();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Unexpected error occurred: {e.GetType().Name} {e.Message}\n{e.StackTrace}");
                throw;
            }
            finally
            {
                mutex.ReleaseMutex();
                mutex.Close();
            }
        }
    }
}
