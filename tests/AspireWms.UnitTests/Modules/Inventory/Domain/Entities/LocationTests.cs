using AspireWms.Api.Modules.Inventory.Domain.Entities;

namespace AspireWms.UnitTests.Modules.Inventory.Domain.Entities;

public sealed class LocationTests
{
    [Test]
    public async Task Create_WithValidData_ReturnsSuccessWithLocation()
    {
        // Act
        var result = Location.Create("A", 1, 2, 3, "Test Location", 50);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Code).IsEqualTo("A-01-02-03");
        await Assert.That(result.Value.Zone).IsEqualTo("A");
        await Assert.That(result.Value.Aisle).IsEqualTo(1);
        await Assert.That(result.Value.Rack).IsEqualTo(2);
        await Assert.That(result.Value.Bin).IsEqualTo(3);
        await Assert.That(result.Value.Capacity).IsEqualTo(50);
        await Assert.That(result.Value.IsActive).IsTrue();
    }

    [Test]
    public async Task Create_NormalizesZone_ToUpperCase()
    {
        // Act
        var result = Location.Create("a", 1, 1, 1);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Zone).IsEqualTo("A");
        await Assert.That(result.Value.Code).IsEqualTo("A-01-01-01");
    }

    [Test]
    public async Task Create_GeneratesDefaultName_WhenNotProvided()
    {
        // Act
        var result = Location.Create("B", 2, 3, 4);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Name).IsEqualTo("Zone B, Aisle 2, Rack 3, Bin 4");
    }

    [Test]
    [Arguments("", "Zone must be a single letter")]
    [Arguments("AB", "Zone must be a single letter")]
    [Arguments("1", "Zone must be a single letter")]
    public async Task Create_WithInvalidZone_ReturnsFailure(string zone, string expectedError)
    {
        // Act
        var result = Location.Create(zone, 1, 1, 1);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains(expectedError);
    }

    [Test]
    [Arguments(0)]
    [Arguments(100)]
    public async Task Create_WithInvalidAisle_ReturnsFailure(int aisle)
    {
        // Act
        var result = Location.Create("A", aisle, 1, 1);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Aisle must be between 1 and 99");
    }

    [Test]
    [Arguments(0)]
    [Arguments(100)]
    public async Task Create_WithInvalidRack_ReturnsFailure(int rack)
    {
        // Act
        var result = Location.Create("A", 1, rack, 1);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Rack must be between 1 and 99");
    }

    [Test]
    [Arguments(0)]
    [Arguments(100)]
    public async Task Create_WithInvalidBin_ReturnsFailure(int bin)
    {
        // Act
        var result = Location.Create("A", 1, 1, bin);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Bin must be between 1 and 99");
    }

    [Test]
    public async Task Create_WithZeroCapacity_ReturnsFailure()
    {
        // Act
        var result = Location.Create("A", 1, 1, 1, capacity: 0);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Message).Contains("Capacity must be at least 1");
    }

    [Test]
    [Arguments("A-01-02-03")]
    [Arguments("a-01-02-03")]
    [Arguments("Z-99-99-99")]
    public async Task CreateFromCode_WithValidCode_ReturnsSuccess(string code)
    {
        // Act
        var result = Location.CreateFromCode(code);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task CreateFromCode_ParsesComponentsCorrectly()
    {
        // Act
        var result = Location.CreateFromCode("B-05-10-15");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Zone).IsEqualTo("B");
        await Assert.That(result.Value.Aisle).IsEqualTo(5);
        await Assert.That(result.Value.Rack).IsEqualTo(10);
        await Assert.That(result.Value.Bin).IsEqualTo(15);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("A-1-2-3")]
    [Arguments("AA-01-02-03")]
    [Arguments("A01-02-03")]
    [Arguments("A-01-02")]
    [Arguments("Invalid")]
    public async Task CreateFromCode_WithInvalidCode_ReturnsFailure(string code)
    {
        // Act
        var result = Location.CreateFromCode(code);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task UpdateCapacity_WithValidValue_UpdatesCapacity()
    {
        // Arrange
        var location = Location.Create("A", 1, 1, 1, capacity: 50).Value;

        // Act
        var result = location.UpdateCapacity(100);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(location.Capacity).IsEqualTo(100);
        await Assert.That(location.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task UpdateCapacity_WithZero_ReturnsFailure()
    {
        // Arrange
        var location = Location.Create("A", 1, 1, 1).Value;

        // Act
        var result = location.UpdateCapacity(0);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var location = Location.Create("A", 1, 1, 1).Value;

        // Act
        location.Deactivate();

        // Assert
        await Assert.That(location.IsActive).IsFalse();
    }
}
