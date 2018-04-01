using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace CubeAuditSSE
{
    class DynamodbAuditLogConnector : AuditLogConnector
    {
        private static bool canLogToDynamoDB;
        private static bool readyToLogToDynamoDB;

        //private static StreamWriter outputFile;

        public static IConfiguration Configuration { get; set; }
        public static AmazonDynamoDBClient client;

        public static Dictionary<string, AttributeValue> responseDict;
        public static List<AttributeValue> responseValues;

        public DynamodbAuditLogConnector()
        {
            canLogToDynamoDB = true;
            readyToLogToDynamoDB = false;


        }
        public override bool LogRequest(Guid g, string appId, string userId)
        {
            if (isLogging)
            {
                responseDict = new Dictionary<string, AttributeValue>();
                responseDict.Add("request_guid", new AttributeValue { S = g.ToString() });
                responseDict.Add("request_timestamp", new AttributeValue { S = DateTime.UtcNow.ToString() });
                responseDict.Add("userid", new AttributeValue { S = GetShortUserId(userId) });
                responseDict.Add("appid", new AttributeValue { S = appId });
                setLoggingStatus(true);
            }
            else
            {
                setLoggingStatus(false);
            }
            
            
            return true;
        }

        public override bool StartLogData(Guid g, string appId, string userId)
        {
            //var user = GetShortUserId(userId);
            //var folder = Path.Combine(new string[] { fileFolder, DateTime.Now.ToString("yyyyMMdd"), appId, user, $"{ DateTime.Now.ToString("yyyyMMdd") }_{ appId }_{ user }_{ g }.txt" });
            //Logger.Trace($"Logging to file: {folder}");
            //Directory.CreateDirectory(Path.GetDirectoryName(folder));
            //outputFile = new StreamWriter(folder, true);

            //Logger.Trace($"Logging to DynamoDB");

            responseValues = new List<AttributeValue>();

            return true;
        }

        public override bool LogData(Guid g, string data)
        {
            //outputFile.WriteLine($"{DateTime.Now}|{g}|{data}");
            responseValues.Add(new AttributeValue { S = data });

            return true;
        }

        public override bool EndLogData()
        {
            

            while (canLogToDynamoDB)
            {
                if (readyToLogToDynamoDB)
                {
                    Logger.Trace("Sending to DynamoDB");
                    responseDict.Add("values", new AttributeValue { L = responseValues });
                    client.PutItemAsync(
                        tableName: "cubeauditsse",
                        item: responseDict
                    );
                    Logger.Trace("Logged to DynamoDB");
                    break;
                }
                else
                {
                    Thread.Sleep(50);
                }
            }

            if (!canLogToDynamoDB)
            {
                Logger.Debug("Could not Log to DynamoDB");
                return false;
            }

            return true;

        }

        public override async Task ConfigureAsync()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddXmlFile("aws.xml");

            Configuration = builder.Build();

            var accessKey = Configuration["key:accesskey"];
            var secretKey = Configuration["key:secretkey"];
            var awsRegion = Configuration["region"];

            Logger.Trace($"ACCESS: {accessKey}");

            try
            {
                var credentials = new BasicAWSCredentials(accessKey, secretKey);
                client = new AmazonDynamoDBClient(credentials, RegionEndpoint.GetBySystemName(awsRegion));

                var tableResponse = await client.ListTablesAsync();


                if (tableResponse.TableNames.Contains("cubeauditsse"))
                {
                    canLogToDynamoDB = true;
                    readyToLogToDynamoDB = true;
                    isLogging = true;
                }
                else
                {
                    canLogToDynamoDB = false;
                    isLogging = false;
                    Logger.Trace($"Table NOT in dynamodb");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"ERROR: {e.Message}");
                canLogToDynamoDB = false;
                isLogging = false;
            }
            


        }




    }
}
