using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // This allows the request body stream to be read multiple times.
        context.Request.EnableBuffering();

        // Read the stream into a string
        var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync();
        
        // Rewind the stream so the next part of the pipeline can read it
        context.Request.Body.Position = 0;

        // Print the raw request body to the console
        Console.WriteLine("--- Raw Request Body ---");
        Console.WriteLine(requestBody);
        Console.WriteLine("------------------------");

        // Call the next middleware in the pipeline
        await _next(context);
    }
}
