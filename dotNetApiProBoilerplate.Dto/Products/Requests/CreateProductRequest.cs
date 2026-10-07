using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Inventory.Dto.Products.Requests;

public sealed class CreateProductRequest
{
    public Guid? CatalogProductId { get; set; }

    [StringLength(
        200,
        ErrorMessage = "Name cannot exceed 200 characters.")]
    public string? Name { get; set; }

    [StringLength(
        100,
        ErrorMessage = "SKU cannot exceed 100 characters.")]
    public string? Sku { get; set; }

    [StringLength(
        100,
        ErrorMessage = "Barcode cannot exceed 100 characters.")]
    public string? Barcode { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [StringLength(
        100,
        ErrorMessage = "Category cannot exceed 100 characters.")]
    public string? Category { get; set; }

    [StringLength(
        100,
        ErrorMessage = "Brand cannot exceed 100 characters.")]
    public string? Brand { get; set; }

    [StringLength(
        50,
        ErrorMessage = "Unit cannot exceed 50 characters.")]
    public string? Unit { get; set; }

    [Range(
        0,
        double.MaxValue,
        ErrorMessage = "Sale price cannot be negative.")]
    public decimal SalePrice { get; set; }

    [Range(
        0,
        double.MaxValue,
        ErrorMessage = "Sale price 2 cannot be negative.")]
    public decimal SalePrice2 { get; set; }

    [Range(
        0,
        double.MaxValue,
        ErrorMessage = "Sale price 3 cannot be negative.")]
    public decimal SalePrice3 { get; set; }

    [Range(
        0,
        double.MaxValue,
        ErrorMessage = "Purchase price cannot be negative.")]
    public decimal PurchasePrice { get; set; }

    [Range(
        0,
        100,
        ErrorMessage = "VAT rate must be between 0 and 100.")]
    public decimal VatRate { get; set; }

    [Range(
        0,
        double.MaxValue,
        ErrorMessage = "Minimum stock cannot be negative.")]
    public decimal MinStockLevel { get; set; }

    [Range(
        0,
        double.MaxValue,
        ErrorMessage = "Maximum stock cannot be negative.")]
    public decimal MaxStockLevel { get; set; }

    public bool IsTracked { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductStatus IsActive { get; set; } =
        ProductStatus.Active;
}