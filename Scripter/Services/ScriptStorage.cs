using System.Text.Json;
using System.Text.Json.Serialization;
using Scripter.Models;

namespace Scripter.Services;

public static class ScriptStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string ScriptsDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Scripter");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static IEnumerable<(string Name, string Path, Script Script)> ListScripts()
    {
        foreach (var path in Directory.EnumerateFiles(ScriptsDirectory, "*.json"))
        {
            Script? script = null;
            try
            {
                script = Load(path);
            }
            catch
            {
                continue;
            }

            if (script != null)
            {
                yield return (script.Name, path, script);
            }
        }
    }

    public static Script Load(string path)
    {
        var json = File.ReadAllText(path);
        var script = JsonSerializer.Deserialize<Script>(json, JsonOptions)
            ?? throw new InvalidDataException("Script file is empty or invalid.");
        script.Actions ??= [];
        return script;
    }

    public static void Save(Script script, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(script, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static string ImagesDirectory
    {
        get
        {
            var dir = Path.Combine(ScriptsDirectory, "images");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SaveImage(Bitmap bitmap)
    {
        var fileName = $"{Guid.NewGuid():N}.png";
        var path = Path.Combine(ImagesDirectory, fileName);
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return fileName;
    }

    public static string? ResolveImagePath(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        if (Path.IsPathRooted(stored) && File.Exists(stored))
        {
            return stored;
        }

        var combined = Path.Combine(ImagesDirectory, Path.GetFileName(stored));
        return File.Exists(combined) ? combined : stored;
    }

    public static string GetDefaultPath(Script script)
    {
        var safeName = SanitizeFileName(script.Name);
        return Path.Combine(ScriptsDirectory, $"{safeName}.json");
    }

    public static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Untitled" : cleaned;
    }
}
