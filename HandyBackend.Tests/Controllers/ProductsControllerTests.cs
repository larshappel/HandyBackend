using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HandyBackend.Controllers;
using HandyBackend.Models;
using HandyBackend.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HandyBackend.Tests.Controllers;

public class ProductsControllerTests
{
    private record DeliveryResponse(string message, int productOrderDetailId, double newAmount);

    private record ErrorResponse(string message);

    // TC1 reference: docs/test_specifications_EN.md –
    // Valid delivery with kilogram input updates amount, label count, and identification number.
    [Fact]
    public async Task ProcessDelivery_WithValidKilogramPayload_ReturnsUpdatedAmount()
    {
        // Arrange: harness builds controller + mocks following the shared spec defaults.
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest();

        // Mock service returns the seeded product when the controller looks it up by order detail id.
        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);

        var updatedProduct = harness.BuildDefaultProduct(p =>
        {
            p.Amount = 11.25d;
            p.LabelCollectCount = 1;
            p.IdentificationNumber = 1234567890;
            p.UpdateDate = DateTime.UtcNow.Date;
            p.UpdateTime = DateTime.UtcNow.TimeOfDay;
        });

        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    1234567890L
                )
            )
            .ReturnsAsync(updatedProduct);

        // Act: invoke the delivery endpoint.
        var result = await harness.Controller.ProcessDelivery(request);

        // Assert: verify HTTP 200 and inspect the typed projection of the anonymous response.
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var responseBody = ControllerResponseReader.ReadAnonymous<DeliveryResponse>(
            okResult.Value!
        );

        Assert.Equal("Delivery processed successfully", responseBody.message);
        Assert.Equal(existingProduct.OrderDetailId, responseBody.productOrderDetailId);
        Assert.Equal(11.25d, responseBody.newAmount, precision: 3);

        // Ensure the controller called into the service as expected with the mutated product state.
        harness.ProductServiceMock.Verify(
            service => service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId),
            Times.Once
        );
        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    1234567890L
                ),
            Times.Once
        );

        var entries = harness.Logger.Entries;
        var expectedLogMessage = ",9000123456, 1.25, 1234567890, 1, Amount updated: 11.25";
        var amountLogs = entries.Where(e => e.Message == expectedLogMessage).ToList();

        Assert.Equal(2, amountLogs.Count);
        Assert.All(amountLogs, e => Assert.Equal(LogLevel.Information, e.Level));
        Assert.Single(amountLogs.Where(HasClientAccessScope));
        Assert.Single(amountLogs.Where(e => !HasClientAccessScope(e)));
    }

    // TC2 reference: docs/test_specifications_EN.md –
    // Gram amount must convert to kilograms and update stored quantity.
    [Fact]
    public async Task ProcessDelivery_WithGramPayload_ConvertsAndUpdatesAmount()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest(dto => dto.amount = "750");

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);

        var updatedProduct = harness.BuildDefaultProduct(p =>
        {
            p.Amount = 10.75d;
            p.LabelCollectCount = 1;
            p.IdentificationNumber = 1234567890;
        });

        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 0.75d) < 0.0001),
                    1234567890L
                )
            )
            .ReturnsAsync(updatedProduct);

        var result = await harness.Controller.ProcessDelivery(request);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var responseBody = ControllerResponseReader.ReadAnonymous<DeliveryResponse>(
            okResult.Value!
        );

        Assert.Equal("Delivery processed successfully", responseBody.message);
        Assert.Equal(existingProduct.OrderDetailId, responseBody.productOrderDetailId);
        Assert.Equal(10.75d, responseBody.newAmount, precision: 3);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 0.75d) < 0.0001),
                    1234567890L
                ),
            Times.Once
        );
    }

    // TC3 reference: docs/test_specifications_EN.md –
    // Product IDs shorter than the expected prefix currently throw; test exposes the defect.
    [Fact]
    public async Task ProcessDelivery_WithGramPayload_TruncatesToTwoDecimalPlaces()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest(dto => dto.amount = "456");

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);

        var updatedProduct = harness.BuildDefaultProduct(p =>
        {
            p.Amount = 10.45d;
            p.LabelCollectCount = 1;
            p.IdentificationNumber = 1234567890;
        });

        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 0.45d) < 0.0001),
                    1234567890L
                )
            )
            .ReturnsAsync(updatedProduct);

        var result = await harness.Controller.ProcessDelivery(request);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var responseBody = ControllerResponseReader.ReadAnonymous<DeliveryResponse>(
            okResult.Value!
        );

        Assert.Equal("Delivery processed successfully", responseBody.message);
        Assert.Equal(existingProduct.OrderDetailId, responseBody.productOrderDetailId);
        Assert.Equal(10.45d, responseBody.newAmount, precision: 3);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 0.45d) < 0.0001),
                    1234567890L
                ),
            Times.Once
        );

        var expectedLogMessage = ",9000123456, 456, 1234567890, 1, Amount updated: 10.45";
        var matchingEntries = harness.Logger.Entries
            .Where(entry => entry.Message == expectedLogMessage)
            .ToList();

        Assert.Equal(2, matchingEntries.Count);
        Assert.Single(matchingEntries.Where(HasClientAccessScope));
        Assert.Single(matchingEntries.Where(entry => !HasClientAccessScope(entry)));
    }

    [Fact]
    public async Task ProcessDelivery_WithThreeDecimalKilogramPayload_TruncatesToTwoDecimalPlaces()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest(dto => dto.amount = "1.259");

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);

        var updatedProduct = harness.BuildDefaultProduct(p =>
        {
            p.Amount = 11.25d;
            p.LabelCollectCount = 1;
            p.IdentificationNumber = 1234567890;
        });

        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    1234567890L
                )
            )
            .ReturnsAsync(updatedProduct);

        var result = await harness.Controller.ProcessDelivery(request);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var responseBody = ControllerResponseReader.ReadAnonymous<DeliveryResponse>(
            okResult.Value!
        );

        Assert.Equal("Delivery processed successfully", responseBody.message);
        Assert.Equal(existingProduct.OrderDetailId, responseBody.productOrderDetailId);
        Assert.Equal(11.25d, responseBody.newAmount, precision: 3);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    1234567890L
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessDelivery_WithShortProductId_ReturnsBadRequest()
    {
        var harness = new ProductsControllerTestHarness();
        var request = harness.BuildDeliveryRequest(dto => dto.product_id = "123");

        var result = await harness.Controller.ProcessDelivery(request);

        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        var error = ControllerResponseReader.ReadAnonymous<ErrorResponse>(badRequest.Value!);

        Assert.Equal("Invalid Product ID format.", error.message);

        harness.ProductServiceMock.Verify(
            service => service.GetProductByOrderDetailIdAsync(It.IsAny<int>()),
            Times.Never
        );
        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<long?>()),
            Times.Never
        );

        var entries = harness.Logger.Entries;
        var invalidLogs = entries
            .Where(e => e.Message.EndsWith("Invalid product ID", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(2, invalidLogs.Count);
        Assert.All(invalidLogs, e => Assert.Equal(LogLevel.Information, e.Level));
        Assert.Single(invalidLogs.Where(HasClientAccessScope));
        Assert.Single(invalidLogs.Where(e => !HasClientAccessScope(e)));
    }

    // TC4 reference: docs/test_specifications_EN.md –
    // Non-numeric product IDs must be rejected with a 400 response.
    [Fact]
    public async Task ProcessDelivery_WithNonNumericProductId_ReturnsBadRequest()
    {
        var harness = new ProductsControllerTestHarness();
        var request = harness.BuildDeliveryRequest(dto => dto.product_id = "9999ABCD");

        var result = await harness.Controller.ProcessDelivery(request);

        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        var error = ControllerResponseReader.ReadAnonymous<ErrorResponse>(badRequest.Value!);

        Assert.Equal("Invalid Product ID format.", error.message);

        harness.ProductServiceMock.Verify(
            service => service.GetProductByOrderDetailIdAsync(It.IsAny<int>()),
            Times.Never
        );
        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<long?>()),
            Times.Never
        );
    }

    // TC13 reference: docs/test_specifications_EN.md –
    // Comma decimal amounts must be rejected with the invalid format response.
    [Fact]
    public async Task ProcessDelivery_WithCommaAmount_ReturnsBadRequest()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var harness = new ProductsControllerTestHarness();
            var request = harness.BuildDeliveryRequest(dto => dto.amount = "1,25");

            var result = await harness.Controller.ProcessDelivery(request);

            var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
            var error = ControllerResponseReader.ReadAnonymous<ErrorResponse>(badRequest.Value!);

            Assert.Equal("Invalid amount format.", error.message);

            harness.ProductServiceMock.Verify(
                service => service.GetProductByOrderDetailIdAsync(It.IsAny<int>()),
                Times.Never
            );
            harness.ProductServiceMock.Verify(
                service =>
                    service.ApplyDeliveryAsync(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<long?>()),
                Times.Never
            );

            var expectedLogMessage = ",9000123456, 1,25, 1234567890, 1, Invalid amount format";
            var matchingEntries = harness.Logger.Entries
                .Where(entry => entry.Message == expectedLogMessage)
                .ToList();

            Assert.Equal(2, matchingEntries.Count);
            Assert.All(matchingEntries, entry => Assert.Equal(LogLevel.Information, entry.Level));
            Assert.Single(matchingEntries.Where(HasClientAccessScope));
            Assert.Single(matchingEntries.Where(entry => !HasClientAccessScope(entry)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    // TC14 reference: docs/test_specifications_EN.md –
    // Negative amounts must be rejected before hitting the service.
    [Fact]
    public async Task ProcessDelivery_WithNegativeAmount_ReturnsBadRequest()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var harness = new ProductsControllerTestHarness();
            var request = harness.BuildDeliveryRequest(dto => dto.amount = "-1.25");

            var result = await harness.Controller.ProcessDelivery(request);

            var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
            var error = ControllerResponseReader.ReadAnonymous<ErrorResponse>(badRequest.Value!);

            Assert.Equal("Invalid amount format.", error.message);

            harness.ProductServiceMock.Verify(
                service => service.GetProductByOrderDetailIdAsync(It.IsAny<int>()),
                Times.Never
            );
            harness.ProductServiceMock.Verify(
                service =>
                    service.ApplyDeliveryAsync(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<long?>()),
                Times.Never
            );

            var expectedLogMessage = ",9000123456, -1.25, 1234567890, 1, Invalid amount format";
            var matchingEntries = harness.Logger.Entries
                .Where(entry => entry.Message == expectedLogMessage)
                .ToList();

            Assert.Equal(2, matchingEntries.Count);
            Assert.All(matchingEntries, entry => Assert.Equal(LogLevel.Information, entry.Level));
            Assert.Single(matchingEntries.Where(HasClientAccessScope));
            Assert.Single(matchingEntries.Where(entry => !HasClientAccessScope(entry)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    // TC5 reference: docs/test_specifications_EN.md –
    // Unknown product IDs should yield a 404 and skip updates.
    [Fact]
    public async Task ProcessDelivery_WithUnknownProduct_ReturnsNotFound()
    {
        var harness = new ProductsControllerTestHarness();
        var request = harness.BuildDeliveryRequest();
        var trimmedProductId = 123456;

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(trimmedProductId)
            )
            .ReturnsAsync((Product?)null);

        var result = await harness.Controller.ProcessDelivery(request);

        var notFound = Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundObjectResult>(result);
        var error = ControllerResponseReader.ReadAnonymous<ErrorResponse>(notFound.Value!);

        Assert.Equal("Product '123456' not found", error.message);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<long?>()),
            Times.Never
        );
    }

    // TC6 reference: docs/test_specifications_EN.md –
    // Invalid numeric formats must be rejected before hitting the service.
    [Fact]
    public async Task ProcessDelivery_WithInvalidAmount_ReturnsBadRequest()
    {
        var harness = new ProductsControllerTestHarness();
        var request = harness.BuildDeliveryRequest(dto => dto.amount = "not-a-number");

        var result = await harness.Controller.ProcessDelivery(request);

        var badRequest = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        var error = ControllerResponseReader.ReadAnonymous<ErrorResponse>(badRequest.Value!);

        Assert.Equal("Invalid amount format.", error.message);

        harness.ProductServiceMock.Verify(
            service => service.GetProductByOrderDetailIdAsync(It.IsAny<int>()),
            Times.Never
        );
        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<long?>()),
            Times.Never
        );
    }

    // TC7 reference: docs/test_specifications_EN.md –
    // Scan limit reached should short-circuit with an informational response.
    [Fact]
    public async Task ProcessDelivery_WithLabelLimitReached_ReturnsAlreadyScannedMessage()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct(p =>
        {
            p.LabelIssueCount = 5;
            p.LabelCollectCount = 5;
        });
        var request = harness.BuildDeliveryRequest();
        var trimmedProductId = 123456;

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(trimmedProductId)
            )
            .ReturnsAsync(existingProduct);

        var result = await harness.Controller.ProcessDelivery(request);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var messageOnly = ControllerResponseReader.ReadAnonymous<ErrorResponse>(okResult.Value!);

        Assert.Equal("It's already scanned!", messageOnly.message);
        Assert.Equal(10.0, existingProduct.Amount);
        Assert.Equal(5, existingProduct.LabelCollectCount);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<long?>()),
            Times.Never
        );
    }

    // TC8 reference: docs/test_specifications_EN.md –
    // Non-parsable individual IDs should leave IdentificationNumber untouched while updating other fields.
    [Fact]
    public async Task ProcessDelivery_WithInvalidIndividualId_DoesNotSetIdentificationNumber()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest(dto => dto.individual_id = "abc");

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);

        var updatedProduct = harness.BuildDefaultProduct(p =>
        {
            p.Amount = 11.25d;
            p.LabelCollectCount = 1;
            p.IdentificationNumber = null;
        });

        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    It.Is<long?>(id => id == null)
                )
            )
            .ReturnsAsync(updatedProduct);

        var result = await harness.Controller.ProcessDelivery(request);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var responseBody = ControllerResponseReader.ReadAnonymous<DeliveryResponse>(
            okResult.Value!
        );

        Assert.Equal("Delivery processed successfully", responseBody.message);
        Assert.Equal(existingProduct.OrderDetailId, responseBody.productOrderDetailId);
        Assert.Equal(11.25d, responseBody.newAmount, precision: 3);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    It.Is<long?>(id => id == null)
                ),
            Times.Once
        );
    }

    // TC11 reference: docs/test_specifications_EN.md –
    // Numeric-looking individual IDs beyond long.MaxValue should be treated as missing.
    [Fact]
    public async Task ProcessDelivery_WithOverflowIndividualId_ForwardsNullToService()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest(dto => dto.individual_id = new string('9', 25));

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);

        var updatedProduct = harness.BuildDefaultProduct(p =>
        {
            p.Amount = 11.25d;
            p.LabelCollectCount = 1;
            p.IdentificationNumber = null;
        });

        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    It.Is<long?>(id => id == null)
                )
            )
            .ReturnsAsync(updatedProduct);

        var result = await harness.Controller.ProcessDelivery(request);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var responseBody = ControllerResponseReader.ReadAnonymous<DeliveryResponse>(
            okResult.Value!
        );

        Assert.Equal("Delivery processed successfully", responseBody.message);
        Assert.Equal(existingProduct.OrderDetailId, responseBody.productOrderDetailId);
        Assert.Equal(11.25d, responseBody.newAmount, precision: 3);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    It.Is<long?>(id => id == null)
                ),
            Times.Once
        );
    }

    // TC9 reference: docs/test_specifications_EN.md –
    // Service returning null should be handled gracefully with an informational response.
    [Fact]
    public async Task ProcessDelivery_WhenUpdateReturnsNull_ReturnsNotExistsMessage()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest();

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);
        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.IsAny<double>(),
                    It.IsAny<long?>()
                )
            )
            .ReturnsAsync((Product?)null);

        var result = await harness.Controller.ProcessDelivery(request);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var messageOnly = ControllerResponseReader.ReadAnonymous<ErrorResponse>(okResult.Value!);

        Assert.Equal("The product no longer exists.", messageOnly.message);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.IsAny<double>(),
                    It.IsAny<long?>()
                ),
            Times.Once
        );
    }

    // TC10 reference: docs/test_specifications_EN.md –
    // Service-level label limit exception should surface the friendly "already scanned" response.
    [Fact]
    public async Task ProcessDelivery_WhenServiceThrowsInvalidOperation_ReturnsAlreadyScanned()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest();

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);
        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.IsAny<double>(),
                    It.IsAny<long?>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("Label scan limit reached."));

        var result = await harness.Controller.ProcessDelivery(request);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var messageOnly = ControllerResponseReader.ReadAnonymous<ErrorResponse>(okResult.Value!);

        Assert.Equal("It's already scanned!", messageOnly.message);

        harness.ProductServiceMock.Verify(
            service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.IsAny<double>(),
                    It.IsAny<long?>()
                ),
            Times.Once
        );
    }

    // TC12 reference: docs/test_specifications_EN.md –
    // Successful deliveries must emit both unscoped and client-access scoped logs.
    [Fact]
    public async Task ProcessDelivery_WithValidPayload_EmitsClientAccessLogs()
    {
        var harness = new ProductsControllerTestHarness();
        var existingProduct = harness.BuildDefaultProduct();
        var request = harness.BuildDeliveryRequest();

        harness
            .ProductServiceMock.Setup(service =>
                service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId)
            )
            .ReturnsAsync(existingProduct);

        var updatedProduct = harness.BuildDefaultProduct(p =>
        {
            p.Amount = 11.25d;
            p.LabelCollectCount = 1;
            p.IdentificationNumber = 1234567890;
        });

        harness
            .ProductServiceMock.Setup(service =>
                service.ApplyDeliveryAsync(
                    existingProduct.Id,
                    It.Is<double>(delta => Math.Abs(delta - 1.25d) < 0.0001),
                    1234567890L
                )
            )
            .ReturnsAsync(updatedProduct);

        var result = await harness.Controller.ProcessDelivery(request);

        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);

        var expectedMessage = ",9000123456, 1.25, 1234567890, 1, Amount updated: 11.25";
        var matchingEntries = harness.Logger.Entries
            .Where(entry => entry.Message == expectedMessage)
            .ToList();

        Assert.Equal(2, matchingEntries.Count);
        Assert.All(matchingEntries, entry => Assert.Equal(LogLevel.Information, entry.Level));

        Assert.Single(matchingEntries.Where(entry => HasClientAccessScope(entry)));
        Assert.Single(matchingEntries.Where(entry => !HasClientAccessScope(entry)));
    }

    private static bool HasClientAccessScope(TestLogger<ProductsController>.LogEntry entry)
    {
        foreach (var scope in entry.Scopes)
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> keyValuePairs)
            {
                if (
                    keyValuePairs.Any(pair =>
                        pair.Key == "LogType"
                        && string.Equals(
                            pair.Value?.ToString(),
                            "ClientAccess",
                            StringComparison.Ordinal
                        )
                    )
                )
                {
                    return true;
                }
            }
        }

        return false;
    }
}
