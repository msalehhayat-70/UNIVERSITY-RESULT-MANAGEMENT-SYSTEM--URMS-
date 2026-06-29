// Published service worker
self.importScripts('./service-worker-assets.js');
self.addEventListener('install', e => e.waitUntil(onInstall(e)));
self.addEventListener('activate', e => e.waitUntil(onActivate(e)));
self.addEventListener('fetch', e => e.respondWith(onFetch(e)));
const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
async function onInstall(e) { await caches.open(cacheName); }
async function onActivate(e) {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys.filter(k => k.startsWith(cacheNamePrefix) && k !== cacheName).map(k => caches.delete(k)));
}
async function onFetch(e) {
    if (e.request.method !== 'GET') return;
    const shouldServeIndexHtml = e.request.mode === 'navigate';
    const request = shouldServeIndexHtml ? 'index.html' : e.request;
    const cachedResponse = await caches.match(request);
    return cachedResponse || fetch(request);
}
