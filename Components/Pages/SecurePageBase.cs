using Microsoft.AspNetCore.Components;

public class SecurePageBase : ComponentBase
{
    [Inject] protected PinService PinService { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (await PinService.HasPinAsync() && !await PinService.IsUnlockedAsync())
        {
            Nav.NavigateTo("/unlock", true);
        }
    }
}
