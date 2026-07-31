namespace Inventory.Ui.Services
{
    public sealed class CustomerDisplayState
    {
        public CustomerDisplaySnapshot Snapshot { get; private set; } =
            new(
                Array.Empty<CustomerDisplayLine>(),
                0m,
                0m,
                0m,
                0m,
                null,
                CustomerDisplayMode.Idle);

        public event Action? Changed;

        public void Publish(CustomerDisplaySnapshot snapshot)
        {
            Snapshot = snapshot;
            Changed?.Invoke();
        }

        public void Reset()
        {
            Publish(
                new CustomerDisplaySnapshot(
                    Array.Empty<CustomerDisplayLine>(),
                    0m,
                    0m,
                    0m,
                    0m,
                    null,
                    CustomerDisplayMode.Idle));
        }
    }
}
