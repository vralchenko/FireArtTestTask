using FireArtTestTask.Api.DTOs.Products;
using FireArtTestTask.Api.Validators;
using FluentAssertions;

namespace FireArtTestTask.Tests.Unit.Validators;

public class ProductValidatorTests
{
    private readonly CreateProductRequestValidator _createValidator = new();
    private readonly UpdateProductRequestValidator _updateValidator = new();

    [Fact]
    public void CreateValidator_ValidRequest_Passes()
    {
        var result = _createValidator.Validate(new CreateProductRequest("Widget", "Desc", 10m, "Gadgets"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_EmptyName_Fails()
    {
        var result = _createValidator.Validate(new CreateProductRequest("", "Desc", 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_NegativePrice_Fails()
    {
        var result = _createValidator.Validate(new CreateProductRequest("Name", "Desc", -1m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_ZeroPrice_Fails()
    {
        var result = _createValidator.Validate(new CreateProductRequest("Name", "Desc", 0m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_EmptyCategory_Fails()
    {
        var result = _createValidator.Validate(new CreateProductRequest("Name", "Desc", 10m, ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateValidator_TooLongName_Fails()
    {
        var result = _createValidator.Validate(
            new CreateProductRequest(new string('x', 201), "Desc", 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_ValidRequest_Passes()
    {
        var result = _updateValidator.Validate(new UpdateProductRequest("Widget", "Desc", 10m, "Cat"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateValidator_EmptyName_Fails()
    {
        var result = _updateValidator.Validate(new UpdateProductRequest("", "Desc", 10m, "Cat"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_NegativePrice_Fails()
    {
        var result = _updateValidator.Validate(new UpdateProductRequest("Name", "Desc", -5m, "Cat"));
        result.IsValid.Should().BeFalse();
    }
}
