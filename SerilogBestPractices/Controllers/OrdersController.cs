using MediatR;
using Microsoft.AspNetCore.Mvc;
using SerilogBestPractices.Features.Orders;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(
        IMediator mediator, 
        ILogger<OrdersController> logger
        ) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            logger.LogInformation(
                "Received create order request for customer {CustomerName}",
                request.CustomerName);

            var command = new CreateOrderCommand(request.CustomerName, request.Amount);
            var order = await mediator.Send(command);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var query = new GetOrderQuery(id);
            var order = await mediator.Send(query);

            if (order is null)
            {
                return NotFound();
            }

            return Ok(order);
        }
    }
}
