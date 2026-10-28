import test from 'node:test';
import assert from 'node:assert';
import { createRequire } from 'node:module';

const P = createRequire(import.meta.url)('../../../ISFDyT124/wwwroot/js/pwa-core.js');

const campos = (fecha, extra = []) => [['CaMaId', '7'], ['FechaAsistencia', fecha], ['Rows[0].UsId', '1'], ...extra];

test('hoy en Argentina a las 22:30 sigue siendo el mismo día aunque UTC ya cambió', () => {
    assert.equal(P.hoyArgentina(Date.parse('2026-10-29T01:30:00Z')), '2026-10-28');
});
test('misma cátedra y fecha: la última pisa a la anterior', () => {
    let b = P.agregarEntrada([], '/Asistencias/Asistencia', campos('2026-10-28'));
    b = P.agregarEntrada(b, '/Asistencias/Asistencia', campos('2026-10-28', [['x', '1']]));
    assert.equal(b.length, 1);
    assert.equal(b[0].campos.length, 4);
    b = P.agregarEntrada(b, '/Asistencias/Asistencia', campos('2026-10-29'));
    assert.equal(b.length, 2);
});
test('formulario con campo renombrado no se reconoce', () => {
    assert.ok(P.formularioReconocido('/Asistencias/Asistencia', campos('2026-10-28')));
    assert.ok(!P.formularioReconocido('/Asistencias/Asistencia', [['CaMaId', '7'], ['Fecha', 'x'], ['Rows[0].UsId', '1']]));
    assert.ok(!P.formularioReconocido('/Otra/Cosa', campos('2026-10-28')));
    assert.ok(!P.formularioReconocido('/Asistencias/Asistencia', campos('')), 'sin fecha elegida no se intercepta');
});
test('interpretación de respuestas', () => {
    assert.equal(P.interpretarRespuesta({ status: 200, url: '/Asistencias', html: '<p>ok</p>' }).estado, 'aceptada');
    const rech = P.interpretarRespuesta({ status: 200, url: '/Asistencias/Asistencia', html: '<div id="operationError"><strong>x</strong><p>Fecha inválida</p></div>' });
    assert.deepEqual([rech.estado, rech.motivo], ['rechazada', 'Fecha inválida']);
    assert.equal(P.interpretarRespuesta({ status: 200, url: '/Account/Login?ReturnUrl=x', html: '' }).estado, 'sesionVencida');
    const mal = P.interpretarRespuesta({ status: 400, url: '/Asistencias/Asistencia', html: 'La fecha no pertenece al calendario de cursada.' });
    assert.deepEqual([mal.estado, mal.motivo], ['rechazada', 'La fecha no pertenece al calendario de cursada.']);
    assert.equal(P.interpretarRespuesta({ status: 502 }).estado, 'reintentable');
    assert.equal(P.interpretarRespuesta({ error: true }).estado, 'reintentable');
});
test('rechazada no se reintenta, reintentable sí y suma intentos', () => {
    let b = P.agregarEntrada([], '/Asistencias/Asistencia', campos('2026-10-28'));
    const k = b[0].clave;
    b = P.aplicarResultado(b, k, { estado: 'reintentable', motivo: 'red' });
    assert.equal(P.pendientes(b).length, 1);
    assert.equal(b[0].intentos, 1);
    b = P.aplicarResultado(b, k, { estado: 'rechazada', motivo: 'no' });
    assert.equal(P.pendientes(b).length, 0);
    assert.equal(P.rechazadas(b).length, 1);
    assert.equal(P.descartar(b, k).length, 0);
});
test('esperas progresivas con tope de 15 minutos', () => {
    assert.deepEqual([0, 1, 2, 3, 9].map(P.esperaMs), [5000, 15000, 45000, 900000, 900000]);
});
test('"hoy" sin señal sólo con una única fecha igual a la guardada', () => {
    const guardada = Date.parse('2026-10-26T15:00:00Z');
    const ahora = Date.parse('2026-10-28T15:00:00Z');
    assert.equal(P.debeAgregarHoy(['2026-10-26'], guardada, ahora, true), '2026-10-28');
    assert.equal(P.debeAgregarHoy(['2026-10-26'], guardada, ahora, false), null);
    assert.equal(P.debeAgregarHoy(['2026-10-26', '2026-10-27'], guardada, ahora, true), null);
    assert.equal(P.debeAgregarHoy(['2026-10-20'], guardada, ahora, true), null);
});
