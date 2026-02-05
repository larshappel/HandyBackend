# Delivery Endpoint Test Specifications

## Scope

Focuses on the behavioural contract of `ProductsController.ProcessDelivery` to
ensure valid deliveries are recorded and invalid ones are rejected with the
documented responses.

## Shared Test Setup

- Controller unit tests will run against `ProductsController` with
`IProductService` mocked and `ILogger<ProductsController>` captured via a test
logger.
- Unless a case states otherwise, seed the mocked service with a `Product`
whose `Id = 42`, `OrderDetailId = 123456`, `Amount = 10.0`, `LabelIssueCount =
5`, `LabelCollectCount = 2`, and `IdentificationNumber = null`.
- Successful paths should assert that `UpdateProductAsync` receives a `Product`
reflecting the mutation (amount change, label count increment, timestamp
updates).

## Test Cases

### TC1 Valid Delivery – Kilogram Amount

Arrange: Service returns the seeded product when queried by order detail id and
echoes the mutated product from `UpdateProductAsync`.
Database row for product_id `9000123456` exists and has an amount of 10kg set.
LabelCollectCount for the product is at 0.
LabelIssueCount for the product is at 5.

Act: POST with `{
product_id: "9000123456",
amount: "1.25",
individual_id: "1234567890",
device_id: "1"
}`.

Assert: HTTP 200 with body `message == "Delivery processed
successfully"`, `productOrderDetailId == 123456`, `newAmount == 11.25`; verify
`LabelCollectCount` incremented to 1, `IdentificationNumber` set to `1234567890`,
and timestamps updated to the fixed test clock if introduced.

### TC2 Valid Delivery – Gram Conversion

Arrange: Same as TC1.

Act: POST with `{
product_id: "9000123456",
amount: "750",
individual_id: "1234567890",
device_id: "1"
}`.

Assert: HTTP 200 with `newAmount == 10.75`; confirm amount delta of `0.75`
(gram-to-kilogram conversion) and other mutations as in TC1 (LabelCollectCount incremented)

### TC3 Product Id Shorter Than Prefix

Arrange: Service is not called because request preprocessing fails.

Act: POST with `{ product_id: "123", amount: "1.0", individual_id: "1",
device_id: "1" }`.

Assert: HTTP 400 with payload `{ message: "Invalid Product ID format." }`; the
service should remain untouched.

### TC4 Non-numeric Product Id After Prefix Removal

Arrange: Service is not called.

Act: POST with `{
product_id: "9999ABCD",
amount: "1.0",
individual_id: "1",
device_id: "dev"
}`.

Assert: HTTP 400 with
payload `{ message: "Invalid Product ID format." }` and no update invocation.

### TC5 Unknown Product

Arrange: `GetProductByOrderDetailIdAsync` returns `null`, i.e. the product_id
`9000123456` isn't in the database.

Act: POST with `{
product_id: "9000123456",
amount: "1.0",
individual_id: "1",
device_id: "1"
}`.

Assert: HTTP 404 with payload `{ message: "Product '9000123456' not found" }`;
confirm `UpdateProductAsync` is never called.

### TC6 Invalid Amount Format

Arrange: Service is not called.

Act:
POST with `{
product_id: "9000123456",
amount: "not-a-number",
individual_id: "1",
device_id: "1" }`.

Assert: HTTP 400 with payload `{ message: "Invalid amount format." }`; ensure
no mutation or service update is attempted.

### TC7 Label Scan Limit Reached

Arrange: Delivered product exist in the DB, but has `LabelCollectCount == LabelIssueCount`.

Act: POST with `{
product_id: "9000123456",
amount: "1.0",
individual_id: "1",
device_id: "1" }`.

Assert: HTTP 200 with payload `{ message: "It's already scanned!" }`;
verify `UpdateProductAsync` is not called and original counts remain unchanged.

### TC8 Individual Id Not Parsable

Arrange: Service returns the seeded product and update completes.

Act: POST with `{
product_id: "9000123456",
amount: "1.0",
individual_id: "abc",
device_id: "1" }`.

Assert: HTTP 200 with success payload; mutated product
keeps `IdentificationNumber == null` (or previous value) while amount and label
count update as usual.

### TC9 Service Update Returns Null

Arrange: `GetProductByOrderDetailIdAsync` returns the seeded product but
`UpdateProductAsync` returns `null` to mimic a race where the row was deleted.

Act: POST with valid payload.

Assert: HTTP 200 with payload `{ message: "The product no longer exists." }`.
No further updates should be attempted.

### TC10 Service Throws Label Limit

Arrange: `GetProductByOrderDetailIdAsync` returns the seeded product while
`ApplyDeliveryAsync` throws `InvalidOperationException` to simulate the scan
limit being enforced inside the service.

Act: POST a valid delivery payload.

Assert: HTTP 200 with payload `{ message: "It's already scanned!" }`; verify
`ApplyDeliveryAsync` was invoked exactly once and no retry or alternative
update path is taken.

### TC11 Individual Id Overflow

Arrange: The service returns the seeded product and `ApplyDeliveryAsync`
completes successfully.

Act: POST a valid delivery payload except `individual_id` is a numeric string
longer than `long.MaxValue` (for example, twenty-five nines).

Assert: HTTP 200 success payload; confirm the controller forwards `null` for
`identificationNumber` despite the numeric-looking input and the service
receives the expected amount delta.

### TC12 Client Access Logging

Arrange: Seed the service to return the standard product and capture logs
through a test logger that records scope information.

Act: POST a valid delivery payload that results in a successful update.

Assert: Two informative log entries with identical messages exist—one emitted
without a scope and another emitted within a scope containing `LogType ==
"ClientAccess"`; ensure no additional entries with that message are produced.

### TC13 Comma Amount Rejected

Arrange: Use the shared harness defaults and let the test logger capture
messages.

Act: POST the TC1 payload but supply `amount: "1,25"` (comma decimal) after
forcing `CultureInfo.InvariantCulture` for the controller invocation.

Assert: HTTP 400 with `{ message: "Invalid amount format." }`; confirm neither
`GetProductByOrderDetailIdAsync` nor `ApplyDeliveryAsync` run and that the
client-access log records "Invalid amount format".

### TC14 Negative Amount Rejected

Arrange: Use the shared harness defaults (culture set to invariant for safety).

Act: POST the TC1 payload with `amount: "-1.25"`.

Assert: HTTP 400 with `{ message: "Invalid amount format." }`; verify neither
`GetProductByOrderDetailIdAsync` nor `ApplyDeliveryAsync` run and that the
client-access log records "Invalid amount format".

## Open Questions / Further considerations

- Should delivery amounts containing commas ever be accepted? TC13 currently
codifies rejection; revisit if localisation requirements change.
- Confirm whether client-facing logs need verification in integration tests or
if unit coverage (TC12/TC13) is sufficient to meet the logging requirement.
