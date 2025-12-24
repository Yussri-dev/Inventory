namespace Inventory.Services.Models.Core
{
    // Base service result model
    //
    // Purpose:
    // - Represents the outcome of a service-layer operation
    // - Aggregates messages instead of throwing exceptions for every scenario
    // - Enables rich feedback without breaking execution flow
    //
    // Design philosophy:
    // - Services return results, not HTTP responses
    // - Controllers decide how to translate results into HTTP responses
    // - Exceptions are reserved for truly exceptional cases
    public class ServiceResult
    {
        // Default constructor
        // Initializes the message collection
        public ServiceResult()
        {
            Messages = new List<ServiceMessage>();
        }

        // Collection of service messages
        //
        // Can contain:
        // - Informational messages
        // - Warnings
        // - Errors
        //
        // Multiple messages allow:
        // - Field-level validation feedback
        // - Combined warnings + success info
        public List<ServiceMessage> Messages { get; set; }

        // Indicates whether the operation succeeded
        //
        // Rule:
        // - Success = no Error-priority messages
        // - Failure = at least one Error-priority message
        //
        // This keeps success logic declarative and consistent
        public bool isSuccess => Messages.All(
            m => m.MessagePriority != MessagePriority.Error
        );
    }

    // Generic service result
    //
    // Purpose:
    // - Extends ServiceResult to carry a data payload
    // - Used for queries, commands, and operations returning values
    //
    // Examples:
    // - Create product -> ServiceResult<Product>
    // - Login -> ServiceResult<AuthResult>
    // - Search -> ServiceResult<IEnumerable<Product>>
    public class ServiceResult<T> : ServiceResult
    {
        // Parameterless constructor
        // Allows building the result incrementally
        public ServiceResult() { }

        // Data payload returned by the service
        //
        // Nullable because:
        // - Operation may fail
        // - Some operations return messages only
        public T? Data { get; set; }

        // Convenience constructor
        // Allows quick creation of successful results with data
        public ServiceResult(T? data = default)
        {
            Data = data;
        }
    }
}
