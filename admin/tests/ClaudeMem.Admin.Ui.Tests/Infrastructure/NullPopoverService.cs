using System.Collections.Generic;
using System.Threading.Tasks;
using MudBlazor;

namespace ClaudeMem.Admin.Ui.Tests.Infrastructure;

/// <summary>A no-op popover service for unit testing — avoids the MudPopoverProvider requirement.</summary>
internal sealed class NullPopoverService : IPopoverService
{
    public PopoverOptions PopoverOptions { get; } = new();

    public IEnumerable<IMudPopoverHolder> ActivePopovers { get; } = [];

    public bool IsInitialized
    {
        get { return true; }
    }

    public void Subscribe(IPopoverObserver observer) { }

    public void Unsubscribe(IPopoverObserver observer) { }

    public Task CreatePopoverAsync(IPopover popover)
    {
        return Task.CompletedTask;
    }

    public Task<bool> UpdatePopoverAsync(IPopover popover)
    {
        return Task.FromResult(true);
    }

    public Task<bool> DestroyPopoverAsync(IPopover popover)
    {
        return Task.FromResult(true);
    }

    public ValueTask<int> GetProviderCountAsync()
    {
        return ValueTask.FromResult(1);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
