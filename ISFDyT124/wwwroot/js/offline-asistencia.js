// Cola offline de asistencia (frente 7 / PWA). Ver documentacion/Gustavo-EDT-offline-pwa.md
// para el plan completo. Depende de Dexie.js (wwwroot/lib/dexie).
//
// Funciona en silencio, sin banners ni botones: si no hay señal al guardar, se encola
// acá; apenas el dispositivo recupera conexión, se sincroniza sola.
(function () {
    if (typeof Dexie === 'undefined') return; // Si no cargó la librería, no rompemos el resto de la página.

    var db = new Dexie('is124-asistencia-offline');
    db.version(1).stores({
        // ++id: clave autogenerada local, solo para Dexie. clientGuid es la clave real
        // que el servidor usa para deduplicar. enviado: 0 = pendiente, 1 = ya sincronizado.
        pendientes: '++id, clientGuid, enviado, maId, fecha',
    });

    function getAntiforgeryToken() {
        var meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.content : null;
    }

    function generarGuid() {
        if (window.crypto && crypto.randomUUID) return crypto.randomUUID();
        // Respaldo simple para navegadores viejos sin crypto.randomUUID.
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = (Math.random() * 16) | 0;
            var v = c === 'x' ? r : (r & 0x3) | 0x8;
            return v.toString(16);
        });
    }

    /// Encola en IndexedDB una tanda de asistencia (todo el formulario de una cátedra/fecha).
    /// filas: [{ usId, presente, justificacion }, ...]
    async function encolarAsistencia(maId, fecha, filas) {
        var ahora = new Date().toISOString();
        var registros = filas.map(function (fila) {
            return {
                clientGuid: generarGuid(),
                maId: maId,
                fecha: fecha,
                fechaCarga: ahora,
                usId: fila.usId,
                presente: fila.presente,
                justificacion: fila.justificacion,
                enviado: 0,
            };
        });
        await db.pendientes.bulkAdd(registros);
        return registros.length;
    }

    /// Manda al servidor lo que esté pendiente, sin avisar nada en pantalla. Si falla (sigue
    /// sin señal, o la sesión venció mientras tanto), la cola queda intacta y se reintenta
    /// solo en el próximo evento 'online' o la próxima vez que se abra la app — no se pierde
    /// nada, cada fila se marca enviada únicamente si el servidor confirmó haberla guardado.
    async function sincronizarPendientes() {
        var pendientes = await db.pendientes.where('enviado').equals(0).toArray();
        if (pendientes.length === 0) return;

        var token = getAntiforgeryToken();
        var body = pendientes.map(function (p) {
            return {
                clientGuid: p.clientGuid,
                usId: p.usId,
                maId: p.maId,
                fecha: p.fecha,
                fechaCarga: p.fechaCarga,
                presente: p.presente,
                justificacion: p.justificacion,
            };
        });

        var respuesta;
        try {
            respuesta = await fetch('/api/asistencias/sincronizar', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token || '',
                },
                body: JSON.stringify(body),
            });
        } catch (e) {
            return; // Sin conexión todavía — se reintenta después.
        }

        if (!respuesta.ok) return; // 401 u otro error: se deja todo en la cola, se reintenta después.

        var resultados = await respuesta.json();
        for (var i = 0; i < resultados.length; i++) {
            if (resultados[i].ok) {
                await db.pendientes.where('clientGuid').equals(resultados[i].clientGuid).modify({ enviado: 1 });
            }
        }
        // Limpieza: no tiene sentido acumular filas ya enviadas para siempre en el dispositivo.
        await db.pendientes.where('enviado').equals(1).delete();
    }

    window.OfflineAsistencia = {
        encolarAsistencia: encolarAsistencia,
    };

    // Sincronización automática al recuperar señal + al cargar la página (por si ya había
    // señal pero el evento 'online' nunca llegó a disparar). Todo en silencio.
    window.addEventListener('online', sincronizarPendientes);
    document.addEventListener('DOMContentLoaded', function () {
        if (navigator.onLine) sincronizarPendientes();
    });
})();
