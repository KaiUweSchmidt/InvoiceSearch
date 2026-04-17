using InvoiceSearch.Services;

namespace InvoiceSearch.Tests.Services;

public class WindowPlacementTests
{
    [Fact]
    public void WhenCreatedWithDefaultsThenAllValuesAreZeroOrFalse()
    {
        var placement = new WindowPlacement();

        Assert.Equal(0, placement.Left);
        Assert.Equal(0, placement.Top);
        Assert.Equal(0, placement.Width);
        Assert.Equal(0, placement.Height);
        Assert.False(placement.IsMaximized);
    }

    [Fact]
    public void WhenCreatedWithValuesThenPropertiesAreSet()
    {
        var placement = new WindowPlacement
        {
            Left = 100,
            Top = 200,
            Width = 800,
            Height = 600,
            IsMaximized = true
        };

        Assert.Equal(100, placement.Left);
        Assert.Equal(200, placement.Top);
        Assert.Equal(800, placement.Width);
        Assert.Equal(600, placement.Height);
        Assert.True(placement.IsMaximized);
    }

    [Fact]
    public void WhenTwoPlacementsHaveSameValuesThenTheyAreEqual()
    {
        var a = new WindowPlacement { Left = 10, Top = 20, Width = 300, Height = 400 };
        var b = new WindowPlacement { Left = 10, Top = 20, Width = 300, Height = 400 };

        Assert.Equal(a, b);
    }
}
