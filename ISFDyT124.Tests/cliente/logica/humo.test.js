import test from 'node:test';
import assert from 'node:assert/strict';
import 'fake-indexeddb/auto';

function abrir(nombre) {
  return new Promise((resolver, rechazar) => {
    const pedido = indexedDB.open(nombre, 1);
    pedido.onupgradeneeded = () => pedido.result.createObjectStore('bandeja', { keyPath: 'id' });
    pedido.onsuccess = () => resolver(pedido.result);
    pedido.onerror = () => rechazar(pedido.error);
  });
}

test('el almacenamiento local simulado guarda y devuelve un registro', async () => {
  const db = await abrir('humo');
  await new Promise((ok, mal) => {
    const tx = db.transaction('bandeja', 'readwrite');
    tx.objectStore('bandeja').put({ id: 'a1', estado: 'pendiente' });
    tx.oncomplete = ok;
    tx.onerror = () => mal(tx.error);
  });
  const leido = await new Promise((ok, mal) => {
    const pedido = db.transaction('bandeja').objectStore('bandeja').get('a1');
    pedido.onsuccess = () => ok(pedido.result);
    pedido.onerror = () => mal(pedido.error);
  });
  assert.deepEqual(leido, { id: 'a1', estado: 'pendiente' });
});
