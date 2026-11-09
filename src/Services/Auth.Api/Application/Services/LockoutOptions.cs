namespace Auth.Api.Application.Services;

public class LockoutOptions
{
    public const string SectionName = "Seguridad";

    public int MaxIntentosFallidos { get; set; } = 5;
    public int MinutosBloqueo { get; set; } = 15;
}
