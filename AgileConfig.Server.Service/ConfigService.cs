﻿using AgileConfig.Server.Data.Entity;
using AgileConfig.Server.IService;
using AgileConfig.Server.Data.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgileConfig.Server.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AgileConfig.Server.Service
{
    public class ConfigService : IConfigService
    {
        private AgileConfigDbContext _dbContext;

        public ConfigService(ISqlContext context)
        {
            _dbContext = context as AgileConfigDbContext;
        }

        public async Task<bool> AddAsync(Config config)
        {
            await _dbContext.Configs.AddAsync(config);
            int x = await _dbContext.SaveChangesAsync();
            return x > 0;
        }

        public async Task<bool> DeleteAsync(Config config)
        {
            config = _dbContext.Configs.Find(config.Id);
            if (config != null)
            {
                _dbContext.Configs.Remove(config);
            }
            int x = await _dbContext.SaveChangesAsync();
            return x > 0;
        }

        public async Task<bool> DeleteAsync(string configId)
        {
            var config = _dbContext.Configs.Find(configId);
            if (config != null)
            {
                _dbContext.Configs.Remove(config);
            }
            int x = await _dbContext.SaveChangesAsync();
            return x > 0;
        }

        public Task<Config> GetAsync(string id)
        {
            return _dbContext.Configs.FindAsync(id);
        }

        public Task<List<Config>> GetAllConfigsAsync()
        {
            return _dbContext.Configs.ToListAsync();
        }

        public async Task<bool> UpdateAsync(Config config)
        {
            _dbContext.Update(config);
            var x = await _dbContext.SaveChangesAsync();

            return x > 0;
        }

        public Task<Config> GetByAppIdKey(string appId, string group, string key)
        {
            return _dbContext.Configs.FirstOrDefaultAsync(c =>
                c.AppId == appId &&
                c.Key == key &&
                c.Group == group &&
                c.Status == ConfigStatus.Enabled
            );
        }

        public Task<List<Config>> GetByAppId(string appId)
        {
            return _dbContext.Configs.Where(c =>
                c.AppId == appId &&
                c.Status == ConfigStatus.Enabled
            ).ToListAsync();
        }
    }
}
