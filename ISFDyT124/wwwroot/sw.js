const CACHE_VERSION = 'is124-v4';
const STATIC_CACHE = `${CACHE_VERSION}-static`;
// A propósito NO atada a CACHE_VERSION: acá se va acumulando cada pantalla real
// que el docente visitó con señal. Si la atamos a la versión, cada vez que
// subimos un cambio de CSS/JS (que solo debería refrescar lo estático) se
// borraba junto con eso todo el historial de páginas ya guardado, dejando al
// docente sin nada para ver la próxima vez que abriera la app sin conexión.
const PAGES_CACHE = 'is124-pages';

const APP_SHELL = [
    '/css/style.css',
    '/js/script.js',
    '/lib/jquery/dist/jquery.min.js',
    '/images/logo.png',
    '/images/icons/icon-192.png',
    '/images/icons/icon-512.png',
    '/manifest.json',
    '/offline.html',
    // Pantalla pública (no requiere sesión) — se precachea para que abrir la
    // app sin señal, incluso en el primerísimo arranque, muestre el login de
    // verdad en vez del cartel genérico de "sin conexión".
    '/Account/Login',
    // Frente 7 (PWA offline): la cola de asistencia necesita Dexie disponible
    // localmente aunque la primera carga de la pantalla haya sido sin señal.
    '/lib/dexie/dist/dexie.min.js',
    '/js/offline-asistencia.js',
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(STATIC_CACHE)
            // Uno por uno en vez de addAll: addAll rechaza TODO si falla un solo archivo, y
            // entonces la instalación fracasa y el Service Worker nunca se activa — la app
            // queda sin ninguna capacidad offline por culpa de un archivo suelto.
            .then((cache) =>
                Promise.all(
                    APP_SHELL.map((recurso) => cache.add(recurso).catch(() => null))
                )
            )
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(
                keys
                    .filter((key) => key.startsWith('is124-') && key !== STATIC_CACHE && key !== PAGES_CACHE)
                    .map((key) => caches.delete(key))
            )
        ).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    const { request } = event;

    // Solo GET: los POST (guardar asistencia, login, altas/bajas) siempre van directo a la red.
    if (request.method !== 'GET') return;

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;

    // Navegación (las páginas .cshtml renderizadas): red primero, con la última copia
    // vista como respaldo si no hay conexión, y una página de "sin conexión" si tampoco hay copia.
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request)
                .then((response) => {
                    // Solo se guardan respuestas OK y NO redirigidas: cache.put tira TypeError
                    // con una respuesta redirigida (ej. el 302 al login cuando venció la sesión),
                    // y sin este chequeo quedaba una promesa rechazada suelta y la pantalla
                    // nunca terminaba de guardarse.
                    if (response.ok && !response.redirected) {
                        const copy = response.clone();
                        // waitUntil: sin esto el navegador puede matar al Service Worker apenas
                        // devuelve la respuesta (pasa seguido en celulares) y el guardado queda
                        // a medias — por eso a veces parecía que una pantalla ya visitada no
                        // estaba cacheada.
                        event.waitUntil(
                            caches
                                .open(PAGES_CACHE)
                                .then((cache) => cache.put(request, copy))
                                .catch(() => null)
                        );
                    }
                    return response;
                })
                .catch(() => responderDesdeCache(request, url))
        );
        return;
    }

    // Estáticos (css/js/imágenes): cache primero, red como respaldo y actualización silenciosa.
    if (STATIC_CACHE_EXTENSIONS(url.pathname)) {
        event.respondWith(
            caches.match(request).then((cached) => {
                const network = fetch(request)
                    .then((response) => {
                        const copy = response.clone();
                        caches.open(STATIC_CACHE).then((cache) => cache.put(request, copy));
                        return response;
                    })
                    .catch(() => cached);
                return cached || network;
            })
        );
    }
});

function STATIC_CACHE_EXTENSIONS(pathname) {
    return /\.(css|js|png|jpg|jpeg|svg|ico|woff2?|ttf)$/i.test(pathname);
}

/// Busca la mejor copia guardada para una navegación que no llegó a la red.
/// SIEMPRE devuelve una Response de verdad: si devolviera undefined (que es lo que pasaba
/// cuando offline.html tampoco estaba en el cache), el navegador lo interpreta como error
/// de red y muestra SU propia pantalla de "sin conexión" en vez de la nuestra.
async function responderDesdeCache(request, url) {
    const exacta = await caches.match(request);
    if (exacta) return exacta;

    if (url.pathname === '/') {
        // El start_url de la PWA instalada (manifest.json) pide "/?source=pwa" — esa URL exacta
        // nunca se cachea sola si el docente siempre entró por "Inicio" del menú (que pide "/").
        const inicio = await caches.match('/', { ignoreSearch: true });
        if (inicio) return inicio;

        // Sin sesión activa la app manda al login: mostrar esa pantalla es más útil que un
        // cartel genérico, al menos se ve la app y no un error del navegador.
        const login = await caches.match('/Account/Login', { ignoreSearch: true });
        if (login) return login;
    }

    const offline = await caches.match('/offline.html');
    if (offline) return offline;

    return new Response(
        '<!DOCTYPE html><html lang="es"><head><meta charset="utf-8">' +
            '<meta name="viewport" content="width=device-width, initial-scale=1"><title>Sin conexión</title>' +
            '</head><body style="font-family:sans-serif;text-align:center;padding:40px;color:#002e8c">' +
            '<h1>Sin conexión</h1><p style="color:#64748b">Esta pantalla todavía no quedó guardada en el ' +
            'dispositivo. Abrila una vez con señal y va a estar disponible sin conexión.</p></body></html>',
        { status: 200, headers: { 'Content-Type': 'text/html; charset=utf-8' } }
    );
}
