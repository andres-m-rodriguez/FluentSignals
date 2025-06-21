using Microsoft.AspNetCore.Components;

namespace FluentSignals.Web.Shared.Features.Status.Actions;

public partial class RedirectAction(NavigationManager navigationManager) : ComponentBase
{
    [Parameter, EditorRequired]
    public string Url { get; set; } = default!;
    protected override void OnInitialized() => navigationManager.NavigateTo(Url);
}
