using System.Text.Json.Serialization;

namespace CartBuddy.Shared.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ApiLogDirection>))]
public enum ApiLogDirection
{
    Request,
    Response,
    KrogerRequest,
    KrogerResponse,
}