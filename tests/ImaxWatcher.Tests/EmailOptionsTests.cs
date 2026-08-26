using System.ComponentModel.DataAnnotations;
using ImaxWatcher.Options;

namespace ImaxWatcher.Tests;

public class EmailOptionsRecipientsTests
{
    [Fact]
    public void SplitsACommaSeparatedToIntoMultipleAddresses()
    {
        var options = NewOptions(to: "a@x.com, b@y.com");

        Assert.Equal(["a@x.com", "b@y.com"], options.Recipients);
    }

    [Fact]
    public void TreatsAnEmptyToAsNoRecipients()
    {
        var options = NewOptions(to: "");

        Assert.Empty(options.Recipients);
    }

    private static EmailOptions NewOptions(string to) => new()
    {
        Host = "smtp.gmail.com",
        From = "sender@x.com",
        To = to,
        Password = "secret",
    };
}

public class EmailOptionsValidationTests
{
    [Fact]
    public void FailsWhenEnabledAndToIsEmpty()
    {
        Assert.False(Validate(enabled: true, to: ""));
    }

    [Fact]
    public void FailsWhenEnabledAndOneAddressIsInvalid()
    {
        Assert.False(Validate(enabled: true, to: "ok@x.com,not-an-address"));
    }

    [Fact]
    public void PassesWhenEnabledWithMultipleValidAddresses()
    {
        Assert.True(Validate(enabled: true, to: "a@x.com,b@y.com"));
    }

    [Fact]
    public void PassesWhenDisabledAndToIsEmpty()
    {
        Assert.True(Validate(enabled: false, to: ""));
    }

    private static bool Validate(bool enabled, string to)
    {
        var options = new EmailOptions
        {
            Enabled = enabled,
            Host = "smtp.gmail.com",
            Port = 465,
            From = "sender@x.com",
            To = to,
            Password = "secret",
        };

        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);
    }
}
