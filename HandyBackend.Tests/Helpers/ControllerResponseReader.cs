using System.Text.Json;

namespace HandyBackend.Tests.Helpers;

internal static class ControllerResponseReader
{
    public static T ReadAnonymous<T>(object result)
    {
        var json = JsonSerializer.Serialize(result);
        return JsonSerializer.Deserialize<T>(json) ??
            throw new InvalidOperationException("Unable to deserialize controller response.");
    }
}
