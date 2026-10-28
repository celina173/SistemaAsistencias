// AppFactory pasa la base descartable a la app por una variable de entorno, que es de todo el proceso:
// dos fábricas a la vez se pisarían la cadena de conexión. Las clases de prueba corren de a una.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
