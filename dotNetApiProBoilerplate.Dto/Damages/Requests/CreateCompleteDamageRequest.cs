using System.ComponentModel.DataAnnotations;
using Inventory.Dto.Damages.Requests;

namespace Inventory.Dto.Damages.Requests
{
    public class CreateCompleteDamageRequest : CreateDamageRequest
    {
        // Wrapper to indicate this request is intended for the Complete/Automated flow.
        // Even if CreateDamageRequest has IsApproved, the service forces it to false in normal CreateAsync.
        // In CreateCompleteAsync, we will respect this being 'Complete' (which implies approved or processed).
    }
}
