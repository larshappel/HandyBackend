using HandyBackend.Controllers;
using HandyBackend.DTOs;
using HandyBackend.Models;
using HandyBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HandyBackend.Tests.Controllers;

public class ProductsControllerTests
{
    // TC1 reference: docs/test_specifications_EN.md –
    // Valid delivery with kilogram input updates amount, label count, and identification number.
    [Fact]
    public async Task ProcessDelivery_WithValidKilogramPayload_ReturnsUpdatedAmount()
    {
        // Arrange
        var existingProduct = new Product
        {
            Id = 42,
            OrderDetailId = 123456,
            Amount = 10.0,
            LabelIssueCount = 5,
            LabelCollectCount = 0,
            IdentificationNumber = null,
        };

        // Mock service returns the seeded product when the controller looks it up by order detail id.
        var productServiceMock = new Mock<IProductService>();
        productServiceMock
            .Setup(service => service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId))
            .ReturnsAsync(existingProduct);
        // Subsequent update returns the mutated product so the controller can echo it back.
        productServiceMock
            .Setup(service => service.UpdateProductAsync(existingProduct.Id, It.IsAny<Product>()))
            .ReturnsAsync((int _, Product updated) => updated);

        // Controller under test wired with mocked dependencies and the TC1 request payload.
        var controller = new ProductsController(
            productServiceMock.Object,
            NullLogger<ProductsController>.Instance
        );
        var request = new DeliveryRecordDto
        {
            product_id = "9000123456",
            amount = "1.25",
            individual_id = "1234567890",
            device_id = "1",
        };

        // Act: invoke the delivery endpoint.
        var result = await controller.ProcessDelivery(request);

        // Assert: verify HTTP 200 and inspect the anonymous response object.
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = okResult.Value;
        Assert.NotNull(responseValue);

        // Anonymous object comes back via reflection: pull out each field.
        var responseType = responseValue.GetType();
        var message = responseType.GetProperty("message")?.GetValue(responseValue)?.ToString();
        var orderDetailId = (int)(
            responseType.GetProperty("productOrderDetailId")?.GetValue(responseValue)
            ?? throw new InvalidOperationException("productOrderDetailId not present")
        );
        var newAmount = (double)(
            responseType.GetProperty("newAmount")?.GetValue(responseValue)
            ?? throw new InvalidOperationException("newAmount not present")
        );

        Assert.Equal("Delivery processed successfully", message);
        Assert.Equal(existingProduct.OrderDetailId, orderDetailId);
        Assert.Equal(11.25d, newAmount, precision: 3);

        // Ensure the controller called into the service as expected with the mutated product state.
        productServiceMock.Verify(
            service => service.GetProductByOrderDetailIdAsync(existingProduct.OrderDetailId),
            Times.Once
        );
        productServiceMock.Verify(
            service =>
                service.UpdateProductAsync(
                    existingProduct.Id,
                    It.Is<Product>(p =>
                        Math.Abs(p.Amount - 11.25d) < 0.0001
                        && p.LabelCollectCount == 1
                        && p.IdentificationNumber == 1234567890
                        && p.UpdateDate.HasValue
                        && p.UpdateTime.HasValue
                    )
                ),
            Times.Once
        );
    }
}
