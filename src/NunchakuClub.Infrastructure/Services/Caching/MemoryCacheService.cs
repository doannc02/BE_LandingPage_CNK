using Microsoft.Extensions.Caching.Memory;
using NunchakuClub.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NunchakuClub.Infrastructure.Services.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly List<string> _keys = new();
    private readonly object _lock = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration;
        else
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

        _cache.Set(key, value, options);

        lock (_lock)
        {
            if (!_keys.Contains(key))
                _keys.Add(key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        lock (_lock)
        {
            _keys.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        List<string> keysToRemove;
        lock (_lock)
        {
            keysToRemove = _keys.FindAll(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        lock (_lock)
        {
            _keys.RemoveAll(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        return Task.CompletedTask;
    }
}
