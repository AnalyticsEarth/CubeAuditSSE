using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using NLog;
using System.Configuration;
using System.Threading;
using Prometheus;

namespace CubeAuditSSE
{
    static class CubeAuditMetrics
    {
        public static MetricServer MetricServer;
        public static readonly Gauge UpGauge = Metrics.CreateGauge("CubeAuditSSE_IsLogging", "Specifies whether logging is enabled or not, based up error status of the logging connector. 1: Enabled, 0: Disabled");
    }

    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {

            CubeAuditMetrics.MetricServer = new MetricServer(19345);
            CubeAuditMetrics.MetricServer.Start();

            CubeAuditMetrics.UpGauge.Set(1);



            Logger.Info(
                $"{Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location)} uses NLog. Set log level by adding or changing logger rules in NLog.config, setting minLevel=\"Info\" or \"Debug\" or \"Trace\".");

            Logger.Info(
                $"Changes to NLog config are immediately reflected in running application, unless you change the setting autoReload=\"true\".");

            var appSettings = ConfigurationManager.AppSettings;

            var grpcHost = appSettings["grpcHost"];
            var grpcPort = Convert.ToInt32(appSettings["grpcPort"]);
            var certificateFolder = appSettings["certificateFolder"];

            ServerCredentials sslCredentials = null;

            Logger.Info("Looking for certificates according to certificateFolderFullPath in config file.");

            if (certificateFolder.Length > 3)
            {
                var rootCertPath = Path.Combine(certificateFolder, @"root_cert.pem");
                var serverCertPath = Path.Combine(certificateFolder, @"sse_server_cert.pem");
                var serverKeyPath = Path.Combine(certificateFolder, @"sse_server_key.pem");
                if (File.Exists(rootCertPath) &&
                    File.Exists(serverCertPath) &&
                    File.Exists(serverKeyPath))
                {
                    var rootCert = File.ReadAllText(rootCertPath);
                    var serverCert = File.ReadAllText(serverCertPath);
                    var serverKey = File.ReadAllText(serverKeyPath);
                    var serverKeyPair = new KeyCertificatePair(serverCert, serverKey);
                    sslCredentials = new SslServerCredentials(new List<KeyCertificatePair>() { serverKeyPair }, rootCert, true);

                    Logger.Info($"Path to certificates ({certificateFolder}) and certificate files found. Opening secure channel with mutual authentication.");
                }
                else
                {
                    Logger.Error($"Path to certificates ({certificateFolder}) not found or files missing. The gRPC server will not be started.");
                    sslCredentials = null;
                }
            }
            else
            {
                Logger.Info("No certificates defined. Opening insecure channel.");
                sslCredentials = ServerCredentials.Insecure;
            }

            if (sslCredentials != null)
            {
                var server = new Grpc.Core.Server
                {
                    Services = { Qlik.Sse.Connector.BindService(new CubeAuditConnector()) },
                    Ports = { new ServerPort(grpcHost, grpcPort, sslCredentials) }
                };

                server.Start();
                Logger.Info($"gRPC listening on port {grpcPort}");

                //Logger.Info("Press any key to stop gRPC server and exit...");

                try {
                      while(true) {
                        Thread.Sleep(10000);
                      }
                    } finally {
                      Logger.Info("Shutting down Connector");
                      server.ShutdownAsync().Wait();

                    }
                
            }
            else
            {
                //Logger.Info("Press any key to exit...");

                //Console.ReadKey();
            }

        }
    }
}
