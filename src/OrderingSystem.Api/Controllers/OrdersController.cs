using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.Orders.Commands.CreateOrder;
using OrderingSystem.Application.Orders.Queries.GetOrders; // You'll need this namespace
using OrderingSystem.Application.Orders.Querys.GetOrderById;

namespace OrderingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderCommand command)
    {
        // Add this to verify the API receives the call
        Console.WriteLine($">>> API: Received CreateOrderCommand for {command.CustomerName}");
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        // Instead of a service, we send a Query to MediatR
        var query = new GetOrdersQuery();
        var result = await mediator.Send(query);

        return Ok(result);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(id));

        return result is not null ? Ok(result) : NotFound();
    }
}