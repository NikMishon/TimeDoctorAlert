using Serilog;
using TimeDoctorAlert.Platform;

namespace TimeDoctorAlert;

public class WindowMonitorService
{
    private readonly WindowTracker _tracker;
    private readonly IAudioPlayer _audioPlayer;
    private readonly Func<WindowInfo, bool> _filter;
    private int _previousCount;

    public WindowMonitorService(WindowTracker tracker, IAudioPlayer audioPlayer, Func<WindowInfo, bool> filter)
    {
        _tracker = tracker;
        _audioPlayer = audioPlayer;
        _filter = filter;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var count = _tracker.UpdateWindowList(_filter);

                if (count > _previousCount)
                    await CheckActivityAndPlaySound(cancellationToken);

                if (_previousCount != count)
                {
                    Log.Information("Windows count changed: {Previous} -> {Current}", _previousCount, count);
                    _previousCount = count;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                Log.Error(e, "MonitorWindow: Error");
            }

            try
            {
                await Task.Delay(500, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CheckActivityAndPlaySound(CancellationToken cancellationToken)
    {
        Log.Information("CheckActivityAndPlaySound start");

        using var playCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, playCts.Token);

        var playTask = _audioPlayer.PlayAsync(linkedCts.Token);

        try
        {
            var start = DateTime.Now;
            while (true)
            {
                if (_tracker.UpdateWindowList(_filter) <= _previousCount)
                {
                    Log.Information("Time Doctor window closed");
                    break;
                }

                if (DateTime.Now - start > TimeSpan.FromMinutes(1))
                {
                    Log.Information("Timeout reached (1 minute)");
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(200, cancellationToken);
            }
        }
        finally
        {
            playCts.Cancel();
            try
            {
                await playTask;
            }
            catch (OperationCanceledException)
            {
                Log.Information("Audio playback stopped");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Audio playback error");
            }
        }
    }
}
