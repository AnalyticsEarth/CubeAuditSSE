using System;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using Amazon.DynamoDBv2;



namespace CubeAuditSSE
{
    class DynamodbAuditLogConnector : AuditLogConnector
    {
        private static string fileFolder;
        private static int pattern;

        private static StreamWriter outputFile;

        public DynamodbAuditLogConnector()
        {
            var appSettings = ConfigurationManager.AppSettings;
            //fileFolder = appSettings["fileLogFolderWindows"];

            
        }
        public override bool LogRequest(Guid g, string appId, string userId)
        {
            //All Logging is done in the data request
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

        private bool configureDynamodb()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets("RemoteDynamoDB")
                .Build();
            var accessKey = config["aws-access-key"];
            var secretKey = config["aws-secret-key"];
            client = BuildClient(accessKey, secretKey);

            return true;
        }

        private AmazonDynamoDBClient BuildClient(string accessKey, string secretKey)
    {
        Console.WriteLine("Creating DynamoDB client...");
        var credentials = new BasicAWSCredentials(
            accessKey: accessKey,
            secretKey: secretKey);
        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.USWest2
        };
        return new AmazonDynamoDBClient(credentials, config);
    }


    }
}
