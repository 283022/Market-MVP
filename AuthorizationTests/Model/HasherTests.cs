using AuthorizationMicroservices;
using FluentAssertions;

namespace AuthorizationTests.Model;

public class HasherTests
{
    private readonly Hasher _hasher = new();

    [Fact]
    public void Hash_ShouldReturnDifferentValueThanPassword()
    {
        // Arrange
        const string password = "123456";

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().NotBe(password);
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_ShouldGenerateDifferentHashesForSamePassword()
    {
        // Arrange
        const string password = "123456";

        // Act
        var firstHash = _hasher.Hash(password);
        var secondHash = _hasher.Hash(password);

        // Assert
        firstHash.Should().NotBe(secondHash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "123456";

        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(
            hash,
            password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "123456";

        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(
            hash,
            "wrong-password");

        // Assert
        result.Should().BeFalse();
    }
}
