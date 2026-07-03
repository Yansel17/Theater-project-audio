using System.Text.Json;
using TheaterCue.Application;
using TheaterCue.Domain;

namespace TheaterCue.Infrastructure.Audio;

public sealed class JsonProjectRepository : IProjectRepository
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public async Task SaveAsync(ShowProject project, string filePath,
        CancellationToken cancellationToken = default)
    {
        project.LastModifiedUtc = DateTime.UtcNow;
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, project, _options, cancellationToken);
    }

    public async Task<ShowProject> LoadAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var project = await JsonSerializer.DeserializeAsync<ShowProject>(
            stream, _options, cancellationToken);
        return project ?? throw new InvalidDataException("El archivo .cueshow está vacío o corrupto.");
    }
}