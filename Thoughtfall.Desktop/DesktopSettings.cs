namespace Thoughtfall.Desktop;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>Reads desktop preferences and replaces settings.json while preserving other settings.</summary>
internal sealed class DesktopSettings(string folder)
{
    private readonly string _filePath = Path.Combine(folder, "settings.json");

    internal CaptureShortcut? LoadShortcut(out string? error)
    {
        try
        {
            var shortcut = ReadShortcut(ReadDocument());
            error = null;
            return shortcut;
        }
        catch (Exception ex) when (IsSettingsError(ex))
        {
            error = $"Couldn't read capture shortcut settings. Using Ctrl + Alt + J for now.\n{ex.Message}";
            return CaptureShortcut.Default;
        }
    }

    internal AppearancePreferences LoadAppearance(out string? error)
    {
        try
        {
            var document = ReadDocument();
            var scheme = ReadString(document["colourScheme"]) == "Forest" ? ColourScheme.Forest : ColourScheme.Hearth;
            var appearance = ReadString(document["appearance"]) switch
            {
                "Light" => AppearanceMode.Light,
                "Dark" => AppearanceMode.Dark,
                _ => AppearanceMode.System,
            };
            error = null;
            return new AppearancePreferences(scheme, appearance);
        }
        catch (Exception ex) when (IsSettingsError(ex))
        {
            error = $"Couldn't read appearance settings. Using Hearth and System for now.\n{ex.Message}";
            return new AppearancePreferences();
        }
    }

    internal string? SaveAppearance(AppearancePreferences preferences)
    {
        try
        {
            var document = ReadDocument();
            document["colourScheme"] = preferences.ColourScheme.ToString();
            document["appearance"] = preferences.Appearance.ToString();
            WriteDocument(document);
            return null;
        }
        catch (Exception ex) when (IsSettingsError(ex))
        {
            return $"Couldn't save appearance settings. Your previous preferences are unchanged.\n{ex.Message}";
        }
    }

    internal string? SaveShortcut(CaptureShortcut? shortcut)
    {
        try
        {
            // Re-read before writing: unreadable or invalid settings must never be silently replaced.
            var document = ReadDocument();
            ReadShortcut(document);

            document["captureShortcut"] = shortcut is null
                ? null
                : new JsonObject
                {
                    ["key"] = shortcut.KeyName,
                    ["control"] = shortcut.Control,
                    ["alt"] = shortcut.Alt,
                    ["shift"] = shortcut.Shift,
                };

            WriteDocument(document);
            return null;
        }
        catch (Exception ex) when (IsSettingsError(ex))
        {
            return $"Couldn't save capture shortcut settings. Your previous preference is unchanged.\n{ex.Message}";
        }
    }

    private static string? ReadString(JsonNode? preference) => preference is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private void WriteDocument(JsonObject document)
    {
        string? temporaryFile = null;
        try
        {
            Directory.CreateDirectory(folder);
            temporaryFile = Path.Combine(folder, $".settings-{Guid.NewGuid():N}.tmp");

            using (var stream = new FileStream(temporaryFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                document.WriteTo(writer);
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(_filePath))
                File.Replace(temporaryFile, _filePath, null);
            else
                File.Move(temporaryFile, _filePath);
        }
        finally
        {
            if (temporaryFile is not null)
            {
                try
                {
                    File.Delete(temporaryFile);
                }
                catch (Exception ex) when (IsSettingsError(ex))
                {
                    ErrorReporting.Unexpected("removing temporary settings", ex);
                }
            }
        }
    }

    private JsonObject ReadDocument()
    {
        try
        {
            using var stream = File.OpenRead(_filePath);
            return JsonNode.Parse(stream) as JsonObject ?? throw new InvalidDataException("settings.json must contain a JSON object.");
        }
        catch (FileNotFoundException)
        {
            return new JsonObject();
        }
        catch (DirectoryNotFoundException)
        {
            return new JsonObject();
        }
    }

    private static CaptureShortcut? ReadShortcut(JsonObject document)
    {
        if (!document.TryGetPropertyValue("captureShortcut", out var preference))
            return CaptureShortcut.Default;

        if (preference is null)
            return null;

        if (
            preference is not JsonObject chord
            || chord["key"] is not JsonValue key
            || !key.TryGetValue<string>(out var keyName)
            || chord["control"] is not JsonValue control
            || !control.TryGetValue<bool>(out var useControl)
            || chord["alt"] is not JsonValue alt
            || !alt.TryGetValue<bool>(out var useAlt)
            || chord["shift"] is not JsonValue shift
            || !shift.TryGetValue<bool>(out var useShift)
        )
            throw new InvalidDataException("The captureShortcut preference in settings.json is invalid.");

        var shortcut = new CaptureShortcut(keyName, useControl, useAlt, useShift);

        return shortcut.IsValid ? shortcut : throw new InvalidDataException("The saved capture shortcut is unsupported.");
    }

    private static bool IsSettingsError(Exception ex) =>
        ex
            is IOException
                or InvalidDataException
                or UnauthorizedAccessException
                or System.Security.SecurityException
                or JsonException
                or ArgumentException;
}
