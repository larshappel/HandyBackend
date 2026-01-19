using System;
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
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        var responseDtos = products.Select(MapToResponseDto);
        return Ok(responseDtos);
    }

    // This one is the main endpoint we're using to transfer the data.
    // The method first validates the delivery record before updating
    // the database.
    [HttpPost("delivery")]
    public async Task<IActionResult> ProcessDelivery(DeliveryRecordDto deliveryRecord)
    {
        // Log string will be prefixed by a timestamp, suffixed by a status msg -> CSV format
        string logStringBase =
            $",{deliveryRecord.product_id}, {deliveryRecord.amount}, {deliveryRecord.individual_id}, {deliveryRecord.device_id}, ";

        // _logger.LogInformation(
        //     "Delivery received - Product ID: {ProductId}, Amount: {Amount}, Individual ID: {IndividualId}, Device ID: {DeviceID}",
        //     deliveryRecord.product_id,
        //     deliveryRecord.amount,
        //     deliveryRecord.individual_id,
        //     deliveryRecord.device_id
        // );

        // Cut the leading '9' (present in product id barcodes to distinguish from others)
        deliveryRecord.product_id = deliveryRecord.product_id.Substring(4);

        // Early return if the product ID format is wrong (i.e. not an integer).
        if (!int.TryParse(deliveryRecord.product_id, out int productId))
        {
            _logger.LogWarning(logStringBase + ", Invalid product ID");
            LogClientAccess(logStringBase + "Invalid product ID");
            return BadRequest(new { message = "Invalid Product ID format." });
        }

        // Early return if the product ID isn't in the DB.
        var product = await _productService.GetProductByOrderDetailIdAsync(productId);
        if (product == null)
        {
            _logger.LogWarning(logStringBase + ", Non-existent product ID");
            LogClientAccess(logStringBase + "Non-existent product ID");
            return NotFound(new { message = $"Product '{deliveryRecord.product_id}' not found" });
        }

        // Early return if amount is formatted incorrectly (needs to represent a double)
        if (!double.TryParse(deliveryRecord.amount, out double amountDouble))
        {
            _logger.LogWarning(logStringBase + ", Invalid amount format");
            LogClientAccess(logStringBase + "Invalid amount format");
            return BadRequest(new { message = "Invalid amount format." });
        }

        // Early return if scan count limit reached. (LabelIssueCount vs LabelScanCount)
        if (product.LabelCollectCount >= product.LabelIssueCount)
        {
            _logger.LogInformation(
                logStringBase
                    + ", All labels already scanned. (LabelIssueCount vs LabelCollectCount"
            );
            LogClientAccess(logStringBase + "Excess Label Scan");
            return Ok(new { message = "It's already scanned!" });
        }

        // The prerequisites and early returns are finished.
        // Next the data will be interpreted and written to the DB.

        if (deliveryRecord.amount.Contains('.') || deliveryRecord.amount.Contains(','))
        {
            _logger.LogInformation("Amount has dot. It was in kilo, use as is.");
            // do nothing
        }
        else
        {
            _logger.LogInformation("Amount has no dot. It was in gram. Converting to kilo.");
            amountDouble = amountDouble / 1000d;
        }

        // Update the product's amount (add the amount to the existing stock)
        product.Amount += amountDouble;

        // Count the times this label has been scanned.
        product.LabelCollectCount++;

        if (long.TryParse(deliveryRecord.individual_id, out long individualId))
        {
            product.IdentificationNumber = individualId;
        }
        else
        {
            _logger.LogInformation(logStringBase + ", no valid individual ID");
        }
        product.UpdateDate = DateTime.UtcNow.Date;
        product.UpdateTime = DateTime.UtcNow.TimeOfDay;

        // Save the changes
        var updatedProduct = await _productService.UpdateProductAsync(product.Id, product);

        LogClientAccess(logStringBase + "Amount updated: " + updatedProduct?.Amount);

        return Ok(
            new
            {
                message = "Delivery processed successfully",
                productOrderDetailId = product.OrderDetailId,
                newAmount = updatedProduct.Amount,
            }
        );
    }

    /**
     * Uses the logger to write to a client-accessible log location.
     */
    private void LogClientAccess(string msg)
    {
        // 'Using' keyword to dispose of the scope (ClientAccess logtype) automatically after the block
        using (_logger.BeginScope(new Dictionary<string, object> { ["LogType"] = "ClientAccess" }))
        {
            _logger.LogInformation(msg);
        }
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
        Console.WriteLine("Post endpoint hit");
        Console.WriteLine(createDto);
        var product = new Product
        {
            OrderDetailId = createDto.OrderDetailId,
            // Price = createDto.Price,
            Amount = (int)createDto.Amount,
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
        // if (updateDto.Price.HasValue)
        //     existingProduct.Price = updateDto.Price.Value;
        if (updateDto.Amount.HasValue)
            existingProduct.Amount = (int)updateDto.Amount.Value;

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
            // Price = updateDto.Price,
            Amount = (int)updateDto.Amount,
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

    // Helper method to map Product to ProductResponseDto
    private static ProductResponseDto MapToResponseDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            OrderDetailId = product.OrderDetailId,
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
