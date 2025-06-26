using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using HandyBackend.Models;
using HandyBackend.Services;
using HandyBackend.Models.DTOs;

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
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            Amount = createDto.Amount
        };

        var createdProduct = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, MapToResponseDto(createdProduct));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchProduct(int id, ProductUpdateDto updateDto)
    {
        var existingProduct = await _productService.GetProductByIdAsync(id);
        if (existingProduct == null)
            return NotFound();

        // Update only provided properties
        if (!string.IsNullOrEmpty(updateDto.Name))
            existingProduct.Name = updateDto.Name;
        if (updateDto.Description != null)
            existingProduct.Description = updateDto.Description;
        if (updateDto.Price.HasValue)
            existingProduct.Price = updateDto.Price.Value;
        if (updateDto.Amount.HasValue)
            existingProduct.Amount = updateDto.Amount.Value;

        existingProduct.UpdatedAt = DateTime.UtcNow;

        var updatedProduct = await _productService.UpdateProductAsync(id, existingProduct);
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, ProductCreateDto updateDto)
    {
        var product = new Product
        {
            Id = id,
            Name = updateDto.Name,
            Description = updateDto.Description,
            Price = updateDto.Price,
            Amount = updateDto.Amount,
            UpdatedAt = DateTime.UtcNow
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

    // Helper method to map Product to ProductResponseDto
    private static ProductResponseDto MapToResponseDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Amount = product.Amount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
