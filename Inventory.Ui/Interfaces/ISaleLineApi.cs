using Inventory.Dto.SaleLines.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface ISaleLineApi
    {
        //[Get("/api/v1/salelines/by-sale/{saleId}")]
        //Task<List<SaleLineResult>> GetBySaleIdAsync(Guid saleId);

        [Get("/api/v1/saleLines/by-sale/{saleId}")]
        Task<List<SaleLineWithReturnsResult>> GetBySaleWithReturnsAsync(Guid saleId);


        [Get("/api/v1/salelines")]
        Task<List<SaleLineResult>> GetAllAsync();
    }


}
