using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Inventory.Dto.Products.Requests;

public sealed class UpdateProductRequest
{
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

    public bool IsTracked { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductStatus IsActive { get; set; } =
        ProductStatus.Active;
}