using Control.Api.Application.Common;
using Control.Api.Domain;

namespace Control.Api.Tests;

public class CurrentUserTests
{
    [Fact]
    public void Operador_SoloVeSusPropiasCargas()
    {
        var operador = new CurrentUser(5, "op@test.com", "Operador", Roles.Operador);

        Assert.True(operador.PuedeVer(new Carga { UsuarioId = 5 }));
        Assert.False(operador.PuedeVer(new Carga { UsuarioId = 9 }));
    }

    [Theory]
    [InlineData(Roles.Administrador)]
    [InlineData(Roles.Consulta)]
    public void AdministradorYConsulta_VenTodasLasCargas(string rol)
    {
        var usuario = new CurrentUser(1, "user@test.com", "Usuario", rol);

        Assert.True(usuario.PuedeVer(new Carga { UsuarioId = 99 }));
    }
}
