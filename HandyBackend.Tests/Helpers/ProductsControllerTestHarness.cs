using HandyBackend.Controllers;
using HandyBackend.DTOs;
using HandyBackend.Models;
using HandyBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HandyBackend.Tests.Helpers;

internal sealed class ProductsControllerTestHarness
{
    public Mock<IProductService> ProductServiceMock { get; } = new();
    public ProductsController Controller { get; }

    public ProductsControllerTestHarness()
    {
        Controller = new ProductsController(
            ProductServiceMock.Object,
            NullLogger<ProductsController>.Instance
        );
    }

    public Product BuildDefaultProduct(Action<Product>? configure = null)
    {
        var product = new Product
        {
            Id = 42,
            OrderDetailId = 123456,
            Amount = 10.0,
            LabelIssueCount = 5,
            LabelCollectCount = 0,
            IdentificationNumber = null,
        };

        configure?.Invoke(product);
        return product;
    }

    public DeliveryRecordDto BuildDeliveryRequest(Action<DeliveryRecordDto>? configure = null)
    {
        var request = new DeliveryRecordDto
        {
            product_id = "9000123456",
            amount = "1.25",
            individual_id = "1234567890",
            device_id = "1",
        };

        configure?.Invoke(request);
        return request;
    }
}
