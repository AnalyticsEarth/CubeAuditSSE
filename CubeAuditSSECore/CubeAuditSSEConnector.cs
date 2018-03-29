using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Utils;
using NLog;
using Qlik.Sse;


namespace CubeAuditSSE
{
    /// <summary>
    /// The BasicExampleConnector inherits the generated class Qlik.Sse.Connector.ConnectorBase
    /// </summary>
    class CubeAuditConnector : Connector.ConnectorBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static AuditLogConnector logConnector;

        private static string logType;

        public CubeAuditConnector()
        {
            var appSettings = ConfigurationManager.AppSettings;

            logType = appSettings["logType"];

        }

        private enum FunctionConstant
        {
            LogAsSeenStrCheck,
            LogAsSeenStrEcho,
            LogAsSeenCheck,
            LogAsSeenEcho
        };

        private static readonly Capabilities ConnectorCapabilities = new Capabilities
        {
            PluginIdentifier = "CubeAuditSSE",
            PluginVersion = "1.0.0",
            AllowScript = false,
            Functions =
            {
                new FunctionDefinition {
                    FunctionId = (int)FunctionConstant.LogAsSeenStrCheck,
                    FunctionType = FunctionType.Scalar,
                    Name = "LogAsSeenStrCheck",
                    Params = { new Parameter { Name = "Field", DataType = DataType.String }},
                    ReturnType = DataType.String
                },
                new FunctionDefinition {
                    FunctionId = (int)FunctionConstant.LogAsSeenStrEcho,
                    FunctionType = FunctionType.Scalar,
                    Name = "LogAsSeenStrEcho",
                    Params = { new Parameter { Name = "Field", DataType = DataType.String }},
                    ReturnType = DataType.String
                },
                new FunctionDefinition {
                    FunctionId = (int)FunctionConstant.LogAsSeenCheck,
                    FunctionType = FunctionType.Scalar,
                    Name = "LogAsSeenCheck",
                    Params = { new Parameter { Name = "Field", DataType = DataType.Numeric }},
                    ReturnType = DataType.String
                },
                new FunctionDefinition {
                    FunctionId = (int)FunctionConstant.LogAsSeenEcho,
                    FunctionType = FunctionType.Scalar,
                    Name = "LogAsSeenEcho",
                    Params = { new Parameter { Name = "Field", DataType = DataType.Numeric }},
                    ReturnType = DataType.Numeric
                },
            }
        };

        public override Task<Capabilities> GetCapabilities(Empty request, ServerCallContext context)
        {
            if (Logger.IsTraceEnabled)
            {
                Logger.Trace("-- GetCapabilities --");

                TraceServerCallContext(context);
            }
            else
            {
                Logger.Debug("GetCapabilites called");
            }

            return Task.FromResult(ConnectorCapabilities);
        }

        private Row LogRow(AuditLogConnector logConnector, Guid g, String appId, String userId, string strData, string returnData)
        {
            var resultRow = new Row();
            Logger.Trace($"     {g} : {strData}");
            resultRow.Duals.Add(new Dual { StrData = returnData });
            logConnector.LogData(g, strData);
            return resultRow;
        }

        private Row LogRow(AuditLogConnector logConnector, Guid g, String appId, String userId, double numData, string returnData)
        {
            var resultRow = new Row();
            Logger.Trace($"     {g} : {numData.ToString()}");
            resultRow.Duals.Add(new Dual { StrData = returnData });
            logConnector.LogData(g, numData.ToString());
            return resultRow;
        }

        private Row LogRow(AuditLogConnector logConnector, Guid g, String appId, String userId, string strData, double returnData)
        {
            var resultRow = new Row();
            Logger.Trace($"     {g} : {strData}");
            resultRow.Duals.Add(new Dual { NumData = returnData });
            logConnector.LogData(g, strData);
            return resultRow;
        }

        private Row LogRow(AuditLogConnector logConnector, Guid g, String appId, String userId, double numData, double returnData)
        {
            var resultRow = new Row();
            Logger.Trace($"     {g} : {numData.ToString()}");
            resultRow.Duals.Add(new Dual { NumData = returnData });
            logConnector.LogData(g, numData.ToString());
            return resultRow;
        }

