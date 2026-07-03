using TheaterCue.Domain;

namespace TheaterCue.Application;

/// <summary>
/// Abstracción de la persistencia del proyecto. La implementación concreta
/// (Infrastructure.Persistence) decide el formato real (JSON) y el manejo de
/// errores de E/S; Application y UI solo conocen este contrato.
/// </summary>
public interface IProjectRepository
{
    Task SaveAsync(ShowProject project, string filePath, CancellationToken cancellationToken = default);

    Task<ShowProject> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
