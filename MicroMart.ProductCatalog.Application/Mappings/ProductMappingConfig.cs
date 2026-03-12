using Mapster;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Mappings;

public class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // ── Product → ProductResponse ─────────────────────────────────────
        // Mapster auto-maps properties with matching names.
        // We only need to configure the non-obvious mappings.
            config.NewConfig<Product, ProductResponse>()

            // Map strongly-typed value objects to plain strings/decimals
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Sku, src => src.Sku.Value)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)

            // Flatten Money value object
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.SalePrice, src => src.SalePrice == null
                ? (decimal?)null
                : src.SalePrice.Amount)
            .Map(dest => dest.EffectivePrice, src => src.EffectivePrice.Amount)
            .Map(dest => dest.Currency, src => src.Price.Currency)

            // Flatten Dimensions value object
            .Map(dest => dest.WeightKg, src => src.Dimensions.WeightKg)
            .Map(dest => dest.LengthCm, src => src.Dimensions.LengthCm)
            .Map(dest => dest.WidthCm, src => src.Dimensions.WidthCm)
            .Map(dest => dest.HeightCm, src => src.Dimensions.HeightCm)

            // Tags: List<Tag> → List<string>
            .Map(dest => dest.Tags, src => src.Tags.Select(t => t.Value).ToList())

            // Map nested collections
            .Map(dest => dest.Images, src => src.Images)
            .Map(dest => dest.Variants, src => src.Variants);

        // ── Product → ProductSummaryResponse ─────────────────────────────
           config.NewConfig<Product, ProductSummaryResponse>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.SalePrice, src => src.SalePrice == null
                ? (decimal?)null
                : src.SalePrice.Amount)
            .Map(dest => dest.EffectivePrice, src => src.EffectivePrice.Amount)
            .Map(dest => dest.Currency, src => src.Price.Currency)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)

            // Only the primary image URL — not the whole Images list
            .Map(dest => dest.PrimaryImage,
                src => src.Images
                    .FirstOrDefault(i => i.IsPrimary)!.Url);

        // ── ProductImage → ProductImageResponse ──────────────────────────
        config.NewConfig<ProductImage, ProductImageResponse>()
            .Map(dest => dest.Url, src => src.Url)
            .Map(dest => dest.AltText, src => src.AltText)
            .Map(dest => dest.IsPrimary, src => src.IsPrimary);

        // ── ProductVariant → ProductVariantResponse ───────────────────────
        config.NewConfig<ProductVariant, ProductVariantResponse>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.Price, src => src.Price.Amount);

        // ── Category → CategoryResponse ───────────────────────────────────
        config.NewConfig<Category, CategoryResponse>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.ParentId, src => src.ParentId == null
                ? null
                : src.ParentId.Value);
    }
}
