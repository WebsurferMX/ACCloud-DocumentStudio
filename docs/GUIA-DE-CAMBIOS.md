# Guía de cambios — ACCloud DocumentStudio

Lista los cambios que afectan a quien consume la API o trabaja en el código, con lo que hay que hacer en cada caso.
El detalle técnico y cronológico está en [BITACORA.md](BITACORA.md).

---

## 0.2.0 — 2026-11-25

### ⚠️ Cambio que rompe compatibilidad: los elementos requieren `type`

`POST /api/render` ahora exige que cada objeto de `template.elements` traiga un campo `type`.
Antes, **cualquier** petición con elementos fallaba con 500, así que no hay clientes funcionando que romper. Aun así, el contrato cambió.

**Antes (fallaba siempre):**

```json
{ "value": "Hola {{nombre}}", "fontSize": 14 }
```

**Ahora:**

```json
{ "type": "text", "value": "Hola {{nombre}}", "fontSize": 14 }
```

Reglas:

| Regla | Detalle |
|---|---|
| Valores válidos de `type` | `"text"` (único por ahora) |
| Posición | `type` debe ser la **primera propiedad** del objeto |
| Sin `type` o con valor desconocido | La API responde error (hoy 500, ver pendientes) |

Ejemplo completo:

```http
POST /api/render
Content-Type: application/json

{
  "template": {
    "page": { "width": 600, "height": 800 },
    "elements": [
      { "type": "text", "value": "Hola {{cliente.nombre}}", "fontSize": 14 }
    ]
  },
  "data": { "cliente": { "nombre": "Mundo" } }
}
```

Respuesta: `200 OK`, `application/pdf`, archivo `document.pdf`.

### Cómo agregar un nuevo tipo de elemento

1. Crear la clase en `TemplateEngine/Models/`, heredando de `ElementDefinition`.
2. Registrarla en `ElementDefinition.cs`:
   ```csharp
   [JsonDerivedType(typeof(ImageElementDefinition), "image")]
   ```
3. Manejarla en `TemplateCompiler.Compile` y en `QuestPdfRenderer`.
4. Agregar un caso en `TemplateDefinitionJsonTests`.

### Reorganización de carpetas (sin cambios de namespace)

Los archivos del Renderer se movieron para que la carpeta coincida con el namespace que ya declaraban.
**No cambió ningún `namespace` ni `using`**, así que el código que los consume no necesita ajustes.

| Antes | Ahora |
|---|---|
| `Renderer/Render/Interfaces/IDocumentRenderer.cs` | `Renderer/Interfaces/IDocumentRenderer.cs` |
| `Renderer/Render/Layout/ILayoutEngine.cs` | `Renderer/Layout/ILayoutEngine.cs` |
| `Renderer/Render/Models/SimpleLayoutEngine.cs` | `Renderer/Layout/SimpleLayoutEngine.cs` |
| `Renderer/Render/Services/QuestPdfRenderer.cs` | `Renderer/Services/QuestPdfRenderer.cs` |
| `Api/primer-documento.pdf` | `samples/primer-documento.pdf` |

Archivos eliminados (estaban vacíos o eran plantillas de Visual Studio):

- `Renderer/Render/Interfaces/SimpleLayoutEngine.cs` (vacío, duplicaba el nombre del real)
- `TemplateEngine/Services/TestCompiler.cs` (vacío)
- `Class1.cs` en Application, Contracts, Domain, Persistence, Renderer y TemplateEngine
- `tests/.../UnitTest1.cs` (test vacío)
- Carpeta `ACCloud PDF Builder/` (vacía)

### Solución (`.slnx`)

`ACCloud.DocumentStudio.slnx` solo incluía el proyecto de tests. Ahora incluye los 7 proyectos de `src/`, así que `dotnet build ACCloud.DocumentStudio.slnx` compila todo y Visual Studio los muestra.

### Estructura actual

```
ACCloud DocumentStudio/
├── ACCloud.DocumentStudio.slnx
├── docs/            ← esta guía y la bitácora
├── samples/         ← PDFs de ejemplo
├── src/
│   ├── ACCloud.DocumentStudio.Api/            Controllers, Program.cs
│   ├── ACCloud.DocumentStudio.Application/    (vacío, reservado)
│   ├── ACCloud.DocumentStudio.Contracts/      Render/RenderRequest.cs
│   ├── ACCloud.DocumentStudio.Domain/         Entities, Enums, Interfaces
│   ├── ACCloud.DocumentStudio.Persistence/    (vacío, reservado)
│   ├── ACCloud.DocumentStudio.Renderer/       Interfaces, Layout, Models, Services
│   └── ACCloud.DocumentStudio.TemplateEngine/ Interfaces, Models, Services
└── tests/
    └── ACCloud.DocumentStudio.UnitTests/
```

---

## Pendientes conocidos

| # | Tema | Impacto |
|---|---|---|
| 1 | `QuestPdfRenderer` ignora `X`, `Y`, `Width`, `Height` y el tamaño de página; apila los textos en una columna. | El PDF no respeta el diseño de la plantilla. |
| 2 | Un JSON inválido (sin `type`, tipo desconocido) devuelve **500** con stack trace en lugar de **400**. | Mensajes de error poco claros para el cliente; expone detalles internos. |
| 3 | `TemplateEngine` depende de `Renderer` (el compilador devuelve `RenderTextElement`). | Acopla capas; convendría un modelo intermedio o invertir la dependencia. |
| 4 | `ILayoutEngine` / `SimpleLayoutEngine` no se usan ni están registrados en DI. | Código muerto por ahora. |
| 5 | `Application` y `Persistence` están vacíos; `ITemplateRepository` no tiene implementación. | Las plantillas solo pueden llegar en el body del POST. |
| 6 | `type` debe ir primero en cada elemento. | Se puede relajar con `AllowOutOfOrderMetadataProperties = true` en `AddJsonOptions`. |
