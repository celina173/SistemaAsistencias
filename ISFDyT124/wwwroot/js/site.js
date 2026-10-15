// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Parche: marcar labels de campos obligatorios y proteger contra texto largo en placeholders
document.addEventListener('DOMContentLoaded', function () {
  try {
    // 1) Añadir asterisco a labels asociados a inputs/selects/textarea con required o data-val-required
    const selector = [
      'input[data-val-required]',
      'select[data-val-required]',
      'textarea[data-val-required]',
      'input[required]',
      'select[required]',
      'textarea[required]'
    ].join(',');

    document.querySelectorAll(selector).forEach(function (el) {
      const id = el.id;
      if (!id) return;
      const label = document.querySelector('label[for="' + id + '"]');
      if (label && !label.classList.contains('required-asterisk')) {
        label.classList.add('required-asterisk');
        const star = document.createElement('span');
        star.textContent = ' *';
        star.className = 'required-asterisk-span';
        label.appendChild(star);
        // accesibilidad
        el.setAttribute('aria-required', 'true');
      }
    });

    // 2) Recortar placeholders / textos largos que puedan forzar layout (mitigación visual)
    const MAX_PLACEHOLDER = 60;
    document.querySelectorAll('#formAgregarUsuario input, #formAgregarUsuario textarea').forEach(function (el) {
      const ph = el.getAttribute('placeholder');
      if (ph && ph.length > MAX_PLACEHOLDER) {
        el.setAttribute('placeholder', ph.slice(0, MAX_PLACEHOLDER - 3) + '...');
      }
      // también truncar values visibles al render si son demasiado largos (no modifica datos)
      if (el.value && el.value.length > 200) {
        el.value = el.value.slice(0, 200);
      }
    });

    // 3) Eliminar checkboxes huérfanos (value inválido o etiqueta vacía) — parche temporal
    const cajas = Array.from(document.querySelectorAll('#materiasList input[type=checkbox]'));
    cajas.forEach(cb => {
      const txt = cb.parentElement ? cb.parentElement.textContent.trim() : '';
      if (!txt || cb.value === '0' || cb.value === '' || isNaN(Number(cb.value))) {
        cb.parentElement?.remove();
      }
    });

  } catch (e) {
    console.error('Init site patches error', e);
  }
});
