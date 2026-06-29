using Microsoft.JSInterop;

namespace URMS.Blazor.Services;

/// <summary>
/// Wraps JavaScript interop calls used throughout the Blazor app.
/// Inject this service where you need file downloads or polling.
/// </summary>
public class JsInteropService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<JsInteropService>? _selfRef;

    // Fires when the JS polling timer fires — subscribe in MainLayout
    public event Func<Task>? OnPollNotifications;

    public JsInteropService(IJSRuntime js) => _js = js;

    // ── File download ─────────────────────────────────────────────────────────

    /// <summary>Triggers a browser download for any byte array.</summary>
    public async Task DownloadFileAsync(byte[] bytes, string fileName,
        string contentType = "application/octet-stream")
    {
        var base64 = Convert.ToBase64String(bytes);
        await _js.InvokeVoidAsync("downloadFile", fileName, contentType, base64);
    }

    public Task DownloadPdfAsync(byte[] bytes, string fileName) =>
        DownloadFileAsync(bytes, fileName, "application/pdf");

    public Task DownloadZipAsync(byte[] bytes, string fileName) =>
        DownloadFileAsync(bytes, fileName, "application/zip");

    // ── Notification polling ──────────────────────────────────────────────────

    /// <summary>Starts a JS interval that calls PollNotifications every intervalMs.</summary>
    public async Task StartPollingAsync(int intervalMs = 45_000)
    {
        _selfRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("startNotifPolling", _selfRef, intervalMs);
    }

    public async Task StopPollingAsync() =>
        await _js.InvokeVoidAsync("stopNotifPolling");

    /// <summary>Called by JS timer — raises the C# event so MainLayout refreshes.</summary>
    [JSInvokable]
    public async Task PollNotifications()
    {
        if (OnPollNotifications is not null)
            await OnPollNotifications.Invoke();
    }

    // ── Misc ──────────────────────────────────────────────────────────────────

    public async Task ScrollToTopAsync() =>
        await _js.InvokeVoidAsync("scrollToTop");

    public async ValueTask DisposeAsync()
    {
        await StopPollingAsync();
        _selfRef?.Dispose();
    }
}
