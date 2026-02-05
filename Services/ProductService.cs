using System.Data;
using HandyBackend.Data;
using HandyBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace HandyBackend.Services;

// ProductService provides methods to interact with the database through the ApplicationDbContext.
// It implements the IProductService interface, so we can use dependency injection to inject the ProductService into the controllers.
// It allows us to manipulate the Products table indirectly via classes.
// Explicitly does all the stuff that Laravel does under the hood.
public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product?> GetProductByOrderDetailIdAsync(int orderDetailId)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.OrderDetailId == orderDetailId);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateProductAsync(int id, Product product)
    {
        var existingProduct = await _context.Products.FindAsync(id);
        if (existingProduct == null)
            return null;

        existingProduct.OrderDetailId = product.OrderDetailId;
        // existingProduct.Price = product.Price;
        existingProduct.Amount = product.Amount;
        existingProduct.IdentificationNumber = product.IdentificationNumber;
        existingProduct.UpdateDate = DateTime.UtcNow.Date;
        existingProduct.UpdateTime = DateTime.UtcNow.TimeOfDay;

        await _context.SaveChangesAsync();
        return existingProduct;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Applies the delivery delta to the specified product inside a transaction to prevent races.
    /// </summary>
    /// <param name="productId">Primary key of the product to update.</param>
    /// <param name="amountDelta">Delta (positive or negative) to add to the product amount.</param>
    /// <param name="identificationNumber">Optional individual identifier captured from the delivery record.</param>
    /// <returns>The updated product, or null if it no longer exists.</returns>
    public async Task<Product?> ApplyDeliveryAsync(
        int productId,
        double amountDelta,
        long? identificationNumber
    )
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE orderdetails
            SET SalesQuantity = SalesQuantity + {amountDelta},
                LabelCollectCount = LabelCollectCount + 1,
                IdentificationNumber = {identificationNumber},
                UpdateDate = CURRENT_DATE(),
                UpdateTime = CURRENT_TIME()
            WHERE OrderDetailID = {productId}
              AND LabelCollectCount < LabelIssueCount;
        ");

        if (rowsAffected == 0)
        {
            // Determine whether the row is missing or the label limit was reached.
            var current = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            await transaction.RollbackAsync();

            if (current == null)
            {
                return null;
            }

            if (current.LabelCollectCount >= current.LabelIssueCount)
            {
                throw new InvalidOperationException("Label scan limit reached.");
            }

            throw new InvalidOperationException("Delivery update failed unexpectedly.");
        }

        await transaction.CommitAsync();

        var updatedProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        return updatedProduct;
    }
}
