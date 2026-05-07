namespace KitchenAssistant.Core.Services;

public interface ITextToSpeechService
{
    Task SpeakAsync(string text, CancellationToken cancellationToken = default);
    void Stop();
    bool IsSpeaking { get; }
}
