using Billing.Shared.Results;
using FluentAssertions;
using Xunit;

namespace Billing.UnitTests;

/// <summary>
/// Unit tests for the standard API response envelope <see cref="Result{T}"/> and
/// the non-generic <see cref="Result"/>. These are pure, dependency-free tests.
/// </summary>
public class ResultTests
{
    // ── Result<T> ──────────────────────────────────────────────────────────

    [Fact]
    public void Ok_Should_Set_Success_True_And_Data_And_Default_Message()
    {
        // Arrange
        var data = new { Name = "Widget" };

        // Act
        var result = Result<object>.Ok(data);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(data);
        result.Message.Should().NotBeNullOrEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Ok_With_Custom_Message_Should_Use_Provided_Message()
    {
        // Arrange
        const string customMessage = "Created successfully.";

        // Act
        var result = Result<string>.Ok("data", customMessage);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(customMessage);
    }

    [Fact]
    public void Fail_With_Message_Should_Set_Success_False_And_Message()
    {
        // Arrange
        const string message = "Something went wrong.";

        // Act
        var result = Result<string>.Fail(message);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(message);
        result.Data.Should().BeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Fail_With_Message_And_Errors_Should_Preserve_Errors()
    {
        // Arrange
        var errors = new List<string> { "Field is required.", "Invalid format." };

        // Act
        var result = Result<string>.Fail("Validation failed.", errors);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Validation failed.");
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().ContainInOrder("Field is required.", "Invalid format.");
    }

    [Fact]
    public void Fail_With_Errors_And_Default_Message_Should_Use_Default_Message()
    {
        // Arrange
        var errors = new List<string> { "Error 1" };

        // Act
        var result = Result<string>.Fail(errors);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Validation failed.");
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Fail_With_Null_Errors_Should_Default_To_Empty_Array()
    {
        // Act
        var result = Result<string>.Fail("message", null);

        // Assert
        result.Errors.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
    }

    // ── Non-generic Result ─────────────────────────────────────────────────

    [Fact]
    public void NonGeneric_Ok_Should_Set_Success_True_And_Default_Message()
    {
        // Act
        var result = Result.Ok();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NonGeneric_Ok_With_Custom_Message_Should_Use_Provided_Message()
    {
        // Arrange
        const string customMessage = "Deleted successfully.";

        // Act
        var result = Result.Ok(customMessage);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(customMessage);
    }

    [Fact]
    public void NonGeneric_Fail_Should_Set_Success_False_And_Message()
    {
        // Arrange
        const string message = "Operation failed.";

        // Act
        var result = Result.Fail(message);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(message);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NonGeneric_Fail_With_Errors_Should_Preserve_Errors()
    {
        // Arrange
        var errors = new List<string> { "Error A", "Error B" };

        // Act
        var result = Result.Fail("Invalid.", errors);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid.");
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void NonGeneric_Fail_With_Errors_And_Default_Message_Should_Use_Default()
    {
        // Arrange
        var errors = new List<string> { "Error A" };

        // Act
        var result = Result.Fail(errors);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Validation failed.");
        result.Errors.Should().BeEquivalentTo(errors);
    }
}
