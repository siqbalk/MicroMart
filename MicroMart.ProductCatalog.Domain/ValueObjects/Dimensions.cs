using MicroMart.ProductCatalog.Domain.Primitives;

namespace MicroMart.ProductCatalog.Domain.ValueObjects;

public sealed record Dimensions
{
    public decimal WeightKg { get; }
    public decimal LengthCm { get; }
    public decimal WidthCm { get; }
    public decimal HeightCm { get; }

    private Dimensions(
        decimal weightKg, decimal lengthCm,
        decimal widthCm, decimal heightCm)
    {
        WeightKg = weightKg;
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
    }

    public static Result<Dimensions> Create(
        decimal weightKg, decimal lengthCm,
        decimal widthCm, decimal heightCm)
    {
        if (weightKg < 0) return Result.Failure<Dimensions>(
            Error.Validation("Dimensions", "Weight cannot be negative"));
        if (lengthCm < 0 || widthCm < 0 || heightCm < 0)
            return Result.Failure<Dimensions>(
                Error.Validation("Dimensions", "Dimensions cannot be negative"));

        return Result.Success(new Dimensions(weightKg, lengthCm, widthCm, heightCm));
    }

    public static Dimensions Empty => new(0, 0, 0, 0);

    // Volumetric weight used by couriers for oversized light items
    public decimal VolumetricWeightKg
        => Math.Round((LengthCm * WidthCm * HeightCm) / 5000m, 3);

    // Billable weight is the greater of actual vs volumetric
    public decimal BillableWeightKg
        => Math.Max(WeightKg, VolumetricWeightKg);
}
