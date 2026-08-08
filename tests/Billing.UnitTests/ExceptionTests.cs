using Billing.Shared.Exceptions;
using FluentAssertions;
using Xunit;

namespace Billing.UnitTests;

/// <summary>
/// Unit tests for the custom exception hierarchy. Verifies that each
/// exception type carries the correct HTTP status code and message.
/// </summary>
public class ExceptionTests
{
    [Fact]
    public void NotFoundException_Should_Have_404_Status_Code()
    {
        var ex = new NotFoundException("Product", Guid.NewGuid());

        ex.StatusCode.Should().Be(404);
        ex.Message.Should().Contain("Product");
    }

    [Fact]
    public void ValidationException_Should_Have_400_Status_Code_And_Errors()
    {
        var errors = new List<string> { "Name is required.", "Price must be positive." };

        var ex = new ValidationException(errors);

        ex.StatusCode.Should().Be(400);
        ex.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void UnauthorizedException_Should_Have_401_Status_Code()
    {
        var ex = new UnauthorizedException();

        ex.StatusCode.Should().Be(401);
    }

    [Fact]
    public void UnauthorizedException_With_Message_Should_Preserve_Message()
    {
        const string message = "Invalid credentials.";

        var ex = new UnauthorizedException(message);

        ex.StatusCode.Should().Be(401);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void ForbiddenException_Should_Have_403_Status_Code()
    {
        var ex = new ForbiddenException();

        ex.StatusCode.Should().Be(403);
    }

    [Fact]
    public void ConflictException_Should_Have_409_Status_Code()
    {
        const string message = "Insufficient stock.";

        var ex = new ConflictException(message);

        ex.StatusCode.Should().Be(409);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void TenantContextMissingException_Should_Have_400_Status_Code()
    {
        var ex = new TenantContextMissingException();

        ex.StatusCode.Should().Be(400);
    }

    [Fact]
    public void All_Exceptions_Should_Be_AppException_Subtypes()
    {
        NotFoundException notFound = new("Entity", Guid.NewGuid());
        ValidationException validation = new(new List<string>());
        UnauthorizedException unauthorized = new();
        ForbiddenException forbidden = new();
        ConflictException conflict = new("msg");
        TenantContextMissingException tenantMissing = new();

        notFound.Should().BeAssignableTo<AppException>();
        validation.Should().BeAssignableTo<AppException>();
        unauthorized.Should().BeAssignableTo<AppException>();
        forbidden.Should().BeAssignableTo<AppException>();
        conflict.Should().BeAssignableTo<AppException>();
        tenantMissing.Should().BeAssignableTo<AppException>();
    }
}
