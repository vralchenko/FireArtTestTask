using FireArtTestTask.Application.Products.Commands;
using FireArtTestTask.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FireArtTestTask.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _mediator.Send(new GetProductByIdQuery(id)));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchProductsQuery query)
        => Ok(await _mediator.Send(query));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand command)
    {
        var cmd = command with { Id = id };
        return Ok(await _mediator.Send(cmd));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }
}
