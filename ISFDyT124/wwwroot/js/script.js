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

});
