namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

using Avalonia.Media;
using AStar.Dev.OneDrive.Sync.Client.Converters;
using System.Globalization;

public class DepthToIndentConverterTests
{
    [Fact]
    public void Convert_WithDepth0_ShouldReturn0()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert(0, typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void Convert_WithDepth1_ShouldReturn16()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert(1, typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(16.0);
    }

    [Fact]
    public void Convert_WithDepth2_ShouldReturn32()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert(2, typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(32.0);
    }

    [Fact]
    public void Convert_WithDepth5_ShouldReturn80()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert(5, typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(80.0);
    }

    [Fact]
    public void Convert_WithDepth10_ShouldReturn160()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert(10, typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(160.0);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, 16.0)]
    [InlineData(2, 32.0)]
    [InlineData(3, 48.0)]
    [InlineData(4, 64.0)]
    [InlineData(5, 80.0)]
    [InlineData(10, 160.0)]
    [InlineData(100, 1600.0)]
    public void Convert_WithVaryingDepths_ShouldMultiplyBy16(int depth, double expected)
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert(depth, typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Convert_WithNullValue_ShouldReturn0()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert(null, typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void Convert_WithStringValue_ShouldReturn0()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert("not an int", typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(0.0);
    }

    [Fact]
    public void Convert_WithNegativeDepth_ShouldReturnNegativeValue()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act
        var result = converter.Convert(-5, typeof(double), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBe(-80.0);
    }

    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        // Assert - Instance property should return same object
        var instance1 = DepthToIndentConverter.Instance;
        var instance2 = DepthToIndentConverter.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void ConvertBack_ShouldThrowNotSupported()
    {
        // Arrange
        var converter = new DepthToIndentConverter();

        // Act & Assert
        Should.Throw<NotSupportedException>(() =>
            converter.ConvertBack(16.0, typeof(int), null, CultureInfo.CurrentCulture));
    }
}

public class ConflictCountToColorConverterTests
{
    [Fact]
    public void Convert_WithCount0_ShouldReturnDarkColor()
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act
        var result = converter.Convert(0, typeof(Color), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower().ShouldBe("#ff1a1917");
    }

    [Fact]
    public void Convert_WithCount1_ShouldReturnRedColor()
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act
        var result = converter.Convert(1, typeof(Color), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower().ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void Convert_WithCountGreaterThan0_ShouldReturnRedColor()
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act
        var result = converter.Convert(5, typeof(Color), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower().ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void Convert_WithNegativeCount_ShouldReturnDarkColor()
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act - Negative is not > 0, so should return dark color
        var result = converter.Convert(-1, typeof(Color), null, CultureInfo.CurrentCulture);

        // Assert
        result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower().ShouldBe("#ff1a1917");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Convert_WithZeroOrNegative_ShouldReturnDark(int count)
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act
        var result = converter.Convert(count, typeof(Color), null, CultureInfo.CurrentCulture);

        // Assert
        var color = (Color)result;
        color.ToString().ToLower().ShouldBe("#ff1a1917");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Convert_WithAnyPositiveCount_ShouldReturnRed(int count)
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act
        var result = converter.Convert(count, typeof(Color), null, CultureInfo.CurrentCulture);

        // Assert
        var color = (Color)result;
        color.ToString().ToLower().ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void Convert_WithNullValue_ShouldReturnDarkColor()
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act
        var result = converter.Convert(null, typeof(Color), null, CultureInfo.CurrentCulture);

        // Assert
        var color = (Color)result;
        color.ToString().ToLower().ShouldBe("#ff1a1917");
    }

    [Fact]
    public void Convert_WithStringValue_ShouldReturnDarkColor()
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act
        var result = converter.Convert("not a number", typeof(Color), null, CultureInfo.CurrentCulture);

        // Assert
        var color = (Color)result;
        color.ToString().ToLower().ShouldBe("#ff1a1917");
    }

    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        // Assert
        var instance1 = ConflictCountToColorConverter.Instance;
        var instance2 = ConflictCountToColorConverter.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void ConvertBack_ShouldThrowNotSupported()
    {
        // Arrange
        var converter = new ConflictCountToColorConverter();

        // Act & Assert
        Should.Throw<NotSupportedException>(() =>
            converter.ConvertBack(Color.Parse("#FFE24B4A"), typeof(int), null, CultureInfo.CurrentCulture));
    }
}
