namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Auth;

using AStar.Dev.OneDrive.Sync.Client.Services.Auth;

public class AuthResultTests
{
    [Fact]
    public void Success_ShouldCreateResultWithCorrectProperties()
    {
        // Arrange
        var accessToken = "access-token-abc123";
        var accountId = "account-123";
        var displayName = "Jason Smith";
        var email = "jason@outlook.com";

        // Act
        var result = AuthResult.Success(accessToken, accountId, displayName, email);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsCancelled.ShouldBeFalse();
        result.IsError.ShouldBeFalse();
        result.AccessToken.ShouldBe(accessToken);
        result.AccountId.ShouldBe(accountId);
        result.DisplayName.ShouldBe(displayName);
        result.Email.ShouldBe(email);
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Cancelled_ShouldCreateCancelledResult()
    {
        // Act
        var result = AuthResult.Cancelled();

        // Assert
        result.IsCancelled.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.IsError.ShouldBeFalse();
        result.AccessToken.ShouldBeNull();
        result.AccountId.ShouldBeNull();
        result.DisplayName.ShouldBeNull();
        result.Email.ShouldBeNull();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailureResultWithErrorMessage()
    {
        // Arrange
        var errorMessage = "Authentication failed: Invalid credentials";

        // Act
        var result = AuthResult.Failure(errorMessage);

        // Assert
        result.IsError.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.IsCancelled.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(errorMessage);
        result.AccessToken.ShouldBeNull();
        result.AccountId.ShouldBeNull();
        result.DisplayName.ShouldBeNull();
        result.Email.ShouldBeNull();
    }

    [Theory]
    [InlineData("token-1", "account-1", "User One", "user1@outlook.com")]
    [InlineData("token-2", "account-2", "User Two", "user2@outlook.com")]
    [InlineData("token-3", "account-3", "User Three", "user3@outlook.com")]
    public void Success_ShouldPreserveDifferentTokenAndAccountData(
        string accessToken,
        string accountId,
        string displayName,
        string email)
    {
        // Act
        var result = AuthResult.Success(accessToken, accountId, displayName, email);

        // Assert
        result.AccessToken.ShouldBe(accessToken);
        result.AccountId.ShouldBe(accountId);
        result.DisplayName.ShouldBe(displayName);
        result.Email.ShouldBe(email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Simple error")]
    [InlineData("Authentication failed: Invalid credentials")]
    [InlineData("Network error: Connection timeout")]
    public void Failure_ShouldPreserveDifferentErrorMessages(string errorMessage)
    {
        // Act
        var result = AuthResult.Failure(errorMessage);

        // Assert
        result.ErrorMessage.ShouldBe(errorMessage);
        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void IsError_ShouldBeTrueWhenNotSuccessAndNotCancelled()
    {
        // Act
        var result = AuthResult.Failure("Some error");

        // Assert
        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void IsError_ShouldBeFalseWhenSuccess()
    {
        // Act
        var result = AuthResult.Success("token", "account", "name", "email@test.com");

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public void IsError_ShouldBeFalseWhenCancelled()
    {
        // Act
        var result = AuthResult.Cancelled();

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public void SuccessResult_ShouldNotBeEqual_ToFailureResult()
    {
        // Arrange
        var successResult = AuthResult.Success("token", "account", "name", "email@test.com");
        var failureResult = AuthResult.Failure("error message");

        // Act & Assert
        successResult.IsSuccess.ShouldNotBe(failureResult.IsSuccess);
    }

    [Fact]
    public void CancelledResult_ShouldNotBeEqual_ToFailureResult()
    {
        // Arrange
        var cancelledResult = AuthResult.Cancelled();
        var failureResult = AuthResult.Failure("error message");

        // Act & Assert
        cancelledResult.IsCancelled.ShouldNotBe(failureResult.IsCancelled);
    }

    [Fact]
    public void SuccessResult_ShouldNotBeEqual_ToCancelledResult()
    {
        // Arrange
        var successResult = AuthResult.Success("token", "account", "name", "email@test.com");
        var cancelledResult = AuthResult.Cancelled();

        // Act & Assert
        successResult.IsSuccess.ShouldNotBe(cancelledResult.IsSuccess);
    }
}
