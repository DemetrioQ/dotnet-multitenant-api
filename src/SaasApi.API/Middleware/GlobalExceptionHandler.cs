using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Common.Exceptions;

namespace SaasApi.API.Middleware
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
        {
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

            ProblemDetails problem;
            int statusCode;

            switch (exception)
            {
                case ValidationException ve:
                    statusCode = StatusCodes.Status400BadRequest;
                    problem = new ValidationProblemDetails(
                        ve.Errors
                          .GroupBy(e => e.PropertyName)
                          .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
                    {
                        Title = "Validation failed",
                        Status = statusCode
                    };
                    break;

                case NotFoundException nfe:
                    statusCode = StatusCodes.Status404NotFound;
                    problem = new ProblemDetails
                    {
                        Title = "Not found",
                        Detail = nfe.Message,
                        Status = statusCode
                    };
                    break;

                case ConflictException ce:
                    statusCode = StatusCodes.Status409Conflict;
                    problem = new ProblemDetails
                    {
                        Title = "Conflict",
                        Detail = ce.Message,
                        Status = statusCode
                    };
                    break;

                case BadRequestException bre:
                    statusCode = StatusCodes.Status400BadRequest;
                    problem = new ProblemDetails
                    {
                        Title = "Bad request",
                        Detail = bre.Message,
                        Status = statusCode
                    };
                    break;

                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    problem = new ProblemDetails
                    {
                        Title = "Unauthorized",
                        Detail = "Invalid credentials.",
                        Status = statusCode
                    };
                    break;
                case ArgumentException ae:
                    statusCode = StatusCodes.Status400BadRequest;
                    problem = new ProblemDetails
                    {
                        Title = "Invalid argument",
                        Detail = ae.Message,
                        Status = statusCode
                    };
                    break;
                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    problem = new ProblemDetails
                    {
                        Title = "An unexpected error occurred",
                        Status = statusCode
                    };
                    break;
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problem, ct);
            return true;
        }
    }
}
