using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

namespace PulseBoard.Configuration;

public static class PulseBoardApiExtensions
{
    /// <summary>
    /// Apply consistent JSON serialization settings for the API.
    /// </summary>
    public static IServiceCollection AddPulseBoardApiDefaults(
        this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            JsonSerializerOptions jsonOptions = options.SerializerOptions;

            jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            jsonOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }
}
