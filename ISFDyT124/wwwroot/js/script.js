document.addEventListener('DOMContentLoaded', function () {

    const bodyElement = document.body;

    // =========================================
    // 1. NAVEGACIÓN Y MENÚ LATERAL
    // =========================================
    const menuIcon = document.querySelector('.menu-icon');
    const sidebarMenu = document.getElementById('sidebarMenu');
    const toggleSidebar = document.getElementById('toggleSidebar');

    const handleSidebar = () => {
        if (sidebarMenu) {
            sidebarMenu.classList.toggle('active');
        }
    };

    if (menuIcon) menuIcon.addEventListener('click', handleSidebar);
    if (toggleSidebar) toggleSidebar.addEventListener('click', handleSidebar);


    // =========================================
    // 2. PANTALLA LOGIN -> REDIRECCIÓN
    // =========================================
    const loginForm = document.getElementById('loginForm');

    if (loginForm) {
        loginForm.addEventListener('submit', function (event) {
            event.preventDefault();

            const usuario = document.getElementById('usuario').value.trim();
            const contrasena = document.getElementById('contrasena').value.trim();

            if (usuario === 'docente' && contrasena === 'docente') {
                window.location.href = 'inicio-docente.html';
            } else if (usuario === 'admin' && contrasena === 'admin') {
                window.location.href = 'inicio-admin.html';
            } else {
                alert('Usuario o contraseña incorrectos. Intente nuevamente.');
            }
        });
    }

    // =========================================
    // 3. PANTALLA INICIO DOCENTE -> ASISTENCIA
    // =========================================
    const btnSiguiente = document.getElementById('btnSiguiente');

    if (btnSiguiente) {
        btnSiguiente.addEventListener('click', function () {
            const carrera = document.getElementById('carrera').value;
            const materia = document.getElementById('materia').value;

            if (carrera === "" || materia === "") {
                alert("Por favor, seleccione una Carrera y una Materia antes de continuar.");
            } else {
                window.location.href = 'asistencia.html';
            }
        });
    }

    // =========================================
    // 4. PANTALLA ASISTENCIA DOCENTE -> GUARDAR
    // =========================================
    const btnGuardarAsistencia = document.getElementById('btnGuardar');

    if (btnGuardarAsistencia) {
        btnGuardarAsistencia.addEventListener('click', function () {
            alert("¡La asistencia se ha guardado correctamente!");
            window.location.href = 'asistencia-global.html';
        });
    }

    // =========================================
    // 5. INICIO ADMIN -> ACCESOS RÁPIDOS
    // =========================================
    const cardEstudiantes = document.getElementById('card-estudiantes');
    if (cardEstudiantes) {
        cardEstudiantes.addEventListener('click', function () {
            window.location.href = 'gestion-estudiantes.html';
        });
    }

    const cardMaterias = document.getElementById('card-materias');
    if (cardMaterias) {
        cardMaterias.addEventListener('click', function () {
            window.location.href = 'gestion-materias.html';
        });
    }

    const cardCarreras = document.getElementById('card-carreras');
    if (cardCarreras) {
        cardCarreras.addEventListener('click', function () {
            window.location.href = 'gestion-carreras.html';
        });
    }

    const cardDocentes = document.getElementById('card-docentes');
    if (cardDocentes) {
        cardDocentes.addEventListener('click', function () {
            window.location.href = 'gestion-docentes.html';
        });
    }

    // FIX: Se corrigió la variable indefinida que colgaba el script entero
    const cardInscripciones = document.getElementById('card-inscripciones');
    if (cardInscripciones) {
        cardInscripciones.addEventListener('click', function () {
            window.location.href = 'gestion-inscripciones-materias.html';
        });
    }

    // =========================================
    // 6. GESTIÓN (ABM) -> RENDERIZADO DE TABLA
    // =========================================
    const tablaEstudiantes = document.querySelector('.crud-table tbody');

    if (tablaEstudiantes && bodyElement.classList.contains('gestion-estudiantes')) {
        const estudiantesGuardados = JSON.parse(localStorage.getItem('estudiantes')) || [];
        tablaEstudiantes.innerHTML = '';

        estudiantesGuardados.forEach((est, index) => {
            const nuevaFila = document.createElement('tr');
            nuevaFila.innerHTML = `
                <td>${est.apellidos} ${est.nombres}</td>
                <td style="text-align: center;">${est.dni}</td>
                <td>${est.carrera}</td>
                <td class="action-cell">
                    <button class="icon-btn btn-edit" title="Editar">&#9998;</button>
                    <button class="icon-btn btn-delete" onclick="eliminarEstudiante(${index})">&#128465;</button>
                </td>
            `;
            tablaEstudiantes.appendChild(nuevaFila);
        });
    }

    // =========================================
    // 7. CONTROL CENTRALIZADO DE REDIRECCIÓN PARA AGREGAR (MÁXIMA PRECISIÓN)
    // =========================================
    const btnAgregar = document.getElementById('btnAgregar');

    if (btnAgregar) {
        btnAgregar.addEventListener('click', () => {
            // Aisla el nombre exacto del archivo final (ej: "gestion-materias.html")
            const archivoActual = window.location.pathname.split('/').pop().toLowerCase();

            console.log("Archivo HTML actual detectado:", archivoActual);

            if (archivoActual === 'gestion-inscripciones-materias.html' || archivoActual === 'gestion-inscripciones.html') {
                window.location.href = 'agregar-inscripcion-materia.html';
            } else if (archivoActual === 'gestion-materias.html') {
                window.location.href = 'agregar-materia.html';
            } else if (archivoActual === 'gestion-docentes.html') {
                window.location.href = 'agregar-docente.html';
            } else if (archivoActual === 'gestion-carreras.html') {
                window.location.href = 'agregar-carrera.html';
            } else {
                // Por defecto, si estás en gestion-estudiantes.html
                window.location.href = 'agregar-estudiante.html';
            }
        });
    }

    // =========================================
    // 8. CONTROL CENTRALIZADO DE REDIRECCIÓN PARA EDITAR (MÁXIMA PRECISIÓN)
    // =========================================
    const tablaGlobal = document.querySelector('table, .global-table, .crud-table');

    if (tablaGlobal) {
        tablaGlobal.addEventListener('click', (e) => {
            const btnEdit = e.target.closest('.btn-edit');

            if (btnEdit) {
                e.preventDefault();

                // Aisla el nombre exacto del archivo final
                const archivoActual = window.location.pathname.split('/').pop().toLowerCase();

                if (archivoActual === 'gestion-inscripciones-materias.html' || archivoActual === 'gestion-inscripciones.html') {
                    window.location.href = 'modificar-inscripcion-materia.html';
                } else if (archivoActual === 'gestion-materias.html') {
                    window.location.href = 'modificar-materia.html';
                } else if (archivoActual === 'gestion-docentes.html') {
                    window.location.href = 'modificar-docente.html';
                } else if (archivoActual === 'gestion-carreras.html') {
                    window.location.href = 'modificar-carrera.html';
                } else {
                    window.location.href = 'modificar-estudiante.html';
                }
            }
        });
    }
    // =========================================
    // 9. FORMULARIO AGREGAR -> LOCAL STORAGE
    // =========================================
    const formAgregarEstudiante = document.getElementById('formAgregarEstudiante');

    if (formAgregarEstudiante) {
        formAgregarEstudiante.addEventListener('submit', function (event) {
            event.preventDefault();

            const nuevoEstudiante = {
                apellidos: document.getElementById('apellidos').value,
                nombres: document.getElementById('nombres').value,
                dni: document.getElementById('dni').value,
                carrera: document.getElementById('carrera').value
            };

            let listaActual = JSON.parse(localStorage.getItem('estudiantes')) || [];
            listaActual.push(nuevoEstudiante);
            localStorage.setItem('estudiantes', JSON.stringify(listaActual));

            alert("¡Estudiante guardado con éxito!");
            window.location.href = 'gestion-estudiantes.html';
        });
    }
});