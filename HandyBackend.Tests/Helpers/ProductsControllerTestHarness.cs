using System;
using System.Collections.Generic;
using System.Linq;
using HandyBackend.Controllers;
using HandyBackend.DTOs;
using HandyBackend.Models;
using HandyBackend.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HandyBackend.Tests.Helpers;

internal sealed class ProductsControllerTestHarness
{
    public Mock<IProductService> ProductServiceMock { get; } = new();
    public ProductsController Controller { get; }
    public TestLogger<ProductsController> Logger { get; }

    public ProductsControllerTestHarness(TestLogger<ProductsController>? logger = null)
    {
        Logger = logger ?? new TestLogger<ProductsController>();
        Controller = new ProductsController(
            ProductServiceMock.Object,
            Logger
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

internal sealed class TestLogger<TCategory> : ILogger<TCategory>
{
    private readonly List<LogEntry> _entries = new();
    private readonly Stack<object> _scopeStack = new();
    private readonly object _sync = new();

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_sync)
            {
                return _entries.ToList();
            }
        }
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        if (state == null)
        {
            return NullScope.Instance;
        }

        lock (_sync)
        {
            _scopeStack.Push(state);
        }

        return new Scope(this);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        var message = formatter(state, exception);

        lock (_sync)
        {
            var scopesSnapshot = _scopeStack.Reverse().ToList();
            _entries.Add(
                new LogEntry(
                    logLevel,
                    eventId,
                    message,
                    (object?)state,
                    exception,
                    scopesSnapshot
                )
            );
        }
    }

    private void PopScope()
    {
        lock (_sync)
        {
            if (_scopeStack.Count > 0)
            {
                _scopeStack.Pop();
            }
        }
    }

    internal sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        object? State,
        Exception? Exception,
        IReadOnlyList<object> Scopes
    );

    private sealed class Scope : IDisposable
    {
        private readonly TestLogger<TCategory> _logger;
        private bool _disposed;

        public Scope(TestLogger<TCategory> logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _logger.PopScope();
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}
