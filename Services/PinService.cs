using Microsoft.JSInterop;
using System.Security.Cryptography;
using System.Text;

public class PinService
{
    private readonly IJSRuntime _js;

    public PinService(IJSRuntime js)
    {
        _js = js;
    }

    private const string PinKey = "APP_PIN";
    private const string UnlockKey = "APP_UNLOCKED";

    public async Task<bool> HasPinAsync()
        => !string.IsNullOrEmpty(await _js.InvokeAsync<string>("localStorage.getItem", PinKey));

    public async Task SetPinAsync(string pin)
    {
        var hash = Hash(pin);
        await _js.InvokeVoidAsync("localStorage.setItem", PinKey, hash);
    }

    public async Task<bool> ValidatePinAsync(string pin)
    {
        var saved = await _js.InvokeAsync<string>("localStorage.getItem", PinKey);
        if (saved == null) return false;

        if (saved == Hash(pin))
        {
            await _js.InvokeVoidAsync("localStorage.setItem", UnlockKey, "true");
            return true;
        }
        return false;
    }

    public async Task<bool> IsUnlockedAsync()
        => await _js.InvokeAsync<string>("localStorage.getItem", UnlockKey) == "true";

    public async Task LockAsync()
        => await _js.InvokeVoidAsync("localStorage.removeItem", UnlockKey);

    public async Task RemovePinAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", PinKey);
        await LockAsync();
    }

    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}
