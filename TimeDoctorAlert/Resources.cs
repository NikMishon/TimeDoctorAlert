using System.Reflection;

namespace TimeDoctorAlert;

public static class Resources
{
    private const string Mp3ResourceName = "TimeDoctorAlert.Resources.imperskij-marsh-8bit.mp3";
    private const string IconResourceName = "TimeDoctorAlert.Resources.logo.ico";

    private static byte[]? _cachedMp3Bytes;

    public static byte[] GetMp3Bytes()
    {
        if (_cachedMp3Bytes != null)
            return _cachedMp3Bytes;

        using var stream = GetMp3Stream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _cachedMp3Bytes = ms.ToArray();
        return _cachedMp3Bytes;
    }

    public static Stream GetMp3Stream()
    {
        return GetEmbeddedResource(Mp3ResourceName);
    }

    public static Stream GetIconStream()
    {
        return GetEmbeddedResource(IconResourceName);
    }

    private static Stream GetEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(name);
        if (stream == null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"Embedded resource '{name}' not found. Available: {available}");
        }
        return stream;
    }
}
