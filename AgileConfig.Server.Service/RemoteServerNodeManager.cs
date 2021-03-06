﻿using AgileConfig.Server.Common;
using AgileConfig.Server.Data.Entity;
using AgileConfig.Server.IService;
using AgileHttp;
using AgileHttp.serialize;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AgileConfig.Server.Service
{
    public class RemoteServerNodeManager : IRemoteServerNodeManager
    {
        internal class SerializeProvider : ISerializeProvider
        {
            public T Deserialize<T>(string content)
            {
                return JsonConvert.DeserializeObject<T>(content, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            }

            public string Serialize(object obj)
            {
                return JsonConvert.SerializeObject(obj);
            }
        }

        private IServerNodeService _serverNodeService;
        private ILogger _logger;
        private ConcurrentDictionary<string, ClientInfos> _serverNodeClientReports;

        public IRemoteServerNodeActionProxy NodeProxy { get; }

        public RemoteServerNodeManager(IServerNodeService serverNodeService,ISysLogService sysLogService, ILoggerFactory loggerFactory)
        {
            _serverNodeService = serverNodeService;
            NodeProxy = new RemoteServerNodeProxy(sysLogService);
            _logger = loggerFactory.CreateLogger<RemoteServerNodeManager>();
            _serverNodeClientReports = new ConcurrentDictionary<string, IService.ClientInfos>();
        }

        public IService.ClientInfos GetClientsReport(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return null;
            }
            _serverNodeClientReports.TryGetValue(address, out IService.ClientInfos report);
            return report;
        }

        public Task TestEchoAsync()
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    var nodes = await _serverNodeService.GetAllNodesAsync();
                    nodes.ForEach(n =>
                    {
                        try
                        {
                            FunctionUtil.TRY(() =>
                            {
                                using (var resp = (n.Address + "/home/echo").AsHttp().Send())
                                {
                                    if (resp.StatusCode == System.Net.HttpStatusCode.OK && resp.GetResponseContent() == "ok")
                                    {
                                        n.LastEchoTime = DateTime.Now;
                                        n.Status = Data.Entity.NodeStatus.Online;
                                        var report = GetClientReport(n);
                                        if (report != null)
                                        {
                                            _serverNodeClientReports.AddOrUpdate(n.Address, report, (k, r) => report);
                                        }
                                    }
                                    else
                                    {
                                        n.Status = Data.Entity.NodeStatus.Offline;
                                    }
                                    _serverNodeService.UpdateAsync(n);
                                }
                            }, 5);
                        }
                        catch (Exception e)
                        {
                            _logger.LogInformation(e, "Try test node {0} echo , but fail .", n.Address);
                        }
                    });

                    await Task.Delay(5000 * 1);
                }
            });
        }

        private ClientInfos GetClientReport(ServerNode node)
        {
            return FunctionUtil.TRY(() =>
            {
                using (var resp = (node.Address + "/report/Clients").AsHttp().Config(new RequestOptions(new SerializeProvider())).Send())
                {
                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var content = resp.GetResponseContent();
                        _logger.LogTrace($"ServerNode: {node.Address} report clients infomation , {content}");

                        var report = resp.Deserialize<ClientInfos>();
                        if (report != null)
                        {
                            return report;
                        }
                    }

                    return null;
                }
            }, 5);
        }
    }
}
