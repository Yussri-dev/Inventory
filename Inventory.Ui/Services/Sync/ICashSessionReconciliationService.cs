using Inventory.Dto.CashSessions.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Services.Sync
{
    public interface ICashSessionReconciliationService
    {
        Task<CashSessionReconciliationResult> InspectAsync(
            CancellationToken cancellationToken = default);

        Task<CashSessionResult> CloseServerSessionAsync(
            Guid serverCashSessionId,
            decimal actualCash,
            string? closingNotes = null,
            CancellationToken cancellationToken = default);
    }
}
