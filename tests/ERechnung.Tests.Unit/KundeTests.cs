using ERechnung.Core.Models;
using Xunit;

namespace ERechnung.Tests.Unit;

public class KundeTests
{
    [Fact]
    public void Kunde_Should_Have_Default_Date()
    {
        // Arrange
        var kunde = new Kunde();

        // Assert
        Assert.True(kunde.ErstelltAm.Date == DateTime.Today);
        Assert.Equal("DE", kunde.Land);
    }

    [Fact]
    public void Kunde_Should_Allow_Update_Data()
    {
        // Arrange
        var kunde = new Kunde();

        // Act
        kunde.Firmenname = "Test GmbH";
        kunde.Email = "test@test.de";

        // Assert
        Assert.Equal("Test GmbH", kunde.Firmenname);
        Assert.NotNull(kunde.Email);
    }
}