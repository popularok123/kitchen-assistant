namespace KitchenAssistant.Web.Services;

public class AppState
{
    private readonly LocalStorageService _storage;
    private string? _userName;
    private string? _avatar;

    public string UserName
    {
        get => _userName ?? "";
        set
        {
            _userName = value;
            _ = SaveToStorageAsync();
            NotifyStateChanged();
        }
    }

    public string Avatar
    {
        get => _avatar ?? DefaultAvatars[0];
        set
        {
            _avatar = value;
            _ = SaveToStorageAsync();
            NotifyStateChanged();
        }
    }

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(_userName);

    public event Action? OnChange;

    public static readonly string[] DefaultAvatars = {
        "👨‍🍳", "👩‍🍳", "🧑‍🍳", "👨‍🍳", "👩‍🍳",
        "🍳", "🥘", "🍜", "🍲", "🥗"
    };

    public AppState(LocalStorageService storage)
    {
        _storage = storage;
    }

    public async Task LoadFromStorageAsync()
    {
        _userName = await _storage.GetItemAsync("userName");
        _avatar = await _storage.GetItemAsync("userAvatar");
    }

    private async Task SaveToStorageAsync()
    {
        if (!string.IsNullOrEmpty(_userName))
        {
            await _storage.SetItemAsync("userName", _userName);
        }
        if (!string.IsNullOrEmpty(_avatar))
        {
            await _storage.SetItemAsync("userAvatar", _avatar);
        }
    }

    public void Logout()
    {
        _userName = null;
        _avatar = null;
        _ = _storage.RemoveItemAsync("userName");
        _ = _storage.RemoveItemAsync("userAvatar");
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
