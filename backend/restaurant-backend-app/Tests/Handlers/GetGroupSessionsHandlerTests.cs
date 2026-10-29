using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Handlers.GuestSessions;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using Moq;
using System.Text.Json;

namespace APIHandler.Tests.Handlers;

public class GetGroupSessionsHandlerTests
{
    private readonly Mock<IGuestSessionService> _mockGuestSessionService;
    private readonly Mock<ILambdaContext> _mockLambdaContext;
    private readonly GetGroupSessionsHandler _handler;

    public GetGroupSessionsHandlerTests()
    {
        _mockGuestSessionService = new Mock<IGuestSessionService>();
        _mockLambdaContext = new Mock<ILambdaContext>();
        _mockLambdaContext.SetupGet(context => context.Logger).Returns(Mock.Of<ILambdaLogger>());
        _handler = new GetGroupSessionsHandler(_mockGuestSessionService.Object);
    }

    [Fact]
    public async Task HandleRequest_ShouldSerializeGroupSessionsContract_WhenAuthenticated()
    {
        var result = new GetGroupSessionsResponse(
            [
                new GroupParticipantResponse(
                    "guest-session",
                    ParticipantType.Guest,
                    "Guest",
                    [new GroupSessionItemResponse(new GroupSessionDishResponse("dish-1", "Soup", 4.25m), 2)],
                    8.5m,
                    true),
                new GroupParticipantResponse(
                    "owner-session",
                    ParticipantType.Owner,
                    "Owner",
                    [],
                    0m,
                    false)
            ],
            8.5m);
        _mockGuestSessionService
            .Setup(service => service.GetAllSessionsAsync("user-1", "group-1"))
            .ReturnsAsync(result);

        var response = await _handler.HandleRequest(CreateRequest("group-1", "user-1"), _mockLambdaContext.Object);

        Assert.Equal(200, response.StatusCode);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        Assert.Equal(new[] { "participants", "total" }, root.EnumerateObject().Select(property => property.Name));
        Assert.Equal(8.5m, root.GetProperty("total").GetDecimal());

        var participants = root.GetProperty("participants").EnumerateArray().ToArray();
        Assert.Equal(2, participants.Length);
        var participant = participants[0];
        Assert.Equal("guest-session", participant.GetProperty("id").GetString());
        Assert.Equal("Guest", participant.GetProperty("type").GetString());
        Assert.Equal("Guest", participant.GetProperty("name").GetString());
        Assert.True(participant.GetProperty("isSubmitted").GetBoolean());
        Assert.Equal(8.5m, participant.GetProperty("total").GetDecimal());
        Assert.Equal(new[] { "id", "type", "name", "items", "total", "isSubmitted" },
            participant.EnumerateObject().Select(property => property.Name));

        var item = Assert.Single(participant.GetProperty("items").EnumerateArray());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());
        Assert.Equal(new[] { "dish", "quantity" }, item.EnumerateObject().Select(property => property.Name));
        var dish = item.GetProperty("dish");
        Assert.Equal(new[] { "id", "name", "price" }, dish.EnumerateObject().Select(property => property.Name));
        Assert.Equal("dish-1", dish.GetProperty("id").GetString());
        Assert.Equal("Soup", dish.GetProperty("name").GetString());
        Assert.Equal(4.25m, dish.GetProperty("price").GetDecimal());
        var owner = participants[1];
        Assert.Equal("Owner", owner.GetProperty("type").GetString());
        Assert.False(owner.GetProperty("isSubmitted").GetBoolean());
        Assert.Empty(owner.GetProperty("items").EnumerateArray());
        Assert.Equal(0m, owner.GetProperty("total").GetDecimal());
        _mockGuestSessionService.Verify(service => service.GetAllSessionsAsync("user-1", "group-1"), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ShouldReturnUnauthorized_WhenClaimsAreMissing()
    {
        var request = new APIGatewayProxyRequest { PathParameters = new Dictionary<string, string> { ["groupReservationId"] = "group-1" } };

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(401, response.StatusCode);
        Assert.Equal("Unauthorized.", JsonDocument.Parse(response.Body).RootElement.GetProperty("error").GetString());
        _mockGuestSessionService.Verify(
            service => service.GetAllSessionsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task HandleRequest_ShouldReturnUnauthorized_WhenSubClaimIsMissingOrBlank(string? userId)
    {
        var response = await _handler.HandleRequest(CreateRequest("group-1", userId), _mockLambdaContext.Object);

        Assert.Equal(401, response.StatusCode);
        _mockGuestSessionService.Verify(
            service => service.GetAllSessionsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequest_ShouldReturnBadRequest_WhenGroupReservationIdIsMissing()
    {
        var request = CreateRequest(null, "user-1");

        var response = await _handler.HandleRequest(request, _mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
        _mockGuestSessionService.Verify(
            service => service.GetAllSessionsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequest_ShouldReturnNotFound_WhenGroupReservationDoesNotExist()
    {
        _mockGuestSessionService
            .Setup(service => service.GetAllSessionsAsync("user-1", "missing-group"))
            .ThrowsAsync(new GroupReservationNotFoundException());

        var response = await _handler.HandleRequest(CreateRequest("missing-group", "user-1"), _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
        Assert.Equal("Group reservation was not found", JsonDocument.Parse(response.Body).RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task HandleRequest_ShouldReturnNotFound_WhenParentReservationDoesNotExist()
    {
        _mockGuestSessionService
            .Setup(service => service.GetAllSessionsAsync("user-1", "group-1"))
            .ThrowsAsync(new ReservationNotFoundException("Parent reservation not found."));

        var response = await _handler.HandleRequest(CreateRequest("group-1", "user-1"), _mockLambdaContext.Object);

        Assert.Equal(404, response.StatusCode);
        Assert.Equal("Reservation not found.", JsonDocument.Parse(response.Body).RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task HandleRequest_ShouldReturnForbidden_WhenCallerIsNotReservationOwner()
    {
        _mockGuestSessionService
            .Setup(service => service.GetAllSessionsAsync("other-user", "group-1"))
            .ThrowsAsync(new ActionForbiddenException("Only the reservation owner can perform this action."));

        var response = await _handler.HandleRequest(CreateRequest("group-1", "other-user"), _mockLambdaContext.Object);

        Assert.Equal(403, response.StatusCode);
        Assert.Equal("Only the reservation owner can perform this action.",
            JsonDocument.Parse(response.Body).RootElement.GetProperty("error").GetString());
    }

    private static APIGatewayProxyRequest CreateRequest(string? groupReservationId, string? userId)
    {
        var request = new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = userId is null
                        ? new Dictionary<string, string>()
                        : new Dictionary<string, string> { ["sub"] = userId }
                }
            }
        };
        if (groupReservationId is not null)
        {
            request.PathParameters = new Dictionary<string, string> { ["groupReservationId"] = groupReservationId };
        }

        return request;
    }
}
