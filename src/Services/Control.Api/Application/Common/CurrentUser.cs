using Control.Api.Domain;

namespace Control.Api.Application.Common;

public record CurrentUser(int Id, string Email, string Name, string Role)
{
    public bool PuedeVerTodas => Roles.PuedeVerTodas(Role);

    public bool PuedeVer(Carga carga) => PuedeVerTodas || carga.UsuarioId == Id;
}
