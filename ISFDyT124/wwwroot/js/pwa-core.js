// Lógica pura de la PWA (sin DOM ni red): se usa desde el navegador y desde las pruebas de Node.
// Captura y reenvío: la app guarda el formulario REAL de asistencia y lo reenvía al servidor, que es
// el único que decide las reglas. Acá sólo hay bandeja, esperas e interpretación de respuestas.
(function (raiz, fabrica) {
    if (typeof module === 'object' && module.exports) module.exports = fabrica();
    else raiz.PwaCore = fabrica();
})(typeof self !== 'undefined' ? self : this, function () {
    var ESTADOS = { PENDIENTE: 'pendiente', ENVIANDO: 'enviando', SINCRONIZADO: 'sincronizado', RECHAZADO: 'rechazado' };
    var ESPERAS_MS = [5000, 15000, 45000]; // luego se queda en 15 min
    var ESPERA_MAXIMA_MS = 15 * 60 * 1000;
    var TIEMPO_PEDIDO_MS = 60000; // tolera el arranque en frío (~40 s)
    var ACTIONS = ['/Asistencias/Asistencia', '/Asistencias/ModificarAsistencia'];

    function esActionConocida(action) {
        try { action = new URL(action, 'https://x').pathname; } catch (e) { return false; }
        return ACTIONS.some(function (a) { return a.toLowerCase() === action.toLowerCase(); });
    }

    // Fecha de hoy en Argentina (UTC-3 fijo, sin horario de verano).
    function hoyArgentina(ahoraMs) {
        var d = new Date((ahoraMs === undefined ? Date.now() : ahoraMs) - 3 * 3600 * 1000);
        return d.toISOString().slice(0, 10);
    }

    // Una sola entrada por cátedra y fecha: la última pisa a la anterior.
    function claveEntrada(action, campos) {
        var caMa = '', fecha = '';
        campos.forEach(function (p) {
            if (p[0] === 'CaMaId') caMa = p[1];
            if (p[0] === 'FechaAsistencia') fecha = p[1];
        });
        return [action.toLowerCase(), caMa, fecha].join('|');
    }

    // El formulario es reconocible sólo si trae lo que el servidor espera; si no, no se intercepta.
    function formularioReconocido(action, campos) {
        if (!esActionConocida(action)) return false;
        var nombres = campos.map(function (p) { return p[0]; });
        // Sin fecha elegida no se intercepta: el servidor responde con su propio aviso y no se guarda una carga vacía.
        var fecha = campos.filter(function (p) { return p[0] === 'FechaAsistencia'; })[0];
        return nombres.indexOf('CaMaId') >= 0 && !!fecha && fecha[1] !== '' &&
            nombres.some(function (n) { return /^Rows\[\d+\]\./.test(n); });
    }

    function agregarEntrada(bandeja, action, campos, ahoraMs) {
        var clave = claveEntrada(action, campos);
        var resto = bandeja.filter(function (e) { return e.clave !== clave; });
        resto.push({ clave: clave, action: action, campos: campos, estado: ESTADOS.PENDIENTE,
            intentos: 0, motivo: '', creada: ahoraMs === undefined ? Date.now() : ahoraMs });
        return resto;
    }

    function pendientes(bandeja) {
        return bandeja.filter(function (e) { return e.estado === ESTADOS.PENDIENTE || e.estado === ESTADOS.ENVIANDO; });
    }
    function rechazadas(bandeja) {
        return bandeja.filter(function (e) { return e.estado === ESTADOS.RECHAZADO; });
    }

    function esperaMs(intentos) {
        return intentos < ESPERAS_MS.length ? ESPERAS_MS[intentos] : ESPERA_MAXIMA_MS;
    }

    // Lee el resultado de un reenvío. El servidor responde con una redirección: el éxito llega a la
    // pantalla de destino y el rechazo vuelve a la planilla con el aviso #operationError.
    function interpretarRespuesta(r) {
        if (!r || r.error || r.status === 0 || r.status >= 500 || r.status === 408 || r.status === 429)
            return { estado: 'reintentable', motivo: 'Sin conexión con el servidor' };
        if (r.status === 401 || r.status === 403 || /\/Account\/Login/i.test(r.url || ''))
            return { estado: 'sesionVencida', motivo: 'La sesión venció' };
        var m = /id="operationError"[\s\S]*?<p>([\s\S]*?)<\/p>/i.exec(r.html || '');
        if (m) return { estado: 'rechazada', motivo: m[1].replace(/<[^>]+>/g, '').trim() };
        if (r.status >= 400) {
            // El servidor responde 400 con el motivo en texto plano (p. ej. fecha fuera del calendario de cursada).
            var texto = (r.html || '').trim();
            var motivo = texto && texto.length < 300 && texto.indexOf('<') < 0 ? texto : 'El servidor rechazó la carga (' + r.status + ')';
            return { estado: 'rechazada', motivo: motivo };
        }
        return { estado: 'aceptada', motivo: '' };
    }

    // Aplica el resultado a la entrada. Una rechazada no se reintenta; una reintentable sí.
    function aplicarResultado(bandeja, clave, resultado) {
        return bandeja.map(function (e) {
            if (e.clave !== clave) return e;
            var n = Object.assign({}, e);
            if (resultado.estado === 'aceptada') { n.estado = ESTADOS.SINCRONIZADO; n.motivo = ''; }
            else if (resultado.estado === 'rechazada') { n.estado = ESTADOS.RECHAZADO; n.motivo = resultado.motivo; }
            else { n.estado = ESTADOS.PENDIENTE; n.intentos = e.intentos + 1; n.motivo = resultado.motivo; }
            return n;
        });
    }

    function descartar(bandeja, clave) {
        return bandeja.filter(function (e) { return e.clave !== clave; });
    }

    // "Hoy" sin señal: sólo si la planilla viene de lo guardado y ofrece UNA fecha igual a la del día en que
    // se guardó. No valida nada: el servidor acepta o rechaza al reenviar.
    function debeAgregarHoy(opciones, guardadaEnMs, ahoraMs, desdeGuardado) {
        if (!desdeGuardado || opciones.length !== 1) return null;
        if (opciones[0] !== hoyArgentina(guardadaEnMs)) return null;
        var hoy = hoyArgentina(ahoraMs);
        return hoy !== opciones[0] ? hoy : null;
    }

    return { ESTADOS: ESTADOS, TIEMPO_PEDIDO_MS: TIEMPO_PEDIDO_MS, esActionConocida: esActionConocida,
        hoyArgentina: hoyArgentina, claveEntrada: claveEntrada, formularioReconocido: formularioReconocido,
        agregarEntrada: agregarEntrada, pendientes: pendientes, rechazadas: rechazadas, esperaMs: esperaMs,
        interpretarRespuesta: interpretarRespuesta, aplicarResultado: aplicarResultado, descartar: descartar,
        debeAgregarHoy: debeAgregarHoy };
});