        private Guid LogRequest(AuditLogConnector logConnector, String appId, String userId)
        {
            Guid g = Guid.NewGuid();
            Logger.Trace($"{DateTime.Now} Log Request Here with GUID: {g}");
            logConnector.LogRequest(g, appId, userId);
            logConnector.StartLogData(g, appId, userId);
            return g;
        }

        public override async Task ExecuteFunction(IAsyncStreamReader<BundledRows> requestStream, IServerStreamWriter<BundledRows> responseStream, ServerCallContext context)
        {
            Logger.Trace("-- ExecuteFunction --");

            if (logType == "file")
            {
                logConnector = new FileAuditLogConnector();
            }

            Dictionary<String, String> headerInfo = TraceServerCallContext(context);

            var functionRequestHeaderStream = context.RequestHeaders.SingleOrDefault(header => header.Key == "qlik-functionrequestheader-bin");

            if (functionRequestHeaderStream == null)
            {
                throw new Exception("ExecuteFunction called without Function Request Header in Request Headers.");
            }

            var functionRequestHeader = new FunctionRequestHeader();
            functionRequestHeader.MergeFrom(new CodedInputStream(functionRequestHeaderStream.ValueBytes));

            //Logger.Trace($"FunctionRequestHeader.FunctionId String : {(FunctionConstant)functionRequestHeader.FunctionId}");

            Guid g = LogRequest(logConnector, headerInfo["AppId"], headerInfo["UserId"]);

            switch (functionRequestHeader.FunctionId)
            {
                case (int)FunctionConstant.LogAsSeenStrCheck:
                    {
                        while (await requestStream.MoveNext())
                        {
                            var resultBundle = new BundledRows();
                            var cacheMetadata = new Metadata
                            {
                                { new Metadata.Entry("qlik-cache", "no-store") }
                            };

                            await context.WriteResponseHeadersAsync(cacheMetadata);

                            foreach (var row in requestStream.Current.Rows)
                            {
                                Row resultRow = LogRow(logConnector, g, headerInfo["AppId"], headerInfo["UserId"], row.Duals[0].StrData, "√");
                                resultBundle.Rows.Add(resultRow);
                            }
                            await responseStream.WriteAsync(resultBundle);
                        }

                        break;
                    }
                case (int)FunctionConstant.LogAsSeenStrEcho:
                    {
                        while (await requestStream.MoveNext())
                        {
                            var resultBundle = new BundledRows();
                            var cacheMetadata = new Metadata
                            {
                                { new Metadata.Entry("qlik-cache", "no-store") }
                            };

                            await context.WriteResponseHeadersAsync(cacheMetadata);

                            foreach (var row in requestStream.Current.Rows)
                            {
                                Row resultRow = LogRow(logConnector, g, headerInfo["AppId"], headerInfo["UserId"], row.Duals[0].StrData, row.Duals[0].StrData);
                                resultBundle.Rows.Add(resultRow);
                            }
                            await responseStream.WriteAsync(resultBundle);
                        }

                        break;
                    }
                case (int)FunctionConstant.LogAsSeenCheck:
                    {
                        while (await requestStream.MoveNext())
                        {
                            var resultBundle = new BundledRows();
                            var cacheMetadata = new Metadata
                            {
                                { new Metadata.Entry("qlik-cache", "no-store") }
                            };

                            await context.WriteResponseHeadersAsync(cacheMetadata);

                            foreach (var row in requestStream.Current.Rows)
                            {
                                Row resultRow = LogRow(logConnector, g, headerInfo["AppId"], headerInfo["UserId"], row.Duals[0].NumData, "√");
                                resultBundle.Rows.Add(resultRow);
                            }
                            await responseStream.WriteAsync(resultBundle);
                        }

                        break;
                    }
                case (int)FunctionConstant.LogAsSeenEcho:
                    {
                        while (await requestStream.MoveNext())
                        {
                            var resultBundle = new BundledRows();
                            var cacheMetadata = new Metadata
                            {
                                { new Metadata.Entry("qlik-cache", "no-store") }
                            };

                            await context.WriteResponseHeadersAsync(cacheMetadata);

                            foreach (var row in requestStream.Current.Rows)
                            {
                                Row resultRow = LogRow(logConnector, g, headerInfo["AppId"], headerInfo["UserId"], row.Duals[0].NumData, row.Duals[0].NumData);
                                resultBundle.Rows.Add(resultRow);
                            }
                            await responseStream.WriteAsync(resultBundle);
                        }

                        break;
                    }

                default:
                    break;
            }

            logConnector.EndLogData();

            Logger.Trace("-- (ExecuteFunction) --");
        }

