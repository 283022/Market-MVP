using AuthorizationMicroservices;
using FluentAssertions;

namespace AuthorizationTests;

public class UserServiceTests
{
    private readonly Hasher _hasher;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _hasher = new Hasher();
        _userService = new UserService(_hasher);
    }

    [Fact]
    public void AddUser_WithValidData_ShouldReturnUserId()
    {
        // Act
        var result = _userService.AddUser(
            "Artem",
            "artem@example.com",
            "123456");

        // Assert
        result.IsSuccess.Should().BeTrue();
        //TODO: удалить когда будет реализация через бд
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AddUser_WithEmptyUsername_ShouldFail()
    {
        // Act
        var result = _userService.AddUser(
            "",
            "artem@example.com",
            "123456");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .Contain(e => e is ValidationError);
    }

    [Fact]
    public void AddUser_WithEmptyEmail_ShouldFail()
    {
        // Act
        var result = _userService.AddUser(
            "Artem",
            "",
            "123456");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .Contain(e => e is ValidationError);
    }

    [Fact]
    public void AddUser_WithShortPassword_ShouldFail()
    {
        // Act
        var result = _userService.AddUser(
            "Artem",
            "artem@example.com",
            "123");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .Contain(e => e is ValidationError);
    }

    [Fact]
    public void AddUser_WithDuplicateEmail_ShouldFail()
    {
        // Arrange
        _userService.AddUser(
            "Artem",
            "artem@example.com",
            "123456");

        // Act
        var result = _userService.AddUser(
            "AnotherArtem",
            "artem@example.com",
            "654321");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ConflictError>();
    }

    [Fact]
    public void AddUser_WithEmailDifferentOnlyByCase_ShouldFail()
    {
        // Arrange
        _userService.AddUser(
            "Artem",
            "artem@example.com",
            "123456");

        // Act
        var result = _userService.AddUser(
            "AnotherArtem",
            "ARTEM@EXAMPLE.COM",
            "654321");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ConflictError>();
    }

    [Fact]
    public void Login_WithCorrectCredentials_ShouldReturnUserId()
    {
        // Arrange
        var registerResult = _userService.AddUser(
            "Artem",
            "artem@example.com",
            "123456");
        
        // Act
        var loginResult = _userService.Login(
            "artem@example.com",
            "123456");

        // Assert
        loginResult.IsSuccess.Should().BeTrue();
        
    }

    [Fact]
    public void Login_WithWrongPassword_ShouldReturnAuthError()
    {
        // Arrange
        _userService.AddUser(
            "Artem",
            "artem@example.com",
            "123456");

        // Act
        var result = _userService.Login(
            "artem@example.com",
            "wrong-password");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<AuthError>();
    }

    [Fact]
    public void Login_WithUnknownEmail_ShouldReturnAuthError()
    {
        // Act
        var result = _userService.Login(
            "unknown@example.com",
            "123456");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<AuthError>();
    }

    [Fact]
    public void Login_WithEmptyEmail_ShouldReturnValidationError()
    {
        // Act
        var result = _userService.Login(
            "",
            "123456");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ValidationError>();
    }

    [Fact]
    public void Login_WithEmptyPassword_ShouldReturnValidationError()
    {
        // Act
        var result = _userService.Login(
            "artem@example.com",
            "");

        // Assert
        result.IsFailed.Should().BeTrue();

        result.Errors
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<ValidationError>();
    }

    [Fact]
    public void Login_EmailShouldBeCaseInsensitive()
    {
        // Arrange
        _userService.AddUser(
            "Artem",
            "artem@example.com",
            "123456");

        // Act
        var result = _userService.Login(
            "ARTEM@EXAMPLE.COM",
            "123456");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
