using BookNest.Core.Enums;
using BookNest.Core.Interfaces.IServices;
using BookNest.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BookNest.UnitTests;

[Trait("Category", "Mvc")]
public class OrderStateMachineTests
{
    private static IOrderService CreateService()
        => new OrderService(null!, NullLogger<OrderService>.Instance);

    [Fact]
    public void Pending_CanMoveTo_ProcessingOrCancelled()
    {
        CreateService().AllowedNextStatuses(OrderStatus.Pending)
            .Should().BeEquivalentTo(new[] { OrderStatus.Processing, OrderStatus.Cancelled });
    }

    [Fact]
    public void Processing_CanMoveTo_ShippedOrCancelled()
    {
        CreateService().AllowedNextStatuses(OrderStatus.Processing)
            .Should().BeEquivalentTo(new[] { OrderStatus.Shipped, OrderStatus.Cancelled });
    }

    [Fact]
    public void Shipped_CanOnlyMoveTo_Delivered()
    {
        // BR-01: cancellation is only allowed before Shipped.
        CreateService().AllowedNextStatuses(OrderStatus.Shipped)
            .Should().BeEquivalentTo(new[] { OrderStatus.Delivered });
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void FinalStates_AreTerminal(OrderStatus status)
    {
        CreateService().AllowedNextStatuses(status).Should().BeEmpty();
    }

    [Fact]
    public void NoTransition_EverGoesBackwards()
    {
        var service = CreateService();

        foreach (var from in Enum.GetValues<OrderStatus>())
        {
            foreach (var to in service.AllowedNextStatuses(from))
            {
                ((int)to).Should().BeGreaterThan((int)from,
                    $"{from} must not move back to {to}");
            }
        }
    }
}
