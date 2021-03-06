﻿using System;
using System.IO;
using AgileConfig.Server.Common;
using AgileConfig.Server.IService;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog.Web;

namespace AgileConfig.Server.Apisite
{
    public class Program
    {
        public static IRemoteServerNodeManager RemoteServerNodeManager { get; private set; }

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());
#if DEBUG
            Configuration.Config = 
                 builder
                .AddJsonFile("appsettings.Development.json")
                .AddEnvironmentVariables()
                .Build();
#else
            Configuration.Config = builder.AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
#endif
            var host = CreateWebHostBuilder(args)
                .Build();

            var sp = host.Services;
            RemoteServerNodeManager = sp.CreateScope().ServiceProvider.GetService<IRemoteServerNodeManager>(); ;
            RemoteServerNodeManager.TestEchoAsync();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) 
        {
            return WebHost.CreateDefaultBuilder(args)
                  .UseKestrel(ks =>
                  {
                      ks.ListenAnyIP(5000);
                  })
                  .UseNLog()
                  .UseStartup<Startup>();
        }
          
    }
}
