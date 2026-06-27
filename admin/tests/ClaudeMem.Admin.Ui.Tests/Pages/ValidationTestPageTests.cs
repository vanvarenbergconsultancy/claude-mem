using AwesomeAssertions;
using Bunit;
using ClaudeMem.Admin.Ui.Components.Pages.Dev;
using ClaudeMem.Admin.Ui.Tests.Infrastructure;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Pages;

public sealed class ValidationTestPageTests : UiTestContext
{
    [Fact]
    public void SimulateValidation_Shows_FieldErrors_On_Both_Fields()
    {
        var page = Render<ValidationTestPage>();

        var buttons = page.FindAll("button");
        buttons[0].Click();

        page.Markup.Should().Contain("Name must be at least 4 characters.");
        page.Markup.Should().Contain("Name must not contain special characters.");
        page.Markup.Should().Contain("Description is too long.");
    }

    [Fact]
    public void SimulateAuth_Shows_GeneralError_Not_FieldErrors()
    {
        var page = Render<ValidationTestPage>();

        var buttons = page.FindAll("button");
        buttons[1].Click();

        page.Markup.Should().Contain("Authentication failed");
        page.Markup.Should().NotContain("mud-input-error");
    }

    [Fact]
    public void SimulateNetwork_Shows_ConnectionError_Not_FieldErrors()
    {
        var page = Render<ValidationTestPage>();

        var buttons = page.FindAll("button");
        buttons[2].Click();

        page.Markup.Should().Contain("Could not reach the server");
        page.Markup.Should().NotContain("mud-input-error");
    }

    [Fact]
    public void Clear_Removes_All_Errors()
    {
        var page = Render<ValidationTestPage>();

        var buttons = page.FindAll("button");
        buttons[0].Click();

        page.Markup.Should().Contain("Name must be at least 4 characters.");

        buttons = page.FindAll("button");
        buttons[3].Click();

        page.Markup.Should().NotContain("Name must be at least 4 characters.");
        page.Markup.Should().NotContain("Description is too long.");
        page.Markup.Should().NotContain("Authentication failed");
        page.Markup.Should().NotContain("Could not reach the server");
    }
}
