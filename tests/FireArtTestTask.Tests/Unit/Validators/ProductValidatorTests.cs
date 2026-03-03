using FireArtTestTask.Application.Products.Commands;
using FireArtTestTask.Application.Validators;
using FluentAssertions;

namespace FireArtTestTask.Tests.Unit.Validators;

public class ProductValidatorTests
{
    private readonly CreateProductCommandValidator _createValidator = new();
    private readonly UpdateProductCommandValidator _updateValidator = new();

    // ── CreateProductCommandValidator ───────────────────────────────

    [Fact]
    public void CreateValidator_ValidRequest_Passes()
    {
        var result = _createValidator.Validate(new CreateProductCommand("Widget", "Desc", 10m, "Gadgets"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_EmptyName_Fails()
    {
        var result = _createValidator.Validate(new CreateProductCommand("", "Desc", 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_NegativePrice_Fails()
    {
        var result = _createValidator.Validate(new CreateProductCommand("Name", "Desc", -1m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_ZeroPrice_Fails()
    {
        var result = _createValidator.Validate(new CreateProductCommand("Name", "Desc", 0m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_EmptyCategory_Fails()
    {
        var result = _createValidator.Validate(new CreateProductCommand("Name", "Desc", 10m, ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_TooLongName_Fails()
    {
        var result = _createValidator.Validate(
            new CreateProductCommand(new string('x', 201), "Desc", 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_NameExactly200Chars_Passes()
    {
        var result = _createValidator.Validate(
            new CreateProductCommand(new string('x', 200), "Desc", 10m, "Cat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_DescriptionOver2000Chars_Fails()
    {
        var result = _createValidator.Validate(
            new CreateProductCommand("Name", new string('x', 2001), 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_DescriptionExactly2000Chars_Passes()
    {
        var result = _createValidator.Validate(
            new CreateProductCommand("Name", new string('x', 2000), 10m, "Cat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_CategoryOver100Chars_Fails()
    {
        var result = _createValidator.Validate(
            new CreateProductCommand("Name", "Desc", 10m, new string('x', 101)));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_CategoryExactly100Chars_Passes()
    {
        var result = _createValidator.Validate(
            new CreateProductCommand("Name", "Desc", 10m, new string('x', 100)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_PriceMinimumValid_Passes()
    {
        var result = _createValidator.Validate(
            new CreateProductCommand("Name", "Desc", 0.01m, "Cat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_MultipleSimultaneousErrors()
    {
        var result = _createValidator.Validate(
            new CreateProductCommand("", "Desc", -1m, ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    // ── UpdateProductCommandValidator ───────────────────────────────

    [Fact]
    public void UpdateValidator_ValidRequest_Passes()
    {
        var result = _updateValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "Widget", "Desc", 10m, "Cat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateValidator_EmptyName_Fails()
    {
        var result = _updateValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "", "Desc", 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_NegativePrice_Fails()
    {
        var result = _updateValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "Name", "Desc", -5m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_ZeroPrice_Fails()
    {
        var result = _updateValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "Name", "Desc", 0m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_EmptyCategory_Fails()
    {
        var result = _updateValidator.Validate(new UpdateProductCommand(Guid.NewGuid(), "Name", "Desc", 10m, ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_NameExactly200Chars_Passes()
    {
        var result = _updateValidator.Validate(
            new UpdateProductCommand(Guid.NewGuid(), new string('x', 200), "Desc", 10m, "Cat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateValidator_Name201Chars_Fails()
    {
        var result = _updateValidator.Validate(
            new UpdateProductCommand(Guid.NewGuid(), new string('x', 201), "Desc", 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_DescriptionOver2000Chars_Fails()
    {
        var result = _updateValidator.Validate(
            new UpdateProductCommand(Guid.NewGuid(), "Name", new string('x', 2001), 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_DescriptionExactly2000Chars_Passes()
    {
        var result = _updateValidator.Validate(
            new UpdateProductCommand(Guid.NewGuid(), "Name", new string('x', 2000), 10m, "Cat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateValidator_CategoryOver100Chars_Fails()
    {
        var result = _updateValidator.Validate(
            new UpdateProductCommand(Guid.NewGuid(), "Name", "Desc", 10m, new string('x', 101)));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_CategoryExactly100Chars_Passes()
    {
        var result = _updateValidator.Validate(
            new UpdateProductCommand(Guid.NewGuid(), "Name", "Desc", 10m, new string('x', 100)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateValidator_PriceMinimumValid_Passes()
    {
        var result = _updateValidator.Validate(
            new UpdateProductCommand(Guid.NewGuid(), "Name", "Desc", 0.01m, "Cat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateValidator_MultipleSimultaneousErrors()
    {
        var result = _updateValidator.Validate(
            new UpdateProductCommand(Guid.NewGuid(), "", "Desc", -1m, ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
