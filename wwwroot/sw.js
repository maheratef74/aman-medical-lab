/* معمل أمان — Service Worker */
const CACHE_NAME = 'amanlab-v3';
const CORE_ASSETS = [
    '/',
    '/manifest.webmanifest',
    '/admin-manifest.webmanifest',
    '/css/amanlab.css',
    '/images/aman-logo.png',
    '/images/app-icon.jpeg',
    '/icons/icon-192.png',
    '/icons/icon-512.png'
];

self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(function (cache) { return cache.addAll(CORE_ASSETS); })
            .then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys()
            .then(function (keys) {
                return Promise.all(
                    keys.filter(function (key) { return key !== CACHE_NAME; })
                        .map(function (key) { return caches.delete(key); })
                );
            })
            .then(function () { return self.clients.claim(); })
    );
});

self.addEventListener('fetch', function (event) {
    if (event.request.method !== 'GET') return;

    var url = new URL(event.request.url);
    if (url.origin !== self.location.origin) return;

    // Navigations: network-first, fall back to cached home page when offline
    if (event.request.mode === 'navigate') {
        event.respondWith(
            fetch(event.request)
                .then(function (response) {
                    var copy = response.clone();
                    caches.open(CACHE_NAME).then(function (cache) { cache.put('/', copy); });
                    return response;
                })
                .catch(function () { return caches.match('/'); })
        );
        return;
    }

    // Static assets: cache-first, populate cache on miss
    event.respondWith(
        caches.match(event.request)
            .then(function (cached) {
                if (cached) return cached;
                return fetch(event.request).then(function (response) {
                    if (response && response.status === 200 && response.type === 'basic') {
                        var copy = response.clone();
                        caches.open(CACHE_NAME).then(function (cache) { cache.put(event.request, copy); });
                    }
                    return response;
                });
            })
    );
});