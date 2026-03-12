using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Settings;

public sealed class BlobStorageSettings
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "product-images";
    public string CdnBaseUrl { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
    public string[] AllowedMimeTypes { get; set; } =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];
}
