using NAudio.Wave;

namespace TimeDoctorAlert.Platform.Windows;

public class WindowsAudioPlayer : IAudioPlayer
{
    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        var mp3Data = Resources.GetMp3Bytes();

        using var stream = new MemoryStream(mp3Data);
        using var reader = new Mp3FileReader(stream);
        using var waveOut = new WaveOutEvent();

        waveOut.Init(reader);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                reader.Position = 0;
                waveOut.Play();

                while (waveOut.PlaybackState == PlaybackState.Playing && !cancellationToken.IsCancellationRequested)
                    await Task.Delay(100, cancellationToken);
            }
        }
        finally
        {
            waveOut.Stop();
        }
    }

    public void Dispose() { }
}
