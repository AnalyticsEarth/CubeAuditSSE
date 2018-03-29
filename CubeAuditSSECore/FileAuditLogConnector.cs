using System;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;


namespace CubeAuditSSE
{
    class FileAuditLogConnector : AuditLogConnector
    {
        private static string fileFolder;
        private static int pattern;

        private static StreamWriter outputFile;

        public FileAuditLogConnector()
        {
            var appSettings = ConfigurationManager.AppSettings;

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if(isWindows){
                fileFolder = appSettings["fileLogFolderWindows"];
            }else{
                fileFolder = appSettings["fileLogFolderLinux"];
            }
            
            pattern = Convert.ToInt32(appSettings["fileLogPattern"]);
        }
        public override bool LogRequest(Guid g, string appId, string userId)
        {
            var user = GetShortUserId(userId);
            var folder = Path.Combine(new string[] { fileFolder, DateTime.Now.ToString("yyyyMMdd"), appId, user, $"{DateTime.Now.ToString("yyyyMMdd")}_{appId}_{user}_REQUEST.txt" });
            Logger.Trace($"Logging to file: {folder}");
            Directory.CreateDirectory(Path.GetDirectoryName(folder));
            StreamWriter requestFile = new StreamWriter(folder, true);
            requestFile.WriteLine($"{DateTime.Now}|{g}|{appId}|{userId}");
            requestFile.Close();
            return true;
        }

        public override bool StartLogData(Guid g, string appId, string userId)
        {
            var user = GetShortUserId(userId);
            var folder = Path.Combine(new string[] { fileFolder, DateTime.Now.ToString("yyyyMMdd"), appId, user, $"{ DateTime.Now.ToString("yyyyMMdd") }_{ appId }_{ user }_{ g }.txt" });
            Logger.Trace($"Logging to file: {folder}");
            Directory.CreateDirectory(Path.GetDirectoryName(folder));
            outputFile = new StreamWriter(folder, true);
            return true;
        }

        public override bool LogData(Guid g, string data)
        {
            outputFile.WriteLine($"{DateTime.Now}|{g}|{data}");
            return true;
        }

        public override bool EndLogData()
        {
            outputFile.Close();
            return true;
        }


    }
}
