// URMS JavaScript helpers
// Placed in wwwroot/js/urms.js
// Referenced in wwwroot/index.html

// ── File download trigger ────────────────────────────────────────────────────
window.downloadFile = function (fileName, contentType, base64Data) {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64Data}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// ── Notification polling ─────────────────────────────────────────────────────
let notifTimer = null;

window.startNotifPolling = function (dotnetRef, intervalMs) {
    if (notifTimer) clearInterval(notifTimer);
    notifTimer = setInterval(async () => {
        try {
            await dotnetRef.invokeMethodAsync('PollNotifications');
        } catch (e) {
            // Component may have been disposed — stop polling
            clearInterval(notifTimer);
        }
    }, intervalMs);
};

window.stopNotifPolling = function () {
    if (notifTimer) {
        clearInterval(notifTimer);
        notifTimer = null;
    }
};

// ── Scroll to top ─────────────────────────────────────────────────────────────
window.scrollToTop = function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

// ── Auto-dismiss alerts ───────────────────────────────────────────────────────
window.autoDismiss = function (elementId, ms) {
    setTimeout(() => {
        const el = document.getElementById(elementId);
        if (el) el.style.display = 'none';
    }, ms);
};
