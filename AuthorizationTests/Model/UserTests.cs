
using AuthorizationMicroservices;
using AuthorizationMicroservices.Models;
using FluentAssertions;

namespace AuthorizationTests.Model;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        const string username = "Artem";
        const string email = "artem@example.com";
        const string passwordHash = "hashed_password";

        // Act
        var result = User.Create(
            username,
            email,
            passwordHash);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Username.Should().Be(username);
        result.Value.Email.Should().Be(email);
        result.Value.PasswordHash.Should().Be(passwordHash);
        result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("","","")]
    [InlineData(" "," "," ")]
    [InlineData("       ","        ","       ")]
    [InlineData("", "artem@example.com", "hashed_password")]
    [InlineData("Artem", "", "hashed_password")]
    [InlineData("Artem", "invalid-email", "hashed_password")]
    [InlineData("Artem", "artem@example.com", "")]
    public void Create_WithInvalidData_ShouldReturnValidationError(
        string username,
        string email,
        string passwordHash)
    {
        // Act
        var result = User.Create(
            username,
            email,
            passwordHash);

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ValidationError>();
    }

    
}