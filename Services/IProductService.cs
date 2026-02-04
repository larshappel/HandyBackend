using HandyBackend.Models;

namespace HandyBackend.Services;

// This is the interface for the ProductService.
// It's used to define the methods that the ProductService will implement.
// This is so we can use dependency injection to inject the ProductService into the controllers.
public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product?> GetProductByOrderDetailIdAsync(int orderDetailId);
    Task<Product> CreateProductAsync(Product product);
    Task<Product?> UpdateProductAsync(int id, Product product);

    /// <summary>
    /// Applies a delivery delta to a product while enforcing label limits
    /// and protecting the update with a pessimistic row lock.
    /// </summary>
    /// <param name="productId">Primary key of the product to update.</param>
    /// <param name="amountDelta">Delta (positive or negative) to add to the product amount.</param>
    /// <param name="identificationNumber">Optional individual identifier captured from the request.</param>
    /// <returns>The updated product, or null if it no longer exists.</returns>
    Task<Product?> ApplyDeliveryAsync(int productId, double amountDelta, long? identificationNumber);
    Task<bool> DeleteProductAsync(int id);
}
