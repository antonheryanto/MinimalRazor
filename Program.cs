using Dapper;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using MinimalRazor;
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
	return new ProjectIndex(items.ToList(), context).ToHtml();
}).RequireAuthorization();
app.MapGet("/projects/edit", async (SqliteConnection cn, HttpContext context) => {
    var m = await cn.QueryFirstAsync<Project>("select * from projects where id=@id", new { id = 1 });
    return new ProjectDetails(m, context).ToHtml();
});
app.MapGet("/users/auth", (HttpContext context, IAntiforgery antiforgery) 
    => new UserAuth(antiforgery.GetAndStoreTokens(context)).ToHtml());
app.MapPost("/users/auth", async (SqliteConnection cn, IAntiforgery antiforgery, HttpContext context,
    [FromForm] string email, [FromForm] string password, [FromQuery] string? returnUrl) =>
{
    var m = await cn.QueryFirstAsync<User>("select * from users where email=@email", new { email });
    if (m is null || m.Password != password.Encrypt(m.Salt).Password)
        return new UserAuth(antiforgery.GetAndStoreTokens(context)).ToHtml();
    m.Roles = (await cn.QueryAsync<Role>("select r.* from roleuser ru left join roles r ON r.id=ru.rolesid where ru.usersid=@Id", new { m.Id })).ToList();
    await context.SignInAsync(authScheme, new(new ClaimsIdentity(m.ToClaims(), authScheme)), new() { ExpiresUtc = DateTime.UtcNow.AddDays(1) });
    return Results.Redirect($"{returnUrl ?? "/"}");
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

