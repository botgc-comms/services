self.addEventListener("install", (event) => {
    self.skipWaiting();
});

self.addEventListener("activate", (event) => {
    event.waitUntil(clients.claim());
});

self.addEventListener("push", (event) => {
    const data = event.data ? event.data.json() : {};
    const title = data.title || "Stock take alert";
    const body = data.body || "A stock take is due.";
    const url = data.url || "/";
    event.waitUntil(
        registration.showNotification(title, {
            body: body,
            data: { url: url },
            badge: "/icons/icon-192.png",
            icon: "/icons/icon-192.png"
        })
    );
});

self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    const url = event.notification.data && event.notification.data.url ? event.notification.data.url : "/";
    event.waitUntil(
        clients.matchAll({ type: "window", includeUncontrolled: true }).then((clientList) => {
            for (const client of clientList) {
                if (client.url.includes(self.registration.scope) && "focus" in client) {
                    client.postMessage({ kind: "navigate", url: url });
                    return client.focus();
                }
            }
            return clients.openWindow(url);
        })
    );
});
