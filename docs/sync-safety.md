# Synchronization changes — 2026-10-05

Deploy the API before the Windows client. The client now requires authenticated
`GET /api/sync-downloads/{customers,products,suppliers,stocks,damages,catalogs}`.
An older API returns an error; the client must never treat that error as an empty
snapshot or infer deletions from it. No database schema migration is introduced.

## Data safety

- Downloads return a complete snapshot per table. PostgreSQL RepeatableRead keeps
  the active rows, nested projections and explicit deleted IDs consistent within
  a response. This removes offset pagination drift, including duplicate names.
- Customer/product/supplier tombstones deactivate rows inside the local transaction.
  History is retained. Pending edits and dependent financial/stock operations are
  protected; deletion is deferred until their resolution.
- Pending sale/purchase/return movements protect their actual stock products,
  including pack components. Stock modified during the network request is also
  protected. A failed upload cannot cause its local stock effects to be erased.
- A partial upload still attempts independent downloads. Failures remain visible;
  completing downloads does not turn a conflicted upload into a successful run.
- Daily scheduling stores successful completion separately from attempts and uses
  a five-minute retry delay. The application must be open for this scheduler to run.
- Restocked return quantities reverse their historical unit cost in the period of
  the return. Non-restocked returns do not reverse cost.

Each snapshot is buffered in memory, as the previous full-refresh implementation
already buffered all pages. Large catalogs need load testing of response size,
client memory and HTTP timeout before deployment. Snapshots are consistent per
table; they are not one database-wide snapshot across all six HTTP requests.

## Metrics

`/metrics/requests` requires an Admin or SuperAdmin JWT. Route templates replace
URLs containing IDs, and at most 128 aggregate keys are retained. Counts distinguish
HTTP status families, failures and cumulative duration. Byte counts explicitly
cover known Content-Length only, not chunked transfers. This endpoint is technical
HTTP telemetry, not a count of successfully synchronized business operations.

Counters are process-scoped and include StartedAtUtc. For history across restarts
or multiple server instances, collect these snapshots in an external monitoring
store; the endpoint itself is not a durable metrics database.

The local synchronization screen uses database counts for total sales, stock
movements and cash movements. The displayed detail lists remain limited to 50.

## Store acceptance checks

Validate with a disposable copy before rollout: create an offline sale, force its
upload to fail, download stock and verify the local deduction survives; resolve
the failure and verify convergence. Delete a server customer/product and confirm
local deactivation with transaction history preserved. Repeat with a pending local
edit and confirm it remains protected. Test retry after a temporary API outage and
perform a full and partial return with and without restocking.
