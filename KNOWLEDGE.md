# FormaterXJ - Documento de conocimiento

## Resumen
FormaterXJ es una aplicación de escritorio multiplataforma desarrollada con Avalonia UI y .NET. Su objetivo es detectar automáticamente si el contenido pegado es JSON o XML, formatearlo de manera legible, mostrar la estructura en un árbol y permitir buscar texto dentro del documento sin enviar datos a servidores externos.

## Arquitectura del proyecto
- `Program.cs`: punto de entrada de la aplicación. Configura Avalonia y arranca la ventana principal.
- `App.axaml.cs`: carga el árbol XAML de la app y asigna la ventana principal.
- `App.axaml`: define el estilo global y el `ViewLocator` de Avalonia.
- `Views/MainWindow.axaml`: barras Archivo/Opciones/Formatear, editor con margen opcional de líneas, árbol y controles de búsqueda.
- `Views/MainWindow.axaml.cs`: lógica de UI y carga de archivo.
- `ViewModels/MainViewModel.cs`: estado de la vista, detección de formato, formateo, árbol y navegación de búsquedas.
- `Models/DocumentNode.cs`: nodos del árbol con propiedades, detalle y resaltado de coincidencias.
- `Models/DocumentResult.cs`: resultado de análisis de formato.
- `Services/DocumentFormatter.cs`: detección y formateo de JSON/XML.
- `Services/JsonTreeBuilder.cs`: construcción del árbol para JSON.
- `Services/XmlTreeBuilder.cs`: construcción del árbol para XML.
- `Services/SearchService.cs`: búsqueda insensible a mayúsculas y minúsculas.
- `FormaterXJ.Tests/DocumentFormatterTests.cs`: pruebas unitarias de detección, formateo, árbol y búsqueda.

## Tecnologías principales
- C# con .NET 10
- Avalonia UI 12.x
- `System.Text.Json` para JSON
- `XDocument` y `System.Xml` para XML
- CommunityToolkit.Mvvm para el patrón MVVM
- xUnit para pruebas unitarias

## Comandos de uso
### Restaurar dependencias
```bash
dotnet restore
```

### Compilar la aplicación
```bash
dotnet build FormaterXJ.csproj
```

### Ejecutar la aplicación
```bash
dotnet run --project FormaterXJ.csproj
```

### Ejecutar pruebas
```bash
dotnet test FormaterXJ.Tests/FormaterXJ.Tests.csproj
```

## Behaviors clave
- La ventana principal tiene dos paneles: el editor JSON/XML a la izquierda y el árbol de estructura a la derecha.
- Los menús Archivo/Opciones se muestran integrados en el área de título de Avalonia; el espacio de esa franja permite arrastrar la ventana.
- El divisor vertical entre el editor y el árbol se puede arrastrar para cambiar el ancho de ambos paneles.
- Al seleccionar un nodo del árbol, el editor selecciona y resalta su bloque completo (objeto/arreglo JSON o elemento XML; propiedad/atributo completa); al mover la selección en el editor, se selecciona y desplaza a la vista el nodo correspondiente del árbol.
- El resaltado del texto seleccionado en el editor usa un color visible y permanece visible cuando el editor pierde el foco; al enfocar un nodo se establece primero el caret y después el rango de selección para evitar colapsarlo.
- La sincronización de la selección valida el rango del caret y solo se activa mientras el texto coincide con el último documento formateado.
- El formateo se ejecuta automáticamente al pegar contenido en el editor o cargar un archivo desde `Archivo > Abrir`; escribir normalmente no dispara el análisis.
- Abrir documento, guardar el resultado formateado y cerrar están en el menú `Archivo`.
- El menú `Opciones > Tema` ofrece los temas oscuro, claro y del sistema; oscuro es el tema inicial.
- El menú `Opciones > Vista` agrupa los tamaños de letra independientes (10–18 pt) y la opción de numeración de líneas del editor.
- Los textos y la franja de numeración usan recursos dinámicos del tema para conservar contraste en modo claro y oscuro; el check de numeración actualiza explícitamente el margen del editor.
- El menú `Vista` permite configurar tamaños de letra independientes (10–18 pt) para editor y árbol, y activar/desactivar la numeración de líneas del editor (activada por defecto).
- La franja numerada se muestra a la izquierda del editor, permanece separada del contenido y sincroniza su desplazamiento vertical; al formatear, refleja las líneas del texto formateado.
- Al formatear correctamente, el editor se reemplaza por el contenido formateado; el árbol refleja el documento analizado.
- Si el contenido está vacío, muestra un mensaje claro y no borra el texto original.
- Si el documento no es JSON ni XML válido, se informa el error sin bloquear la aplicación.
- La estructura se presenta en un árbol con nodos expandibles y contraíbles.
- La búsqueda busca en nombres, atributos, rutas y valores, ignorando mayúsculas/minúsculas.
- La coincidencia actual se resalta y se puede navegar con `Anterior` y `Siguiente`.
- La lógica está separada entre UI, modelos, servicios y pruebas.

## Casos cubiertos
- JSON válido
- XML válido
- JSON/XML con atributos
- contenido inválido
- contenido vacío
- arreglos JSON
- búsqueda sin coincidencias
- búsquedas con resultados múltiples

## Notas importantes
- El proyecto no usa servicios externos ni guarda archivos en red.
- Todo el procesamiento se realiza localmente.
- No se realizan reformateos automáticos mientras el usuario escribe.
- La app está diseñada para funcionar en Windows, macOS y Linux mediante Avalonia.
