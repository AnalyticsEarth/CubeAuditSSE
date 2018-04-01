using NLog;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prometheus;

namespace CubeAuditSSE
{
    

    public abstract class AuditLogConnector
    {
        public static bool isLogging = true;

        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public abstract bool LogRequest(Guid g, string appId, string userId);

        public abstract bool StartLogData(Guid g, string appId, string userId);

        public abstract bool LogData(Guid g, string data);

        public abstract bool EndLogData();

        public string GetShortUserId(string longUserId)
        {
            var splitUserId = longUserId.Split(new String[] { ";" }, StringSplitOptions.None);
            var directory = splitUserId[0].Substring(14);
            var user = splitUserId[1].Substring(8);
            return $"{directory}_{user}";
        }

        public abstract Task ConfigureAsync();

    }
}
