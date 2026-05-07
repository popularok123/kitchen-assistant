using System.Diagnostics;
using KitchenAssistant.Core.Services;

namespace KitchenAssistant.Client.Services;

public class MacTextToSpeechService : ITextToSpeechService, IDisposable
{
    private Process? _speechProcess;
    private bool _isDisposed;

    public bool IsSpeaking => _speechProcess != null && !_speechProcess.HasExited;

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        Stop();

        _speechProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "say",
                Arguments = $"\"{text}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _speechProcess.Start();
        await _speechProcess.WaitForExitAsync(cancellationToken);
    }

    public void Stop()
    {
        if (_speechProcess != null && !_speechProcess.HasExited)
        {
            try
            {
                _speechProcess.Kill();
            }
            catch
            {
            }
        }
        _speechProcess = null;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Stop();
        _isDisposed = true;
    }
}
