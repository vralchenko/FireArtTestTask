using FireArtTestTask.Application.Products.Commands;
using FireArtTestTask.Application.Validators;
using FluentAssertions;

namespace FireArtTestTask.Tests.Unit.Validators;

public class ProductValidatorTests
{
    private readonly CreateProductCommandValidator _createValidator = new();
    private readonly UpdateProductCommandValidator _updateValidator = new();

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
}
