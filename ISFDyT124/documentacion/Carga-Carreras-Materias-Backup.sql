-- ============================================================================
-- Carga de Carreras y Materias reales, recuperadas de un backup viejo del
-- proyecto (Instituto.bak, esquema antiguo en snake_case). Idempotente: si ya
-- existe una fila con la misma denominacion, no la vuelve a insertar.
--
-- IMPORTANTE: antes de correr este script, aplicar la migracion
-- 'AmpliarMaDenominacion' (dotnet ef database update) contra esta misma base,
-- que amplia Materias.MaDenominacion de nvarchar(30) a nvarchar(100) -- sin eso,
-- 85 de las 194 materias no entran (superan los 30 caracteres originales).
--
-- Materias: la modalidad y la cantidad de modulos no existian en el esquema
-- viejo, asi que se cargan con un valor por defecto (Presencial / 1 modulo) --
-- hay que revisarlas y corregirlas a mano desde la pantalla de Materias para
-- las que correspondan.
-- ============================================================================
SET NOCOUNT ON;

-- ---------------------------------------------------------------------------
-- Carreras (8)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Carreras WHERE CaDenominacion = N'Tecnicatura Superior en Desarrollo de Software')
    INSERT INTO Carreras (CaDenominacion) VALUES (N'Tecnicatura Superior en Desarrollo de Software');
IF NOT EXISTS (SELECT 1 FROM Carreras WHERE CaDenominacion = N'Profesorado de Educación Inicial')
    INSERT INTO Carreras (CaDenominacion) VALUES (N'Profesorado de Educación Inicial');
IF NOT EXISTS (SELECT 1 FROM Carreras WHERE CaDenominacion = N'Profesorado de Educación Primaria')
    INSERT INTO Carreras (CaDenominacion) VALUES (N'Profesorado de Educación Primaria');
IF NOT EXISTS (SELECT 1 FROM Carreras WHERE CaDenominacion = N'Tecnicatura Superior en Administración General')
    INSERT INTO Carreras (CaDenominacion) VALUES (N'Tecnicatura Superior en Administración General');
IF NOT EXISTS (SELECT 1 FROM Carreras WHERE CaDenominacion = N'Tecnicatura Superior en PYMES')
    INSERT INTO Carreras (CaDenominacion) VALUES (N'Tecnicatura Superior en PYMES');
IF NOT EXISTS (SELECT 1 FROM Carreras WHERE CaDenominacion = N'Profesorado de Educación Especial')
    INSERT INTO Carreras (CaDenominacion) VALUES (N'Profesorado de Educación Especial');
IF NOT EXISTS (SELECT 1 FROM Carreras WHERE CaDenominacion = N'Tecnicatura Superior en Enfermeria')
    INSERT INTO Carreras (CaDenominacion) VALUES (N'Tecnicatura Superior en Enfermeria');
IF NOT EXISTS (SELECT 1 FROM Carreras WHERE CaDenominacion = N'Tecnicatura Superior en Acompañamiento Terapéutico')
    INSERT INTO Carreras (CaDenominacion) VALUES (N'Tecnicatura Superior en Acompañamiento Terapéutico');

