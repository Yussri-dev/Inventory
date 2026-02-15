using Inventory.Dto.GlobalRequests.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IPosApi
    {
        [Get("/api/v1/pos/bootstrap")]
        Task<PosBootstrapResult> Bootstrap();
    }
}
