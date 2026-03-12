using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Settings;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "micromart-catalog:";
    public int DefaultExpirySeconds { get; set; } = 300;
    public int ConnectRetry { get; set; } = 3;
    public int ConnectTimeoutMs { get; set; } = 5000;
    public int SyncTimeoutMs { get; set; } = 3000;
    public bool AbortOnConnectFail { get; set; } = false;
}
