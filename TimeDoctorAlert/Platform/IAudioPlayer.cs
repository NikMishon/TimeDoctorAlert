namespace TimeDoctorAlert.Platform;

public interface IAudioPlayer : IDisposable
{
    Task PlayAsync(CancellationToken cancellationToken);
}
