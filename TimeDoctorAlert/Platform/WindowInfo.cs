namespace TimeDoctorAlert.Platform;

public class WindowInfo
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required bool IsForeground { get; init; }

    public override string ToString() =>
        $"{Title} [{ProcessName}] {Width}x{Height} Foreground={IsForeground}";
}
