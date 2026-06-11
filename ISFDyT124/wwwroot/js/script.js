document.addEventListener('DOMContentLoaded', function () {

    // 1. MENÚ LATERAL
    const menuIcon = document.querySelector('.menu-icon');
    const sidebarMenu = document.getElementById('sidebarMenu');

    if (menuIcon && sidebarMenu) {
        menuIcon.addEventListener('click', function () {
            sidebarMenu.classList.toggle('active');
        });
    }

    // 3. INICIO -> ASISTENCIA
    const btnSiguiente = document.getElementById('btnSiguiente');
    if (btnSiguiente) {
        btnSiguiente.addEventListener('click', function () {
            const carrera = document.getElementById('carrera').value;
            const materia = document.getElementById('materia').value;

            if (carrera === "" || materia === "") {
                alert("Por favor, seleccione una Carrera y una Materia.");
            } else {
                window.location.href = '/Home/Asistencia';
            }
        });
    }

    // 4. ASISTENCIA -> GLOBAL
    const btnGuardarAsistencia = document.getElementById('btnGuardar');
    if (btnGuardarAsistencia) {
        btnGuardarAsistencia.addEventListener('click', function () {
            alert("¡La asistencia se ha guardado correctamente!");
            window.location.href = '/Home/AsistenciaGlobal';
        });
    }

    // 5. PANEL ADMIN -> ABM USUARIOS
    const cardUsuarios = document.getElementById('card-usuarios');
    if (cardUsuarios) {
        cardUsuarios.addEventListener('click', function () {
            window.location.href = '/Admin/UsuariosABM';
        });
    }

    // 6. ABM -> CARGAR TABLA
    const tablaUsuarios = document.querySelector('.crud-table tbody');
    if (tablaUsuarios && document.body.classList.contains('gestion-page')) {
        const usuariosGuardados = JSON.parse(localStorage.getItem('usuarios')) || [];

        usuariosGuardados.forEach((user, index) => {
            const nuevaFila = document.createElement('tr');
            nuevaFila.innerHTML = `
                <td>${user.apellidos} ${user.nombres}</td>
                <td>${user.dni}</td>
                <td><strong>${user.rol}</strong></td>
                <td>${user.carrera}</td>
                <td class="action-cell">
                    <button class="icon-btn btn-edit" title="Editar">&#9998;</button>
                    <button class="icon-btn btn-delete" onclick="eliminarUsuario(${index})">&#128465;</button>
                </td>
            `;
            tablaUsuarios.appendChild(nuevaFila);
        });
    }

    const btnAgregar = document.getElementById('btnAgregar');
    if (btnAgregar) {
        btnAgregar.addEventListener('click', () => window.location.href = '/Admin/UsuarioAgregar');
    }

    // 7. AGREGAR USUARIO -> GUARDAR
    const formAgregarUsuario = document.getElementById('formAgregarUsuario');
    if (formAgregarUsuario) {
        formAgregarUsuario.addEventListener('submit', function (event) {
            event.preventDefault();

            const nuevoUsuario = {
                apellidos: document.getElementById('apellidos').value,
                nombres: document.getElementById('nombres').value,
                dni: document.getElementById('dni').value,
                rol: document.getElementById('rol').value,
                carrera: document.getElementById('carrera').value
            };

            let listaActual = JSON.parse(localStorage.getItem('usuarios')) || [];
            listaActual.push(nuevoUsuario);
            localStorage.setItem('usuarios', JSON.stringify(listaActual));

            alert("¡Usuario guardado con éxito!");
            window.location.href = '/Admin/UsuariosABM';
        });
    }
});

function eliminarUsuario(index) {
    if (confirm("¿Seguro que querés eliminar a este usuario?")) {
        let lista = JSON.parse(localStorage.getItem('usuarios')) || [];
        lista.splice(index, 1);
        localStorage.setItem('usuarios', JSON.stringify(lista));
        location.reload();
    }
}