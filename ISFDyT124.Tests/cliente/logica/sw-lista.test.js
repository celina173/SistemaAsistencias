import test from 'node:test';
import assert from 'node:assert';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// El cliente no tiene etapa de compilación: la lista de archivos que guarda el service worker (APP_SHELL) se
// mantiene a mano. Esta prueba falla si el layout usa un archivo que la lista no guarda, o si la lista nombra
// uno que ya no existe.
const raiz = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../ISFDyT124');
const sw = fs.readFileSync(path.join(raiz, 'wwwroot/sw.js'), 'utf8');
const layout = fs.readFileSync(path.join(raiz, 'Views/Shared/_Layout.cshtml'), 'utf8');

const shell = [...sw.match(/const APP_SHELL = \[([\s\S]*?)\];/)[1].matchAll(/'([^']+)'/g)].map((m) => m[1]);
const usados = [...new Set([...layout.matchAll(/(?:src|href)="~(\/(?:css|js|lib|images)\/[^"?]+)/g)].map((m) => m[1]))];

test('todo archivo que usa el layout está en la lista del service worker', () => {
    const faltan = usados.filter((u) => !shell.includes(u) && !/\.(png|jpg|svg|ico)$/.test(u) && !u.startsWith('/lib/bootstrap'));
    assert.deepEqual(faltan, [], 'Agregar a APP_SHELL en wwwroot/sw.js: ' + faltan.join(', '));
});

test('todo archivo de la lista existe', () => {
    const noExisten = shell.filter((u) => !u.startsWith('/Account') && u !== '/' && !fs.existsSync(path.join(raiz, 'wwwroot', u)));
    assert.deepEqual(noExisten, []);
});
