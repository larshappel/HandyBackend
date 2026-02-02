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

Assert: Request is rejected gracefully. Desired behaviour is HTTP 400 with
message explaining the malformed barcode; current implementation throws
`ArgumentOutOfRangeException` due to `Substring(4)`, so this test will surface
the bug until guarded.

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

Assert: The controller currently dereferences
`updatedProduct` without null-check, leading to a `NullReferenceException`.
Document this as a defect and capture it once behaviour is defined (likely
should translate to 409 or 500).

## Open Questions / Further considerations

- Should delivery amounts containing commas be accepted? Under default culture
`double.TryParse` rejects them before the comma check, so clarify expected
localisation rules. -> Rejecting them is correct, this should trigger the same
error response as TC6 (Invalid amount format). Since the comma vs dot is more
ambiguous, adding a specific unit test to address this might be good.

- Confirm whether client-facing logs need verification in unit tests or can be
covered by integration/logging tests. -> Should be verified somehow that a log
file is written, as the logging is part of the requirement definition.
