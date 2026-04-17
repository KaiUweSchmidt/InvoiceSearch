using InvoiceSearch.Models;

namespace InvoiceSearch.Tests.Models;

public class ClassificationRuleTests
{
    [Fact]
    public void WhenCreatedWithDefaultsThenIdIsZero()
    {
        var rule = new ClassificationRule();

        Assert.Equal(0, rule.Id);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenNameIsEmpty()
    {
        var rule = new ClassificationRule();

        Assert.Equal(string.Empty, rule.Name);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenPatternIsEmpty()
    {
        var rule = new ClassificationRule();

        Assert.Equal(string.Empty, rule.Pattern);
    }

    [Fact]
    public void WhenCreatedWithDefaultsThenActionIsExclude()
    {
        var rule = new ClassificationRule();

        Assert.Equal("Exclude", rule.Action);
    }

    [Fact]
    public void WhenCreatedWithValuesThenPropertiesAreSet()
    {
        var rule = new ClassificationRule
        {
            Id = 5,
            Name = "Insurance",
            Pattern = "Versicherung",
            Action = "Include"
        };

        Assert.Equal(5, rule.Id);
        Assert.Equal("Insurance", rule.Name);
        Assert.Equal("Versicherung", rule.Pattern);
        Assert.Equal("Include", rule.Action);
    }

    [Fact]
    public void WhenTwoRulesHaveSameValuesThenTheyAreEqual()
    {
        var rule1 = new ClassificationRule { Id = 1, Name = "Test", Pattern = "abc", Action = "Exclude" };
        var rule2 = new ClassificationRule { Id = 1, Name = "Test", Pattern = "abc", Action = "Exclude" };

        Assert.Equal(rule1, rule2);
    }

    [Fact]
    public void WhenTwoRulesDifferThenTheyAreNotEqual()
    {
        var rule1 = new ClassificationRule { Id = 1, Name = "Test" };
        var rule2 = new ClassificationRule { Id = 2, Name = "Test" };

        Assert.NotEqual(rule1, rule2);
    }
}
