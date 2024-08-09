namespace BuildingBlocks.Web;

using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MapsterMapper;

[ApiController]
[Route(BaseApiPath)]
[ApiVersion("1.0")]
public abstract class BaseController : ControllerBase
{
    protected const string BaseApiPath = "api/v{version:apiVersion}";

    private IMediator _mediator;
    private IMapper _mapper;

    // Lazy initialization of IMediator
    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    // Lazy initialization of IMapper
    protected IMapper Mapper =>
        _mapper ??= HttpContext.RequestServices.GetRequiredService<IMapper>();
}
