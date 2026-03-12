using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Settings;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public int Port { get; set; } = 5672;
    public int RetryCount { get; set; } = 3;
    public int RetryIntervalSeconds { get; set; } = 5;
}