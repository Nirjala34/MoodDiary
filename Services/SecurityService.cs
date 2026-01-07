namespace JournalApp.Services;

public class SecurityService
{
    private bool unlocked = true; // default to unlocked

    // Check if a PIN exists (for demo, always false)
    public bool HasPin() => false;

    // Check if the app is unlocked
    public bool IsUnlocked() => unlocked;

    // Unlock method (optional)
    public void Unlock() => unlocked = true;

    // Lock method (optional)
    public void Lock() => unlocked = false;
}
