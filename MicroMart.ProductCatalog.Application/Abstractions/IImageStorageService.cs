using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface IImageStorageService
{
    /// Uploads image bytes and returns the public CDN URL.
    Task<string> UploadAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    /// Deletes an image by its CDN URL or blob name.
    Task DeleteAsync(
        string imageUrl,
        CancellationToken ct = default);
}