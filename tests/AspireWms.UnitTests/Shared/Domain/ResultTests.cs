using AspireWms.Api.Shared.Domain;

namespace AspireWms.UnitTests.Shared.Domain;

public class ResultTests
{
    [Test]
    public async Task Success_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.IsFailure).IsFalse();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Test.Error");
    }

    [Test]
    public async Task ImplicitConversion_FromValue_ShouldCreateSuccess()
    {
        // Arrange & Act
        Result<string> result = "hello";

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo("hello");
    }

    [Test]
    public async Task ImplicitConversion_FromError_ShouldCreateFailure()
    {
        // Arrange
        var error = new Error("Code", "Message");

        // Act
        Result<int> result = error;

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(error);
    }

    [Test]
    public async Task Match_OnSuccess_ShouldCallSuccessFunc()
    {
        // Arrange
        var result = Result<int>.Success(10);

        // Act
        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onFailure: e => $"Error: {e.Code}");

        // Assert
        await Assert.That(output).IsEqualTo("Value: 10");
    }

    [Test]
    public async Task Match_OnFailure_ShouldCallFailureFunc()
    {
        // Arrange
        var result = Result<int>.Failure(new Error("Err", "Failed"));

        // Act
        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onFailure: e => $"Error: {e.Code}");

        // Assert
        await Assert.That(output).IsEqualTo("Error: Err");
    }

    [Test]
    public async Task NonGenericResult_Success_ShouldWork()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task NonGenericResult_Failure_ShouldWork()
    {
        // Arrange & Act
        var result = Result.Failure(Error.Validation("Field", "Invalid"));

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("Validation.Field");
    }
}
