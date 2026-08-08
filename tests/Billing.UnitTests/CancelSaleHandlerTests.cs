using System.Data;
using Billing.Application.Abstractions;
using Billing.Application.Commands.Sales;
using Billing.Domain.Entities;
using Billing.Persistence.Repositories;
using Billing.Persistence.UnitOfWork;
using Billing.Shared.Enums;
using Billing.Shared.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Billing.UnitTests;

/// <summary>
/// Unit tests for <see cref="CancelSaleHandler"/>. Verifies the cancellation
/// flow including authorization, not-found handling, and cache invalidation.
/// </summary>
public class CancelSaleHandlerTests
{
    private readonly Mock<ISalesRepository> _salesRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ICacheService> _cache = new();

    private readonly CancelSaleHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid SaleId = Guid.NewGuid();

    public CancelSaleHandlerTests()
    {
        _handler = new CancelSaleHandler(
            _salesRepo.Object, _unitOfWork.Object, _currentUser.Object, _cache.Object);
    }

    [Fact]
    public async Task Handle_Unauthenticated_User_Should_Throw_UnauthorizedException()
    {
        // Arrange
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);
        var request = new CancelSaleCommand(SaleId, "Wrong sale");

        // Act
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Sale_Not_Found_Should_Throw_NotFoundException()
    {
        // Arrange
        _currentUser.SetupGet(x => x.UserId).Returns(UserId);
        _salesRepo
            .Setup(r => r.CancelSaleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // no rows affected → not found

        var request = new CancelSaleCommand(SaleId, "Cancelled by mistake");

        // Act
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Successful_Cancel_Should_Return_Ok_And_Invalidate_Cache()
    {
        // Arrange
        _currentUser.SetupGet(x => x.UserId).Returns(UserId);
        _salesRepo
            .Setup(r => r.CancelSaleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // 1 row affected → success

        var request = new CancelSaleCommand(SaleId, "Customer returned");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveByPatternAsync("dashboard:*", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveByPatternAsync("products:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Repository_Exception_Should_Rollback_And_Rethrow()
    {
        // Arrange
        _currentUser.SetupGet(x => x.UserId).Returns(UserId);
        _salesRepo
            .Setup(r => r.CancelSaleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection lost"));

        var request = new CancelSaleCommand(SaleId, "Test");

        // Act
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
