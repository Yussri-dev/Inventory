using Inventory.Dto.CashSessions.Results;
using Inventory.LocalDB.Models;

namespace Inventory.Ui.Services.Sync
{
    public sealed class CashSessionReconciliationResult
    {
        public CashSessionReconciliationState State { get; init; }

        public LocalCashSession? LocalSession { get; init; }

        public CashSessionResult? ServerSession { get; init; }

        public string Message { get; init; } =
            string.Empty;

        public bool HasConflict =>
            State ==
            CashSessionReconciliationState.Conflict;

        public bool IsOnline =>
            State !=
          
            CashSessionReconciliationState.Offline;
    }

    public enum CashSessionReconciliationState
    {
        Offline,
        Ready,
        MatchingSessionLinked,
        ServerSessionOnly,
        Conflict
    }
}
