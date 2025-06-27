using HandyBackend.Models;

namespace HandyBackend.Services;

// This is the interface for the ProductService.
// It's used to define the methods that the ProductService will implement.
// This is so we can use dependency injection to inject the ProductService into the controllers.
public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product?> GetProductByNameAsync(string name);
    Task<Product> CreateProductAsync(Product product);
    Task<Product?> UpdateProductAsync(int id, Product product);
    Task<bool> DeleteProductAsync(int id);
}
