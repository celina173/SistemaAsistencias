const CACHE_VERSION = 'is124-v6';
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
    '/js/pwa-core.js',
    '/js/pwa-asistencia.js',
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

// Precarga de las páginas reales del docente (lista de cátedras, planilla y asistencia global de cada una).
// Se baja todo primero y recién entonces se reemplaza lo guardado: una precarga cortada no destruye lo anterior.
let precargando = false;

// Una respuesta que pasó por una redirección no se puede servir a una navegación: se copia limpia.
async function copiaLimpia(r) {
    const h = new Headers(r.headers);
    h.set('x-guardada-en', String(Date.now()));
    return new Response(await r.blob(), { status: r.status, statusText: r.statusText, headers: h });
}

async function precargarPaginasDocente() {
    if (precargando) return;
    precargando = true;
    try {
        const lista = await fetch('/Asistencias', { credentials: 'same-origin' });
        if (!lista.ok || /\/Account\/Login/i.test(lista.url)) return; // sin sesión: no se guarda nada
        const html = await lista.clone().text();
        // La lista no trae enlaces con CaMaId: es un selector de carrera y materia. Cada combinación válida
        // redirige a la planilla de esa cátedra (y con _global=1, a su asistencia global).
        const valores = (nombre) => {
            const m = new RegExp('<select[^>]*name="' + nombre + '"[^]*?</select>', 'i').exec(html);
            return m ? [...m[0].matchAll(/<option[^>]*value="(\d+)"/gi)].map((x) => x[1]) : [];
        };
        const carreras = valores('SelectedCarreraId');
        const materias = valores('SelectedMateriaId');
        const bajadas = [['/Asistencias', await copiaLimpia(lista)]];
        let probadas = 0;
        for (const c of carreras) {
            for (const m of materias) {
                if (++probadas > 200) break;
                const q = `?selectedCarreraId=${c}&selectedMateriaId=${m}`;
                const r = await fetch('/Asistencias/Index' + q, { credentials: 'same-origin' });
                if (!r.redirected || !r.ok || !/CaMaId=\d+/i.test(r.url)) continue; // combinación sin cátedra
                const plan = new URL(r.url);
                bajadas.push([plan.pathname + plan.search, await copiaLimpia(r)]);
                const g = await fetch('/Asistencias/Index' + q + '&_global=1', { credentials: 'same-origin' });
                if (g.redirected && g.ok && /CaMaId=\d+/i.test(g.url)) {
                    const glo = new URL(g.url);
                    bajadas.push([glo.pathname + glo.search, await copiaLimpia(g)]);
                }
            }
        }
        // Reemplazo atómico: recién con todo bajado se escribe lo guardado.
        const cache = await caches.open(PAGES_CACHE);
        for (const [u, r] of bajadas) await cache.put(u, r);
    } catch (e) {
        // sin señal o cortada: lo guardado antes queda intacto
    } finally {
        precargando = false;
    }
}

self.addEventListener('message', (event) => {
    if (event.data && event.data.tipo === 'precargar') event.waitUntil(precargarPaginasDocente());
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
