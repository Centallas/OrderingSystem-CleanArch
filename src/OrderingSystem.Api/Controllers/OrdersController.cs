using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.Orders.Commands.CreateOrder;
using OrderingSystem.Application.Orders.Queries.GetOrders; // You'll need this namespace

namespace OrderingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        // Instead of a service, we send a Query to MediatR
        var query = new GetOrdersQuery();
        var result = await mediator.Send(query);
        
        return Ok(result);
    }
}