-- ---------------------------------------------------------------------------
-- Materias (194) -- Modalidad = 'Presencial', CantModulos = 1 por defecto
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Derecho laboral y comercial')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Derecho laboral y comercial', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Taller de Lectura, escritura y oralidad')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Taller de Lectura, escritura y oralidad', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Taller de pensamiento lógico matemático')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Taller de pensamiento lógico matemático', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Taller de definición institucional')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Taller de definición institucional', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Filosofia')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Filosofia', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didactica General')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didactica General', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Pedagogía')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Pedagogía', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Análisis del mundo contemporaneo')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Análisis del mundo contemporaneo', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicología del desarrollo y el aprendizaje I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicología del desarrollo y el aprendizaje I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Corporeidad y Motricidad')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Corporeidad y Motricidad', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Arte y educación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Arte y educación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Campo de la práctica I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Campo de la práctica I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Teorías sociopolíticas y educación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Teorías sociopolíticas y educación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica y curriculum del Nivel Primario')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica y curriculum del Nivel Primario', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicología del desarrollo y aprendizaje II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicología del desarrollo y aprendizaje II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicología social e institucional')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicología social e institucional', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Cultura, comunicación y educación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Cultura, comunicación y educación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Educación artística')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Educación artística', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las prácticas del lenguaje y la literatura I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las prácticas del lenguaje y la literatura I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las Ciencias Sociales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las Ciencias Sociales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las Ciencias Naturales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las Ciencias Naturales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de la Matemática I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de la Matemática I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Campo de la práctica II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Campo de la práctica II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Historia y prospectiva de la educación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Historia y prospectiva de la educación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Políticas, legislación y administración del trabajo escolar')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Políticas, legislación y administración del trabajo escolar', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Configuraciones culturales del sujeto educativo de Primaria')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Configuraciones culturales del sujeto educativo de Primaria', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Medios audiovisuales, TICs y educación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Medios audiovisuales, TICs y educación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Educación Física Escolar')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Educación Física Escolar', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de Prácticas del Lenguaje y Literatura II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de Prácticas del Lenguaje y Literatura II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las Ciencias Sociales II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las Ciencias Sociales II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las Ciencias Naturales II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las Ciencias Naturales II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didactica de la Matemática II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didactica de la Matemática II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Campo de la práctica III')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Campo de la práctica III', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Reflexión filosófica de la educación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Reflexión filosófica de la educación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Dimensión ético-política de la praxis docente')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Dimensión ético-política de la praxis docente', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Pedagogía crítica de las diferencias')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Pedagogía crítica de las diferencias', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ateneo de Prácticas del Lenguaje y de Literatura')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ateneo de Prácticas del Lenguaje y de Literatura', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ateneo de Ciencias Sociales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ateneo de Ciencias Sociales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ateneo de Ciencias Naturales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ateneo de Ciencias Naturales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ateneo de Matemática')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ateneo de Matemática', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Campo de la práctica IV')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Campo de la práctica IV', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Trayecto Formativo Opcional- Informàtica')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Trayecto Formativo Opcional- Informàtica', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Trayectos formativos opcionales - Teatro')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Trayectos formativos opcionales - Teatro', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Educación temprana')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Educación temprana', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Campo de la Práctica')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Campo de la Práctica', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica y curriculum del Nivel inicial')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica y curriculum del Nivel inicial', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicología del desarrollo y aprendizaje II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicología del desarrollo y aprendizaje II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Educación plástica')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Educación plástica', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las prácticas del lenguaje y la lit. I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las prácticas del lenguaje y la lit. I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de la Matemática')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de la Matemática', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Campo de la Practica')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Campo de la Practica', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Políticas, legislación y adm. del trabajo esc.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Políticas, legislación y adm. del trabajo esc.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Juego y desarrollo infantil')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Juego y desarrollo infantil', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Educación musical')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Educación musical', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Taller de literatura infantil')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Taller de literatura infantil', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Taller de ciencias sociales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Taller de ciencias sociales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Taller de ciencias naturales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Taller de ciencias naturales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Taller de matemática')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Taller de matemática', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Producción de materiales y objetos lúdicos')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Producción de materiales y objetos lúdicos', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Dimensión ético política de la praxis docente')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Dimensión ético política de la praxis docente', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Educación en y para la salud')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Educación en y para la salud', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ateneo de Prácticas del lenguaje y la literatura')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ateneo de Prácticas del lenguaje y la literatura', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ateneo de naturaleza y sociedad')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ateneo de naturaleza y sociedad', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ateneo de nuevas expresiones estéticas')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ateneo de nuevas expresiones estéticas', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Práctica en terreno')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Práctica en terreno', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Trayecto Formación Opcional (Taller de Informática)')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Trayecto Formación Opcional (Taller de Informática)', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Trayecto Formación Opcional (Taller de Teatro)')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Trayecto Formación Opcional (Taller de Teatro)', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Filosofía')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Filosofía', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicología y desarrollo del aprendizaje I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicología y desarrollo del aprendizaje I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de prácticas del lenguaje I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de prácticas del lenguaje I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las ciencias sociales I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las ciencias sociales I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las ciencias naturales I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las ciencias naturales I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didactica de la Matemática I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didactica de la Matemática I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Introducción a la educación especial')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Introducción a la educación especial', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicología y aprendizaje II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicología y aprendizaje II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Neurociencias')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Neurociencias', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de las practicas del lenguaje II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de las practicas del lenguaje II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica de la Matemática II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica de la Matemática II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Didáctica y currículum.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Didáctica y currículum.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicologia del des y aprendizaje III')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicologia del des y aprendizaje III', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Abordaje Psicop. del sujeto con discap.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Abordaje Psicop. del sujeto con discap.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Lenguaje y com. en el suj. con discapac. int.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Lenguaje y com. en el suj. con discapac. int.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Sujeto con discapacidad intelectual.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Sujeto con discapacidad intelectual.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Currículum y discapacidad intelectual I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Currículum y discapacidad intelectual I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Atención temprana del desarrollo infantil.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Atención temprana del desarrollo infantil.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Producción de mat. y obj didácticos')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Producción de mat. y obj didácticos', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Historia, política y legislacion educ. argent.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Historia, política y legislacion educ. argent.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Campo de la practica docente III')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Campo de la practica docente III', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Interacciones sociales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Interacciones sociales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicopatologia')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicopatologia', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Currículum y discapacidad intelectual II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Currículum y discapacidad intelectual II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Taller de abordaje familiar en la escuela')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Taller de abordaje familiar en la escuela', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Formación laboral')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Formación laboral', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Multidiscapacidad')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Multidiscapacidad', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Educación y nuevas tecnologías.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Educación y nuevas tecnologías.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Política y legislación referida a la discapac.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Política y legislación referida a la discapac.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Reflexion filosófica de la educación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Reflexion filosófica de la educación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Dimensión ético política de la praxis doc.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Dimensión ético política de la praxis doc.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Campo de la práctica docente IV')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Campo de la práctica docente IV', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Tafo Lengua de señas.III')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Tafo Lengua de señas.III', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Tafo Lengua de señas IV')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Tafo Lengua de señas IV', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Contabilidad')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Contabilidad', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Principios de Adminstración')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Principios de Adminstración', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Economía')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Economía', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Metodología de la Investigación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Metodología de la Investigación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Computación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Computación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Sociología de la Organización')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Sociología de la Organización', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Derecho')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Derecho', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Matemática I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Matemática I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Marketing')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Marketing', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'EDI')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'EDI', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Matemática II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Matemática II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Estadística')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Estadística', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Inglés I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Inglés I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Computación II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Computación II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Contabilidad II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Contabilidad II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Derecho Laboral')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Derecho Laboral', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Administración de Personal')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Administración de Personal', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Derecho Comercial')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Derecho Comercial', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Administración de la Producción')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Administración de la Producción', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Práctica Profesional I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Práctica Profesional I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Inglés II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Inglés II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Técnica Impositiva y Laboral')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Técnica Impositiva y Laboral', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Régimen Tributario')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Régimen Tributario', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Comercio Internacional')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Comercio Internacional', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Costos y Presupuestos')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Costos y Presupuestos', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Administración Estratégica')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Administración Estratégica', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Administración Financiera')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Administración Financiera', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Práctica Profesional II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Práctica Profesional II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicología')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicología', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Teorías Socioculturales de la Salud')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Teorías Socioculturales de la Salud', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Condiciones y medio ambiente de trabajo')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Condiciones y medio ambiente de trabajo', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Salud Publica !')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Salud Publica !', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Biología Humana')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Biología Humana', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Fundamentos del Cuidado')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Fundamentos del Cuidado', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Cuidados de la Salud Centrados en la Comunidad y la Familia')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Cuidados de la Salud Centrados en la Comunidad y la Familia', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Práctica Profesionalizante I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Práctica Profesionalizante I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Comunicación en Ciencias de la Salud')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Comunicación en Ciencias de la Salud', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Inglés')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Inglés', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Introducción a la Metodología de Investigación en Salud')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Introducción a la Metodología de Investigación en Salud', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Nutrición y Dietoterapia')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Nutrición y Dietoterapia', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Salud Pública II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Salud Pública II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Farmacología en Enfermería')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Farmacología en Enfermería', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Enfermería Materno Infantil')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Enfermería Materno Infantil', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Enfermería del Adulto y del Adulto Mayor I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Enfermería del Adulto y del Adulto Mayor I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Práctica Profesionalizante II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Práctica Profesionalizante II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Enfermería en Salud Mental')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Enfermería en Salud Mental', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Organización y Gestión de Servicios de Enfermería')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Organización y Gestión de Servicios de Enfermería', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Enfermería Comunitaria y Práctica Educativa en Salud')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Enfermería Comunitaria y Práctica Educativa en Salud', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Enfermería en Emergencia y Catástrofes')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Enfermería en Emergencia y Catástrofes', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Práctica Profesionalizante III')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Práctica Profesionalizante III', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Salud Pública y Salud Mental')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Salud Pública y Salud Mental', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Contextualización del Campo Profes.del Acomp. T')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Contextualización del Campo Profes.del Acomp. T', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Principios Médicos y de Psicofarmacología')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Principios Médicos y de Psicofarmacología', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Fundamentos de Psic.Gral.y de Interv. Sociocom.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Fundamentos de Psic.Gral.y de Interv. Sociocom.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicología de los Ciclos Vitales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicología de los Ciclos Vitales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicopatología')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicopatología', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Modalidades de Intervenciónen el Acomp. Terap.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Modalidades de Intervenciónen el Acomp. Terap.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Modelo de ocupación humana')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Modelo de ocupación humana', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Etica')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Etica', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Acompañamiento Terapéutico')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Acompañamiento Terapéutico', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicologia de grupos')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicologia de grupos', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Sistemas Familiares')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Sistemas Familiares', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Psicofarmacología')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Psicofarmacología', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Practica Profesionalizante')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Practica Profesionalizante', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Investigación en salud')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Investigación en salud', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Organización y Gestión de los Servicios de Salud Menta')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Organización y Gestión de los Servicios de Salud Menta', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Intervención Comunitaria y Recursos Sociales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Intervención Comunitaria y Recursos Sociales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Acompañamiento Terapéutico en la Niñez y Adolescencia')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Acompañamiento Terapéutico en la Niñez y Adolescencia', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Acompañamiento Terapéutico del Adulto y el Adulto Mayor')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Acompañamiento Terapéutico del Adulto y el Adulto Mayor', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Prácticas Profesionalizantes III')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Prácticas Profesionalizantes III', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Administración finanaciera')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Administración finanaciera', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Administración y gestión de base de datos')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Administración y gestión de base de datos', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Análisis Matemático')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Análisis Matemático', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Contabilidad de gestion')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Contabilidad de gestion', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Costo y planificacion')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Costo y planificacion', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Costo y presupuestos')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Costo y presupuestos', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Fundamentos de matematica')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Fundamentos de matematica', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Gestión de inf. para administración.')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Gestión de inf. para administración.', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ingles I')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ingles I', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Ingles II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Ingles II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Introducción a la programación')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Introducción a la programación', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Introducción a las redes de datos')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Introducción a las redes de datos', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Laboratorio de Hardware')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Laboratorio de Hardware', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Matematica para administración')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Matematica para administración', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Planificacion y estrategica')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Planificacion y estrategica', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Practica profesional II')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Practica profesional II', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Practicas profesional II.Diseño de proy de  adm')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Practicas profesional II.Diseño de proy de  adm', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Principios de administración')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Principios de administración', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Principios de contabilidad')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Principios de contabilidad', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Sistemas Digitales')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Sistemas Digitales', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Sistemas operativos')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Sistemas operativos', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Técnica impositiva y laboral')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Técnica impositiva y laboral', N'Presencial', 1);
IF NOT EXISTS (SELECT 1 FROM Materias WHERE MaDenominacion = N'Tecnologias y sistemas para la administración')
    INSERT INTO Materias (MaDenominacion, MaModalidad, MaCantModulos) VALUES (N'Tecnologias y sistemas para la administración', N'Presencial', 1);

SELECT (SELECT COUNT(*) FROM Carreras) AS TotalCarrerasAhora, (SELECT COUNT(*) FROM Materias) AS TotalMateriasAhora;
