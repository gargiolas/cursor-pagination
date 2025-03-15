using CursorPagination.Application.Features.Users.Queries.GetPagedUserCursorQuery;
using CursorPagination.Infrastructure;
using CursorPagination.Persistence.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.RegisterServices(builder.Configuration);
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.Services.ApplyMigration();
    app.Services.FillUserData();
}

//app.UseHttpsRedirection();

app.MapGet("", () => "API Started")
    .WithName("");

app.MapGet("/data",
        async ([FromQuery] string? cursor, [FromQuery]bool? isNext, ISender sender) =>
        {
            var result = await sender.Send(new GetPagedUserCursorQuery(cursor, isNext ?? true));
            return result.Items.Count == 0 ? Results.NotFound() : Results.Ok(result);
        })
    .WithName("GetCursor");

app.Run();
