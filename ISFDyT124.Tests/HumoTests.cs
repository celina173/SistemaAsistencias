using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ISFDyT124.Tests;

public class HumoTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;

    public HumoTests(AppFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_responde_200()
    {
        var cliente = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        var respuesta = await cliente.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
