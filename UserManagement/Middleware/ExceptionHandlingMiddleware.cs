using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;
using System.Text.Json;
using UserManagement.Domain;

namespace UserManagement.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                var response = ApiResponse<object>.Fail("This email is already registered.");
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (Exception)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = ApiResponse<object>.Fail("An unexpected error occurred.");
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}