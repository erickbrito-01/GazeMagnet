using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GazeMagnet;

internal sealed record AppSettings
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GazeMagnet", "settings.json");

    public bool? AutoConnection { get; init; }
    public int? ManualPort { get; init; }
    public string? ProtectedKey { get; init; }
    public int? Radius { get; init; }
    public int? FixationMs { get; init; }
    public int? Hysteresis { get; init; }
    public int? MaxTargetWidth { get; init; }
    public int? MaxTargetHeight { get; init; }
    public int? GazeStability { get; init; }
    public bool? Magnetic { get; init; }
    public bool? TargetsOnly { get; init; }
    public bool? Demo { get; init; }
    public bool? ShowSearchArea { get; init; }
    public bool? EnterClicksTarget { get; init; }
    public decimal? ClickTime { get; init; }
    public int? LastClickMode { get; init; }

    public static AppSettings Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(this));
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Trace.TraceWarning("Não foi possível salvar os ajustes do GazeMagnet: {0}", error.Message);
        }
    }

    public string ReadManualKey()
    {
        if (ProtectedKey == null)
            return GazePointerPort.DemoKey;
        if (ProtectedKey.Length == 0)
            return "";

        try
        {
            byte[] encrypted = Convert.FromBase64String(ProtectedKey);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser));
        }
        catch (Exception error) when (error is CryptographicException or FormatException)
        {
            return "";
        }
    }

    public static string? ProtectManualKey(string key)
    {
        if (key == GazePointerPort.DemoKey)
            return null;
        if (key.Length == 0)
            return "";

        try
        {
            byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch (CryptographicException)
        {
            return "";
        }
    }
}
