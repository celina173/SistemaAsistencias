// Captura y reenvío de la asistencia del docente (ver pwa-core.js y design.md D3/D4).
// Intercepta el envío del formulario REAL de asistencia, lo guarda en una bandeja local y lo reenvía al
// servidor con una credencial antifalsificación fresca. Si no reconoce el formulario, no intercepta.
(function () {
    'use strict';
    if (!window.PwaCore) return;
    var P = window.PwaCore;
    var CLAVE = 'is124-bandeja-v1';
    var enCurso = false;
    var temporizador = null;

    function leer() { try { return JSON.parse(localStorage.getItem(CLAVE) || '[]'); } catch (e) { return []; } }
    function guardar(b) { try { localStorage.setItem(CLAVE, JSON.stringify(b)); } catch (e) { /* sin almacenamiento */ } mostrar(); }

    function camposDe(form) {
        var pares = [];
        new FormData(form).forEach(function (v, k) { if (k !== '__RequestVerificationToken' && typeof v === 'string') pares.push([k, v]); });
        return pares;
    }

    // ---- Indicador de pendientes y rechazos ----
    function mostrar() {
        var b = leer(), pend = P.pendientes(b), rech = P.rechazadas(b);
        var el = document.getElementById('pwaBandeja');
        if (!pend.length && !rech.length) { if (el) el.remove(); return; }
        if (!el) {
            el = document.createElement('div');
            el.id = 'pwaBandeja';
            el.setAttribute('role', 'status');
            el.style.cssText = 'position:fixed;left:8px;right:8px;bottom:70px;z-index:9999;background:#fff;border:1px solid #cbd5e1;' +
                'border-radius:8px;padding:10px 12px;font:14px sans-serif;color:#1e293b;box-shadow:0 2px 8px rgba(0,0,0,.2)';
            document.body.appendChild(el);
        }
        el.textContent = '';
        if (pend.length) {
            var p = document.createElement('div');
            p.textContent = pend.length + (pend.length === 1 ? ' carga pendiente de enviar' : ' cargas pendientes de enviar') +
                (navigator.onLine ? ' (esperando al servidor)' : ' (sin conexión)');
            el.appendChild(p);
        }
        rech.forEach(function (e) {
            var d = document.createElement('div');
            d.style.cssText = 'color:#b91c1c;margin-top:6px';
            var fecha = (e.campos.filter(function (c) { return c[0] === 'FechaAsistencia'; })[0] || [0, ''])[1];
            d.textContent = 'Rechazada ' + fecha + ': ' + e.motivo + ' ';
            var x = document.createElement('button');
            x.type = 'button'; x.textContent = 'Descartar';
            x.onclick = function () { guardar(P.descartar(leer(), e.clave)); };
            d.appendChild(x);
            el.appendChild(d);
        });
    }

    // ---- Reenvío ----
    function pedir(url, opciones) {
        var ctl = new AbortController();
        var t = setTimeout(function () { ctl.abort(); }, P.TIEMPO_PEDIDO_MS);
        opciones = Object.assign({ credentials: 'same-origin', signal: ctl.signal }, opciones);
        return fetch(url, opciones).finally(function () { clearTimeout(t); });
    }

    function enviar(entrada) {
        var urlPlanilla = entrada.action + '?CaMaId=' + encodeURIComponent((entrada.campos.filter(function (c) { return c[0] === 'CaMaId'; })[0] || [0, ''])[1]);
        return pedir(urlPlanilla).then(function (r) {
            if (/\/Account\/Login/i.test(r.url)) return { status: 401, url: r.url };
            return r.text().then(function (html) {
                var m = /name="__RequestVerificationToken"[^>]*value="([^"]+)"/.exec(html);
                if (!m) return { status: 200, url: r.url, html: '<div id="operationError"><p>La pantalla cambió y no se pudo reenviar la carga.</p></div>' };
                var cuerpo = new URLSearchParams();
                entrada.campos.forEach(function (c) { cuerpo.append(c[0], c[1]); });
                cuerpo.append('__RequestVerificationToken', m[1]);
                return pedir(entrada.action + new URL(r.url).search, { method: 'POST', body: cuerpo })
                    .then(function (r2) { return r2.text().then(function (h) { return { status: r2.status, url: r2.url, html: h }; }); });
            });
        }).catch(function () { return { error: true }; });
    }

    function ciclo() {
        if (enCurso) return;
        var b = leer(), siguiente = P.pendientes(b)[0];
        if (!siguiente) { mostrar(); return; }
        enCurso = true;
        enviar(siguiente).then(function (resp) {
            var res = P.interpretarRespuesta(resp);
            if (res.estado === 'sesionVencida') { enCurso = false; mostrar(); return; } // se conserva la bandeja
            var nueva = P.aplicarResultado(leer(), siguiente.clave, res);
            guardar(nueva.filter(function (e) { return e.estado !== P.ESTADOS.SINCRONIZADO; }));
            enCurso = false;
            if (res.estado === 'reintentable') programar(P.esperaMs(siguiente.intentos));
            else ciclo();
        });
    }

    function programar(ms) {
        clearTimeout(temporizador);
        temporizador = setTimeout(ciclo, ms);
    }

    // ---- Captura ----
    document.addEventListener('submit', function (ev) {
        var form = ev.target;
        if (!form || form.tagName !== 'FORM' || (form.method || '').toLowerCase() !== 'post') return;
        var action = form.getAttribute('action') || '';
        var campos = camposDe(form);
        if (!P.formularioReconocido(action, campos)) return; // no se reconoce: el formulario sigue su camino normal
        ev.preventDefault();
        guardar(P.agregarEntrada(leer(), action.split('?')[0], campos));
        var aviso = document.createElement('div');
        aviso.textContent = 'Asistencia guardada. Se envía al servidor en cuanto haya conexión.';
        aviso.style.cssText = 'position:fixed;top:10px;left:10px;right:10px;z-index:9999;background:#dcfce7;color:#166534;padding:10px;border-radius:8px;font:14px sans-serif';
        document.body.appendChild(aviso);
        setTimeout(function () { aviso.remove(); }, 4000);
        ciclo();
    }, true);

    // ---- Cierre de sesión: envía primero; si quedan cargas, advierte y borra todo al confirmar ----
    var salir = document.getElementById('formLogout');
    if (salir) salir.addEventListener('submit', function (ev) {
        var n = P.pendientes(leer()).length;
        if (n && !confirm('Hay ' + n + ' carga(s) sin enviar. Si salís ahora se pierden. ¿Salir igual?')) { ev.preventDefault(); return; }
        try { localStorage.removeItem(CLAVE); } catch (e) { /* nada */ }
        if (window.caches) caches.delete('is124-pages');
    });

    // ---- Precarga de las páginas del docente (la hace el service worker) ----
    function precargar() {
        if (!navigator.onLine || !navigator.serviceWorker || !navigator.serviceWorker.controller) return;
        navigator.serviceWorker.controller.postMessage({ tipo: 'precargar' });
    }

    window.addEventListener('online', function () { ciclo(); precargar(); });
    document.addEventListener('visibilitychange', function () { if (!document.hidden) { ciclo(); precargar(); } });
    mostrar();
    ciclo();
    precargar();
    setInterval(function () { precargar(); }, 30 * 60 * 1000);
})();
