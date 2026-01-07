public class ThemeService
{
    public event Action? OnThemeChanged;
    private bool _isDark = false;

    public bool IsDark => _isDark;

    public void ToggleTheme()
    {
        _isDark = !_isDark;
        OnThemeChanged?.Invoke();
    }
}
