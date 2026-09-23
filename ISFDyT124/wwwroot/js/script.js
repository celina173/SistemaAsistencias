document.addEventListener('DOMContentLoaded', function () {

    // =========================================
    // 1. NAVEGACIÓN Y MENÚ LATERAL
    // =========================================
    const sidebarMenu = document.getElementById('sidebarMenu');
    const toggleSidebar = document.getElementById('toggleSidebar');

    const handleSidebar = () => {
        if (sidebarMenu) {
            sidebarMenu.classList.toggle('active');
        }
    };

    if (toggleSidebar) {
        toggleSidebar.addEventListener('click', handleSidebar);
    }

    // Cerrar sidebar al hacer click fuera en móviles
    document.addEventListener('click', (e) => {
        if (sidebarMenu && sidebarMenu.classList.contains('active')) {
            if (!sidebarMenu.contains(e.target) && !toggleSidebar.contains(e.target)) {
                sidebarMenu.classList.remove('active');
            }
        }
    });

    // =========================================
    // 2. OTROS COMPORTAMIENTOS CLIENTE
    // =========================================

    // Aquí se pueden agregar validaciones adicionales o efectos visuales.

    // =========================================
    // 3. MODAL DE CONFIRMACIÓN (reemplaza al confirm() nativo del navegador)
    // Uso: <form data-confirm-message="¿Está seguro...?">
    // =========================================
    let confirmOverlay = null;

    function ensureConfirmModal() {
        if (confirmOverlay) return confirmOverlay;

        confirmOverlay = document.createElement('div');
        confirmOverlay.className = 'confirm-modal-overlay';
        confirmOverlay.innerHTML =
            '<div class="confirm-modal">' +
            '<p class="confirm-modal-message"></p>' +
            '<div class="form-buttons">' +
            '<button type="button" class="btn-cancel confirm-modal-cancel">Cancelar</button>' +
            '<button type="button" class="btn-save confirm-modal-accept">Confirmar</button>' +
            '</div>' +
            '</div>';
        document.body.appendChild(confirmOverlay);

        confirmOverlay.querySelector('.confirm-modal-cancel').addEventListener('click', hideConfirmModal);
        confirmOverlay.addEventListener('click', function (e) {
            if (e.target === confirmOverlay) hideConfirmModal();
        });

        return confirmOverlay;
    }

    function hideConfirmModal() {
        if (confirmOverlay) confirmOverlay.classList.remove('active');
    }

    document.querySelectorAll('form[data-confirm-message]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (form.dataset.confirmed === 'true') return;

            e.preventDefault();
            var overlay = ensureConfirmModal();
            overlay.querySelector('.confirm-modal-message').textContent = form.dataset.confirmMessage;
            overlay.classList.add('active');

            var acceptBtn = overlay.querySelector('.confirm-modal-accept');
            // Asignación directa (no addEventListener) para no acumular listeners
            // viejos si el usuario cancela y reintenta con otro formulario.
            acceptBtn.onclick = function () {
                hideConfirmModal();
                form.dataset.confirmed = 'true';
                form.requestSubmit ? form.requestSubmit() : form.submit();
            };
        });
    });

    // =========================================
    // 4. PWA: SERVICE WORKER Y BANNER DE INSTALACIÓN
    // =========================================
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js').catch(function () {
                // Si falla el registro (por ejemplo, en un entorno sin HTTPS) la app sigue
                // funcionando normal, solo sin capacidad offline/instalación.
            });
        });
    }

    const pwaBanner = document.getElementById('pwaInstallBanner');
    const pwaInstallBtn = document.getElementById('pwaInstallBtn');
    const pwaInstallDismiss = document.getElementById('pwaInstallDismiss');

    function yaEstaInstalada() {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    }

    if (pwaBanner && !yaEstaInstalada() && !sessionStorage.getItem('pwaInstallDismissed')) {
        let deferredInstallPrompt = null;

        window.addEventListener('beforeinstallprompt', function (e) {
            e.preventDefault();
            deferredInstallPrompt = e;
            pwaBanner.hidden = false;
        });

        window.addEventListener('appinstalled', function () {
            pwaBanner.hidden = true;
            deferredInstallPrompt = null;
        });

        // iOS Safari no dispara beforeinstallprompt: mostramos igual el aviso, con instrucciones
        // manuales, para que el docente sepa que puede instalarla desde "Compartir".
        const esIOS = /iphone|ipad|ipod/i.test(window.navigator.userAgent);
        if (esIOS && !window.navigator.standalone) {
            pwaBanner.querySelector('.pwa-install-text').textContent =
                'Instalá la app: tocá el botón Compartir de Safari y elegí "Agregar a pantalla de inicio".';
            if (pwaInstallBtn) pwaInstallBtn.hidden = true;
            pwaBanner.hidden = false;
        }

        if (pwaInstallBtn) {
            pwaInstallBtn.addEventListener('click', function () {
                if (!deferredInstallPrompt) return;
                deferredInstallPrompt.prompt();
                deferredInstallPrompt.userChoice.finally(function () {
                    deferredInstallPrompt = null;
                    pwaBanner.hidden = true;
                });
            });
        }

        if (pwaInstallDismiss) {
            pwaInstallDismiss.addEventListener('click', function () {
                pwaBanner.hidden = true;
                sessionStorage.setItem('pwaInstallDismissed', 'true');
            });
        }
    }

});
