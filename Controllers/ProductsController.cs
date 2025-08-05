using HandyBackend.DTOs;
using HandyBackend.Models;
using HandyBackend.Models.DTOs;
using HandyBackend.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace HandyBackend.Controllers;

// A summary of the routes this creates and curl commands to test them:
// GET /api/products - Get all products
// GET /api/products/1 - Get product by ID
// POST /api/products - Create a new product
// PUT /api/products/1 - Update a product
// DELETE /api/products/1 - Delete a product


[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        var responseDtos = products.Select(MapToResponseDto);
        return Ok(responseDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponseDto>> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();

        return Ok(MapToResponseDto(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> CreateProduct(ProductCreateDto createDto)
    {
        var product = new Product
        {
            OrderDetailId = createDto.OrderDetailId,
            IdentificationNumber = createDto.IndividualId,
            // Price = createDto.Price,
            Amount = createDto.Amount,
        };

        var createdProduct = await _productService.CreateProductAsync(product);
        return CreatedAtAction(
            nameof(GetProduct),
            new { id = createdProduct.Id },
            MapToResponseDto(createdProduct)
        );
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchProduct(int id, ProductUpdateDto updateDto)
    {
        var existingProduct = await _productService.GetProductByIdAsync(id);
        if (existingProduct == null)
            return NotFound();

        // Update only provided properties
        if (updateDto.OrderDetailId.HasValue)
            existingProduct.OrderDetailId = updateDto.OrderDetailId.Value;
        if (updateDto.IndividualId != null)
            existingProduct.IdentificationNumber = updateDto.IndividualId;
        // if (updateDto.Price.HasValue)
        //     existingProduct.Price = updateDto.Price.Value;
        if (updateDto.Amount.HasValue)
            existingProduct.Amount = updateDto.Amount.Value;

        existingProduct.UpdateDate = DateTime.UtcNow.Date;
        existingProduct.UpdateTime = DateTime.UtcNow.TimeOfDay;

        var updatedProduct = await _productService.UpdateProductAsync(id, existingProduct);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, ProductCreateDto updateDto)
    {
        var product = new Product
        {
            Id = id,
            OrderDetailId = updateDto.OrderDetailId,
            IdentificationNumber = updateDto.IndividualId,
            // Price = updateDto.Price,
            Amount = updateDto.Amount,
            UpdateDate = DateTime.UtcNow.Date,
            UpdateTime = DateTime.UtcNow.TimeOfDay,
        };

        var updatedProduct = await _productService.UpdateProductAsync(id, product);
        if (updatedProduct == null)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _productService.DeleteProductAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("delivery")]
    public async Task<IActionResult> ProcessDelivery(DeliveryRecordDto deliveryRecord)
    {
        Console.WriteLine(
            $"Delivery received - Product ID: {deliveryRecord.Product_Id}, Amount: {deliveryRecord.Amount}, Individual ID: {deliveryRecord.individual_id}"
        );

        // Find the product by OrderDetailId (using productId as the OrderDetailId)
        var product = await _productService.GetProductByOrderDetailIdAsync(
            deliveryRecord.Product_Id
        );
        if (product == null)
        {
            Console.WriteLine($"Product not found: {deliveryRecord.Product_Id}");
            return NotFound(new { message = $"Product '{deliveryRecord.Product_Id}' not found" });
        }

        // Update the product's amount (set the amount of existing stock)
        product.Amount = deliveryRecord.Amount;
        product.IdentificationNumber = deliveryRecord.individual_id;
        product.UpdateDate = DateTime.UtcNow.Date;
        product.UpdateTime = DateTime.UtcNow.TimeOfDay;

        // Save the changes
        var updatedProduct = await _productService.UpdateProductAsync(product.Id, product);

        Console.WriteLine(
            $"Product '{product.OrderDetailId}' stock updated. New amount: {updatedProduct.Amount}"
        );

        return Ok(
            new
            {
                message = "Delivery processed successfully",
                productOrderDetailId = product.OrderDetailId,
                newAmount = updatedProduct.Amount,
            }
        );
    }

    // Helper method to map Product to ProductResponseDto
    private static ProductResponseDto MapToResponseDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            OrderDetailId = product.OrderDetailId,
            IndividualId = product.IdentificationNumber,
            // Price = product.Price,
            Amount = product.Amount,
            CreatedAt = product.CreatedAt,
            UpdatedAt =
                product.UpdateDate.HasValue && product.UpdateTime.HasValue
                    ? product.UpdateDate.Value.Add(product.UpdateTime.Value)
                    : (DateTime?)null,
        };
    }
}
