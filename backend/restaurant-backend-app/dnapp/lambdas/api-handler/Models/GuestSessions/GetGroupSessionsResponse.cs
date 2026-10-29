using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public sealed record GetGroupSessionsResponse(
    [property: JsonPropertyName("participants")] IEnumerable<GroupParticipantResponse> Participants,
    [property: JsonPropertyName("total")] decimal Total);

public sealed record GroupParticipantResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<ParticipantType>))]
    ParticipantType Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("items")] IEnumerable<GroupSessionItemResponse> Items,
    [property: JsonPropertyName("total")] decimal Total,
    [property: JsonPropertyName("isSubmitted")] bool IsSubmitted);

public sealed record GroupSessionItemResponse(
    [property: JsonPropertyName("dish")] GroupSessionDishResponse Dish,
    [property: JsonPropertyName("quantity")] int Quantity);

public sealed record GroupSessionDishResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("price")] decimal Price);
