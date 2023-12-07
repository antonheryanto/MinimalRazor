using Dapper;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MinimalRazor.Models;
using MinimalRazor.Pages;
using System.Security.Claims;
using System.Text.Json.Serialization;

[module: DapperAot]

var builder = WebApplication.CreateSlimBuilder(args);
var env = builder.Environment;
var cfg = builder.Configuration;
var cs = cfg.GetConnectionString("db") ?? $"Data Source={env.ContentRootPath}/db.sqlite3";
var name = nameof(MinimalRazor);
var authScheme = CookieAuthenticationDefaults.AuthenticationScheme;
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(_ => new SqliteConnection(cs));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
builder.Services.AddAuthentication(authScheme).AddCookie(o =>
{
    o.LoginPath = "/users/auth";
    o.Cookie.Name = name;
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();

var app = builder.Build();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapGet("/", async (SqliteConnection cn, HttpContext context) => {
    var items = await cn.QueryAsync<Project>("select * from projects");
    var user = context?.User?.Identity?.Name ?? "Anonymous";
    var tx = new ProjectIndex(items.ToList());
    return Results.Content(tx.Render(), "text/html");
}).RequireAuthorization();
app.MapGet("/projects/edit", async (SqliteConnection cn) => {
    var m = await cn.QueryFirstAsync<Project>("select * from projects where id=@id", new { id = 1 });
    var tx = new ProjectDetails(m);
    return Results.Content(tx.Render(), "text/html");
});
app.MapGet("/users/auth", (HttpContext context, IAntiforgery antiforgery) => {
    var token = antiforgery.GetAndStoreTokens(context);
    return Results.Content(new UserAuth(token.RequestToken).Render(), "text/html");
});
app.MapPost("/users/auth", async (SqliteConnection cn, IAntiforgery antiforgery, HttpContext context,
    [FromForm] string email, [FromQuery] string? returnUrl) =>
{
    var m = await cn.QueryFirstAsync<User>("select * from users where email=@email", new { email });
    if (m is not null)
    {
        await context.SignInAsync(authScheme, new(new ClaimsIdentity(m.ToClaims(), authScheme)), new() { ExpiresUtc = DateTime.UtcNow.AddDays(1) });
        return Results.Redirect($"{returnUrl ?? "/"}");
    }
    var token = antiforgery.GetAndStoreTokens(context);
    return Results.Content(new UserAuth(token.RequestToken).Render(), "text/html");
});
app.MapGet("/users/logout", (HttpContext context) =>
{
    context.SignOutAsync(authScheme);
    return Results.Redirect("/");
});
app.Run();
[JsonSerializable(typeof(User[]))]
[JsonSerializable(typeof(Project[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;

