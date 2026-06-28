using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using ClaudeMem.Admin.Ui.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClaudeMem.Admin.Ui.Tests.Unit;

public sealed class CacheInvalidationBusTests
{
    private static CacheInvalidationBus CreateBus()
    {
        return new CacheInvalidationBus(NullLogger<CacheInvalidationBus>.Instance);
    }

    [Fact]
    public async Task Notify_NoSubscribers_CompletesSuccessfully()
    {
        var bus = CreateBus();

        await bus.Notify("teams:all", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Notify_SingleSubscriber_InvokesHandlerWithKey()
    {
        var bus = CreateBus();
        string? receivedKey = null;
        bus.Subscribe(key =>
        {
            receivedKey = key;
            return Task.CompletedTask;
        });

        await bus.Notify("teams:all", TestContext.Current.CancellationToken);

        receivedKey.Should().Be("teams:all");
    }

    [Fact]
    public async Task Notify_MultipleSubscribers_AllHandlersInvoked()
    {
        var bus = CreateBus();
        var received = new List<string>();

        bus.Subscribe(key => { received.Add("handler-1:" + key); return Task.CompletedTask; });
        bus.Subscribe(key => { received.Add("handler-2:" + key); return Task.CompletedTask; });

        await bus.Notify("teams:all", TestContext.Current.CancellationToken);

        received.Should().HaveCount(2);
        received.Should().Contain("handler-1:teams:all");
        received.Should().Contain("handler-2:teams:all");
    }

    [Fact]
    public async Task Notify_AfterUnsubscribe_HandlerNotInvoked()
    {
        var bus = CreateBus();
        var invoked = false;
        Func<string, Task> handler = _ => { invoked = true; return Task.CompletedTask; };

        bus.Subscribe(handler);
        bus.Unsubscribe(handler);
        await bus.Notify("teams:all", TestContext.Current.CancellationToken);

        invoked.Should().BeFalse();
    }

    [Fact]
    public async Task Notify_HandlerThrowsSynchronously_OtherHandlersStillInvoked()
    {
        var bus = CreateBus();
        var secondHandlerInvoked = false;

        bus.Subscribe(_ => throw new InvalidOperationException("bad handler"));
        bus.Subscribe(_ => { secondHandlerInvoked = true; return Task.CompletedTask; });

        await bus.Notify("teams:all", TestContext.Current.CancellationToken);

        secondHandlerInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task Notify_HandlerReturnsFailedTask_OtherHandlersStillInvoked()
    {
        var bus = CreateBus();
        var secondHandlerInvoked = false;

        bus.Subscribe(_ => Task.FromException(new InvalidOperationException("async fault")));
        bus.Subscribe(_ => { secondHandlerInvoked = true; return Task.CompletedTask; });

        await bus.Notify("teams:all", TestContext.Current.CancellationToken);

        secondHandlerInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task Notify_CancelledToken_ThrowsOperationCancelledException()
    {
        var bus = CreateBus();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => bus.Notify("teams:all", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Subscribe_SameHandlerTwice_InvokedTwice()
    {
        var bus = CreateBus();
        var count = 0;
        Func<string, Task> handler = _ => { count++; return Task.CompletedTask; };

        bus.Subscribe(handler);
        bus.Subscribe(handler);
        await bus.Notify("teams:all", TestContext.Current.CancellationToken);

        count.Should().Be(2);
    }

    [Fact]
    public async Task Unsubscribe_OneOfTwoIdenticalHandlers_RemovesOnlyOne()
    {
        var bus = CreateBus();
        var count = 0;
        Func<string, Task> handler = _ => { count++; return Task.CompletedTask; };

        bus.Subscribe(handler);
        bus.Subscribe(handler);
        bus.Unsubscribe(handler);
        await bus.Notify("teams:all", TestContext.Current.CancellationToken);

        count.Should().Be(1);
    }
}
