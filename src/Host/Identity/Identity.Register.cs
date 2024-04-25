using System.Security.Claims;
using HyperTextExpression.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static HyperTextExpression.HtmlExp;

public static partial class Identity
{
    public static void Register(IServiceCollection services, Action<DbContextOptionsBuilder> dbOptions)
    {
        services.AddAuthentication()
            .AddCookie(IdentityConstants.BearerScheme);
        services.AddAuthorizationBuilder();

        services.AddDbContext<Context>(dbOptions);

        services.AddIdentityCore<User>()
            .AddEntityFrameworkStores<Context>()
            .AddApiEndpoints();
    }

    public static void Map(WebApplication app)
    {
        app
            .MapGroup("identity/")
            .MapIdentityApi<User>();

        app.MapGet("/", async (ClaimsPrincipal user, Todo.Context context) =>
        {
            if(user?.Identity?.Name is null)
            {
                Console.WriteLine("Need to login");

                return LoginPage.Render();
            }
            else
            {
                Console.WriteLine("LoggedIn");

                return await Todo.List.Render(user, context);
            } 
        })
        .WithOpenApi()
        .AllowAnonymous();

        app.MapGet("/refresh", (HttpContext context) =>
        {
            context.Response.Headers.Append("HX-Refresh", "true");

            return Results.Ok();
        })
        .WithOpenApi()
        .AllowAnonymous();

        using var scope = app.Services.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<Context>();
    
        context.Database.EnsureCreated();
    }

    public static class LoginPage
    {
        public static class Element
        {
            public const string LoginInput = "input-login";
            public const string PasswordInput = "input-password";
        }

        public static IResult Render() => HtmlDoc(
            Head(
                ("title", "Login page!")
            ),
            Body(
                Div(
                    Attrs("style", "max-width: 800px; margin: auto; margin-bottom: 5rem;"),
                    Div(
                        HtmlEl("form",
                            ("h4", "Login or Register"),
                            ("input",
                                Attrs(
                                    ("id", Element.LoginInput),
                                    ("type", "text"),
                                    ("name", "email")
                                )
                            ),
                            ("input",
                                Attrs(
                                    ("id", Element.PasswordInput),
                                    ("type", "password"),
                                    ("name", "password")
                                )
                            ),
                            ("button",
                                Attrs(
                                    Htmx.Post("/identity/login"),
                                    Htmx.Ext("json-enc"),
                                    Htmx.OnAfterRequest($"htmx.trigger('#refresh','click')")
                                ),
                                "Login"),
                            ("button",
                                Attrs(
                                    Htmx.Post("/identity/register"),
                                    Htmx.Ext("json-enc"),
                                    Htmx.OnAfterRequest($"htmx.trigger('#refresh','click')")
                                ),
                                "Register"),
                            Div(
                                Attrs(
                                    ("id", "refresh"),
                                    ("hx-trigger", "click"),
                                    Htmx.Get("/refresh")
                                ))
                        )
                    )
                ),
                Htmx.HtmxScript,
                Htmx.HtmxJsonEncScript
            )
        ).ToIResult();
    }
}