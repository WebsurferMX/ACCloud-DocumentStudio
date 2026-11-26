# Bitácora del sistema — ACCloud DocumentStudio

Registro cronológico de diagnósticos, cambios y verificaciones. Lo más reciente va arriba.
Para el resumen orientado a consumidores de la API, ver [GUIA-DE-CAMBIOS.md](GUIA-DE-CAMBIOS.md).

Formato de cada entrada: **Contexto → Diagnóstico → Acción → Verificación → Pendiente**.

---

## 2026-11-25 — Reorganización de carpetas y limpieza

**Contexto.** La carpeta del Renderer tenía un nivel `Render/` que no coincidía con los namespaces declarados (`Renderer.Interfaces`, `Renderer.Layout`, `Renderer.Services`). `SimpleLayoutEngine` estaba en `Render/Models/`, pero su namespace es `Layout`, y en `Render/Interfaces/` había otro `SimpleLayoutEngine.cs` vacío. La solución `.slnx` solo listaba el proyecto de tests.

**Acción.**

- Respaldo previo del proyecto (sin `bin/` ni `obj/`) en `respaldo-antes-reorganizar.tgz`, en el directorio temporal de la sesión.
- Movidos:
  - `Renderer/Render/Interfaces/IDocumentRenderer.cs` → `Renderer/Interfaces/`
  - `Renderer/Render/Layout/ILayoutEngine.cs` → `Renderer/Layout/`
  - `Renderer/Render/Models/SimpleLayoutEngine.cs` → `Renderer/Layout/`
  - `Renderer/Render/Services/QuestPdfRenderer.cs` → `Renderer/Services/`
  - `Api/primer-documento.pdf` → `samples/`
- Eliminados (vacíos o de plantilla): `Render/Interfaces/SimpleLayoutEngine.cs`, `TemplateEngine/Services/TestCompiler.cs`, los seis `Class1.cs`, `UnitTest1.cs` y la carpeta vacía `ACCloud PDF Builder/`.
- `ACCloud.DocumentStudio.slnx`: agregados los 7 proyectos de `src/` en la carpeta de solución `/src/`.
- Creadas las carpetas `docs/` y `samples/`.
- No se modificó ningún `namespace` ni `using`.

**Verificación.**

- `dotnet build ACCloud.DocumentStudio.slnx` → 0 errores, 0 advertencias.
- `dotnet test` → 3/3 pruebas correctas.
- `POST /api/render` de punta a punta → `200 application/pdf`, 6497 bytes.

**Pendiente.**

- Queda la carpeta vacía `src/ACCloud.DocumentStudio.Renderer/Render/Interfaces/`: Windows la reportó en uso ("Device or resource busy"), probablemente porque el editor la tiene abierta. Se puede borrar a mano después de cerrar VS Code. No afecta la compilación.

---

## 2026-11-25 — `POST /api/render` no generaba el PDF

**Contexto.** Al hacer POST a `/api/render` no se generaba el documento.

**Diagnóstico.**

- Se levantó la API y se envió un POST con un elemento de texto. Respuesta: **500 Internal Server Error**:
  ```
  System.NotSupportedException: Deserialization of interface or abstract types is not supported.
  Type 'ACCloud.DocumentStudio.TemplateEngine.Models.ElementDefinition'. Path: $.template.elements[0]
  ```
- Causa: `TemplateDefinition.Elements` es `IList<ElementDefinition>` y `ElementDefinition` es `abstract`. System.Text.Json no sabe qué clase concreta crear, así que falla al deserializar el body, **antes** de entrar al controller. Ni `TemplateCompiler` ni `QuestPdfRenderer` llegaban a ejecutarse.

**Acción.**

- `TemplateEngine/Models/ElementDefinition.cs`: agregados
  `[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = FailSerialization)]` y
  `[JsonDerivedType(typeof(TextElementDefinition), "text")]`.
- `Api/ACCloud.DocumentStudio.Api.http`: reemplazado el ejemplo `GET /weatherforecast` (de la plantilla, ya no existe) por un `POST /api/render` válido.
- `tests/.../TemplateDefinitionJsonTests.cs` (nuevo):
  - Deserializa un elemento con `"type": "text"` y comprueba que sea `TextElementDefinition` con sus valores.
  - Comprueba que un elemento sin `type` sea rechazado.

**Verificación.**

- Mismo POST con `"type": "text"` → **200 OK**, `application/pdf`, PDF válido (`%PDF-1.4`).
- Pruebas unitarias correctas.

**Pendiente.**

- Errores de JSON devuelven 500 con stack trace; deberían ser 400 (ver pendientes en la guía de cambios).
- El renderer ignora las coordenadas y el tamaño de página.

---

## Estado del sistema al 2026-11-25

| Componente | Estado |
|---|---|
| API (`/api/render`) | Funcional para elementos de texto |
| TemplateEngine (resolver `{{ruta.propiedad}}`) | Funcional, con prueba |
| Renderer (QuestPDF, licencia Community) | Funcional; layout en columna, sin posicionamiento |
| Layout engine | Definido, sin uso |
| Domain (Template, TemplateVersion) | Definido, sin uso |
| Application / Persistence | Vacíos |
| Pruebas unitarias | 3 pruebas, todas correctas |

**Stack:** .NET 10, ASP.NET Core con controllers, Swashbuckle 11.0.1, QuestPDF 2026.9.2, xUnit 2.9.3.
**Puertos locales:** http 5176, https 7184.
