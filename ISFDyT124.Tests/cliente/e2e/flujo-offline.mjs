// Recorrido real en Chromium: precarga con señal, tomar asistencia sin señal, reconectar y ver el reenvío.
// Uso: node e2e/flujo-offline.mjs [urlBase]   (la app y la semilla ya deben estar levantadas)
import { chromium } from 'playwright';
import { spawn, execSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const base = process.argv[2] || 'http://localhost:5199';
const espera = (ms) => new Promise((r) => setTimeout(r, ms));
// "Sin conexión" de verdad: se apaga el servidor. setOffline de Playwright no alcanza a los pedidos del service worker.
const carpetaApp = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../ISFDyT124');
let servidor = null;
async function arrancar() {
    servidor = spawn('dotnet', ['run', '--no-build', '--no-launch-profile'], {
        cwd: carpetaApp, stdio: 'ignore',
        env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', ASPNETCORE_URLS: base },
    });
    for (let i = 0; i < 60; i++) {
        try { if ((await fetch(base + '/Account/Login')).ok) return; } catch { /* aún no */ }
        await espera(1000);
    }
    throw new Error('la app no arrancó');
}
function detener() {
    if (servidor) { try { execSync(`taskkill /PID ${servidor.pid} /T /F`, { stdio: 'ignore' }); } catch { /* ya cerrada */ } servidor = null; }
}
await arrancar();
const navegador = await chromium.launch();
const contexto = await navegador.newContext();
const page = await contexto.newPage();
page.on('response', (r) => { if (r.request().method() === 'POST' || r.status() >= 300) console.log('  [http]', r.request().method(), r.status(), r.url().replace(base, '')); });
page.on('pageerror', (e) => console.log('  [pageerror]', e.message));
let ok = true;
const paso = (nombre, cond, extra = '') => { console.log((cond ? 'OK   ' : 'FALLA'), nombre, extra); if (!cond) ok = false; };

try {
    await page.goto(base + '/Account/Login');
    await page.locator('#usuario').fill('30000001');
    await page.locator('#contrasena').fill('Prueba123');
    await Promise.all([page.waitForLoadState('load'), page.locator('form button[type="submit"], form input[type="submit"]').first().click()]);
    paso('login docente', !/Account\/Login/.test(page.url()), page.url());

    // El service worker toma el control en la carga siguiente a la instalación.
    await page.goto(base + '/Asistencias');
    await page.waitForFunction(() => navigator.serviceWorker.ready.then(() => true));
    await page.reload();
    await page.waitForFunction(() => !!navigator.serviceWorker.controller, null, { timeout: 15000 });
    await espera(4000); // precarga
    const guardadas = await page.evaluate(async () => (await (await caches.open('is124-pages')).keys()).map((r) => new URL(r.url).pathname + new URL(r.url).search));
    paso('precarga guardó la planilla y la global de la cátedra sin haberlas abierto',
        guardadas.some((u) => /Asistencia\?CaMaId=1/i.test(u)) && guardadas.some((u) => /AsistenciaGlobal\?CaMaId=1/i.test(u)), JSON.stringify(guardadas));

    detener(); // sin servidor
    await contexto.setOffline(true);
    // Dos días después, sin señal: la cátedra sin calendario sólo guardó "hoy" de antes, así que se agrega el de ahora.
    await page.clock.setFixedTime(new Date(Date.now() + 2 * 86400000));
    await page.goto(base + '/Asistencias/Asistencia?CaMaId=1');
    const dos = await page.locator('#FechaAsistencia option').evaluateAll((o) => o.map((x) => x.value).filter(Boolean));
    paso('dos días después sin señal se ofrece también "hoy"', dos.length === 2, JSON.stringify(dos));
    await page.clock.setFixedTime(new Date());
    await page.goto(base + '/Asistencias/Asistencia?CaMaId=1');
    const hayForm = await page.locator('form#formAsistencia').count();
    paso('sin señal abre la planilla de una cátedra nunca visitada', hayForm === 1);
    const opciones = await page.locator('#FechaAsistencia option').evaluateAll((o) => o.map((x) => x.value).filter(Boolean));
    paso('la planilla guardada ofrece una fecha', opciones.length === 1, JSON.stringify(opciones));
    paso('muestra "datos al" porque viene de lo guardado', /datos al/.test((await page.locator('body').textContent()) || ''));
    await page.locator('#FechaAsistencia').selectOption(opciones[0]);

    await page.locator('#formAsistencia button[type="submit"], #formAsistencia input[type="submit"]').first().click();
    await page.waitForSelector('#pwaBandeja', { timeout: 8000 }).catch(() => null);
    const txt = (await page.locator('#pwaBandeja').textContent().catch(() => '')) || '';
    paso('guardar sin señal queda en la bandeja', /1 carga pendiente/.test(txt), txt);

    await arrancar(); // vuelve el servidor (la sesión sobrevive: las claves están en la base)
    await contexto.setOffline(false);
    await page.evaluate(() => window.dispatchEvent(new Event('online')));
    await page.waitForFunction(() => !document.getElementById('pwaBandeja'), null, { timeout: 90000 }).catch(() => null);
    const queda = await page.locator('#pwaBandeja').count();
    paso('al volver la señal la bandeja se vacía (reenvío aceptado)', queda === 0, queda ? ((await page.locator('#pwaBandeja').textContent()) || '') : '');
} catch (e) {
    console.log('ERROR', e.message);
    ok = false;
} finally {
    await navegador.close();
    detener();
}
process.exit(ok ? 0 : 1);
