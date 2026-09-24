using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using ResQ.API.ExceptionHandling;

namespace ResQ.Tests;

public class ApiExceptionHandlerTests
{
    [Fact]
    public async Task ValidationException_ReturnsBadRequestValidationProblemDetails()
    {
        var service = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(service, NullLogger<ApiExceptionHandler>.Instance);
        var context = CreateContext();
        var exception = new ValidationException("Invalid name", [new FluentValidation.Results.ValidationFailure("Name", "Name is required")]);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var details = Assert.IsType<ValidationProblemDetails>(service.ProblemDetails);
        Assert.Equal("Name is required", Assert.Single(details.Errors["Name"]));
    }

    [Fact]
    public async Task KeyNotFoundException_ReturnsNotFoundProblemDetails()
    {
        var service = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(service, NullLogger<ApiExceptionHandler>.Instance);
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new KeyNotFoundException("Emergency not found"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("Emergency not found", service.ProblemDetails?.Detail);
    }

    [Fact]
    public async Task UnexpectedException_RemainsInternalServerError()
    {
        var service = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(service, NullLogger<ApiExceptionHandler>.Instance);
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("Sensitive details"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", service.ProblemDetails?.Title);
        Assert.Null(service.ProblemDetails?.Detail);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? ProblemDetails { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            ProblemDetails = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            ProblemDetails = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }
}