        //private static long _callCounter = 0;

        private static Dictionary<String, String> TraceServerCallContext(ServerCallContext context)
        {
            Dictionary<String, String> headerInfo = new Dictionary<String, String>();

            var authContext = context.AuthContext;

            Logger.Trace($"ServerCallContext.Method : {context.Method}");
            Logger.Trace($"ServerCallContext.Host : {context.Host}");
            Logger.Trace($"ServerCallContext.Peer : {context.Peer}");

            headerInfo.Add("Method", context.Method);
            headerInfo.Add("Host", context.Host);
            headerInfo.Add("Peer", context.Peer);

            foreach (var contextRequestHeader in context.RequestHeaders)
            {
                Logger.Trace(
                    $"{contextRequestHeader.Key} : {(contextRequestHeader.IsBinary ? "<binary>" : contextRequestHeader.Value)}");

                if (contextRequestHeader.Key == "qlik-functionrequestheader-bin")
                {
                    var functionRequestHeader = new FunctionRequestHeader();
                    functionRequestHeader.MergeFrom(new CodedInputStream(contextRequestHeader.ValueBytes));

                    Logger.Trace($"FunctionRequestHeader.FunctionId : {functionRequestHeader.FunctionId}");
                    Logger.Trace($"FunctionRequestHeader.Version : {functionRequestHeader.Version}");

                    headerInfo.Add("FunctionId", functionRequestHeader.FunctionId.ToString());
                    headerInfo.Add("Version", functionRequestHeader.Version);
                }
                else if (contextRequestHeader.Key == "qlik-commonrequestheader-bin")
                {
                    var commonRequestHeader = new CommonRequestHeader();
                    commonRequestHeader.MergeFrom(new CodedInputStream(contextRequestHeader.ValueBytes));

                    Logger.Trace($"CommonRequestHeader.AppId : {commonRequestHeader.AppId}");
                    Logger.Trace($"CommonRequestHeader.Cardinality : {commonRequestHeader.Cardinality}");
                    Logger.Trace($"CommonRequestHeader.UserId : {commonRequestHeader.UserId}");

                    headerInfo.Add("AppId", commonRequestHeader.AppId);
                    headerInfo.Add("Cardinality", commonRequestHeader.Cardinality.ToString());
                    headerInfo.Add("UserId", commonRequestHeader.UserId);


                }
                else if (contextRequestHeader.Key == "qlik-scriptrequestheader-bin")
                {
                    var scriptRequestHeader = new ScriptRequestHeader();
                    scriptRequestHeader.MergeFrom(new CodedInputStream(contextRequestHeader.ValueBytes));

                    Logger.Trace($"ScriptRequestHeader.FunctionType : {scriptRequestHeader.FunctionType}");
                    Logger.Trace($"ScriptRequestHeader.ReturnType : {scriptRequestHeader.ReturnType}");

                    int paramIdx = 0;

                    foreach (var parameter in scriptRequestHeader.Params)
                    {
                        Logger.Trace($"ScriptRequestHeader.Params[{paramIdx}].Name : {parameter.Name}");
                        Logger.Trace($"ScriptRequestHeader.Params[{paramIdx}].DataType : {parameter.DataType}");
                        ++paramIdx;
                    }
                    Logger.Trace($"CommonRequestHeader.Script : {scriptRequestHeader.Script}");
                }
            }

            Logger.Trace($"ServerCallContext.AuthContext.IsPeerAuthenticated : {authContext.IsPeerAuthenticated}");
            Logger.Trace(
                $"ServerCallContext.AuthContext.PeerIdentityPropertyName : {authContext.PeerIdentityPropertyName}");
            foreach (var authContextProperty in authContext.Properties)
            {
                var loggedValue = authContextProperty.Value;
                var firstLineLength = loggedValue.IndexOf('\n');

                if (firstLineLength > 0)
                {
                    loggedValue = loggedValue.Substring(0, firstLineLength) + "<truncated at linefeed>";
                }

                Logger.Trace($"{authContextProperty.Name} : {loggedValue}");
            }
            return headerInfo;
        }
    }
}