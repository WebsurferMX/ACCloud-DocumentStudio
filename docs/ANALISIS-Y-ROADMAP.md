# Análisis de referencia y hoja de ruta — ACCloud DocumentStudio

**Objetivo:** definir qué hace falta para que DocumentStudio sea una plataforma de generación de documentos por plantillas con API, del mismo tipo que los SaaS del mercado (se tomó CraftMyPDF como referencia), partiendo del código que ya existe.

**Alcance:** se analizaron funciones y arquitectura **observables públicamente**. No se copió código, diseño visual ni marca; los nombres de rutas propuestos abajo son propios.

Fecha: 2026-11-25.

---

## 1. Fuentes y método

| Fuente | Qué aportó | Tipo de evidencia |
|---|---|---|
| Referencia pública de la API (`craftmypdf.com/docs`), renderizada en navegador e inspeccionada con REA (`inspect_web_page`, evidencia `ev_fa31c1c3…68828b`) | Lista completa de endpoints, nombres de campos, regiones | Observado (árbol de accesibilidad de la página) |
| Páginas públicas: features, pricing, PDF API, e-sign, seguridad, enterprise | Componentes del editor, planes, créditos, retención, arquitectura declarada | Observado (texto de marketing) |
| Captura del dashboard proporcionada por el usuario | Secciones de la app: Dashboard, Manage Templates, Signed Documents, API Integration, Subscription | Observado |

**No observado:** el dashboard y el editor requieren sesión. REA solo vio la página de login, así que el funcionamiento interno del editor **no se analizó**. Lo que se dice del editor sale de las páginas públicas.

---

## 2. Qué ofrece un producto de este tipo (referencia)

### 2.1 API (26 operaciones observadas)

| Grupo | Operaciones | Notas |
|---|---|---|
| Generación de PDF | crear (síncrono), crear asíncrono, crear a partir de varias plantillas (merge), crear en paralelo | Asíncrono con `webhook_url` |
| Imagen | crear imagen desde plantilla | Salida `png` / `jpeg` |
| Plantillas | listar, listar versiones, conservar versiones, crear desde otra, transferir, actualizar, obtener, eliminar, uso por plantilla | Versionado en cada guardado |
| Editor embebido | crear y desactivar sesión de editor | Devuelve una URL del editor para incrustar en apps de terceros |
| Cuenta | transacciones, información de cuenta | Consumo de créditos |
| Manipulación de PDF | unir PDFs por URL, agregar texto, llenar campos de formulario, info de PDF, marca de agua | Operan sobre PDFs existentes |

**Campos relevantes al generar** (observados): `template_id`, `version`, `data` o `load_data_from` (URL), `export_type` (`json` → URL / `file` → binario), `expiration`, `output_file`, `direct_download`, `password_protected` / `password`, `pdf_version`, `resize_images` (`resize_max_width`, `resize_max_height`, `resize_format`), `image_resample_res`, `cloud_storage` + `postaction_s3_bucket` / `postaction_s3_filekey`, `webhook_url`, `merge_expiration`.

**Factura electrónica** (observado): `einvoice`, `einvoice_xml`, `einvoice_format`, `einvoice_profile`, `einvoice_version`, `einvoice_validate`.

**Autenticación:** header con API key. **Regiones:** varias URLs base por región (US, DE, AU y alternas).

### 2.2 Editor y plantillas

- **Componentes:** texto, imagen, tabla, QR, código de barras, formas, gráficas, campos de formulario (texto, checkbox, dropdown, radio) y firma.
- **Propiedades por elemento:** X, Y, ancho, alto, campo de datos, estilo, opacidad.
- **Datos:** expresiones `{{ cliente.nombre }}`; tablas ligadas a un arreglo; JSON anidado; formateo de fechas, monedas y números; funciones JavaScript personalizadas.
- **Flujo:** vista previa con JSON de prueba; cada guardado crea una versión; se puede generar desde cualquier versión.

### 2.3 Flujos de creación de plantillas (observados en la app con sesión)

Capturas de "Manage Templates" proporcionadas por el usuario, 2026-11-25:

| Flujo | Qué hace | Detalles observados |
|---|---|---|
| Nueva plantilla PDF | Parte de una **galería** de plantillas listas (facturas, recibos, certificados…) o de una en blanco | Pide nombre y plantilla inicial; cada tarjeta muestra el tamaño en **mm** y en px (ej. 286×286 mm, 1080×1080) |
| Plantilla desde PDF (IA, beta) | Se sube un PDF; la IA detecta los campos y arma la plantilla | Modo **Static** (texto encima que muestra `{{ data.field }}`) o **Fillable** (campos editables); también "solo usar el PDF como fondo"; procesa 3 páginas en Free y 20 en planes de pago; tarda de 8 a 60 s |
| Importar de Canva (beta) | Importa un diseño de Canva desde su enlace público | Opción de copiar imágenes y fuentes al almacenamiento propio |
| Nueva plantilla de imagen | Editor de imágenes con galería (redes sociales, webinars, vales) | Mismo modelo de datos, con salida PNG/JPEG |

**Concepto de secciones** (títulos de los artículos de ayuda): "Multi-page PDF report with Normal Sections" y "Data-bound Normal Section to repeat an array". La plantilla se organiza en **secciones o bandas** (encabezado, cuerpo, pie) y una sección puede **ligarse a un arreglo** para repetirse por cada elemento, con paginación automática. Es el mismo modelo de bandas de los generadores de reportes clásicos.

**Bundle del editor:** REA confirmó que la app se renderiza en el servidor (login con `?next=`, estilos Bulma) y que el editor vive en un solo bundle de 5.7 MB (`all_apps-bundle.js`). REA no pudo procesarlo dentro del límite de tiempo (evidencia `ev_e9f42797…a334506`, 13 de 14 scripts completos) y no expone rutas de cliente.

### 2.4 El diseñador por dentro (observado con REA)

Plantilla de ejemplo "Packing List" abierta en `/designer/?template_id=…`. Inspección pasiva con REA: captura de pantalla y árbol de accesibilidad, evidencia `ev_32cc7d78c1d661220fbb6613c1bb9a265de5df80c44b8f7e357abb9bb869a9c9`. No se hizo clic ni se modificó nada. Las peticiones de red se hicieron antes de conectarse, así que **no se observó el formato JSON interno** de la plantilla.

**Estructura de la pantalla**

| Zona | Contenido |
|---|---|
| Pestañas | Designer, Preview, Data (JSON de prueba), Settings; además Template Versions y Docs: Expressions |
| Barra | Save, Layers, Undo/Redo, alinear/distribuir, zoom de 10% a 400% |
| Panel izquierdo | **Components** (paleta) y **Data Fields**: árbol de campos con su tipo (texto, fecha, número, imagen, **arreglo** con hijos), sacado del JSON de prueba y arrastrable al lienzo |
| Lienzo | Regla en **cm**; secciones apiladas; cada sección cambia de altura arrastrando |
| Panel derecho | Propiedades del componente seleccionado ("General") |
| Capas | Lista por sección con orden Z, bloquear, mostrar/ocultar, duplicar y eliminar |

**Secciones** (confirmado por el recorrido guiado del editor): **Header**, **Normal** (puede haber varias, con nombre) y **Footer**. El editor indica cuando una sección tiene *data-binding* activo ("is enabled for the section… to repeat, configure data-binding"): la sección completa se repite por cada elemento de un arreglo.

**Componentes vistos en la plantilla:** `Text`, `Image`, `Rectangle`, `Line`, `Qr-code`, `Bar-code-v2`. Cada uno tiene un nombre automático (`text_8`, `qr_code_1`…) y pertenece a una sección.

**Expresiones vistas:**

| Expresión | Significado |
|---|---|
| `{{data.company_name}}` | Campo del JSON raíz (prefijo `data.`) |
| `{{ ifNull(data.order_date, "N/A") }}` | Función con valor por defecto |
| `{{row.qty_ordered}}`, `{{row.tracking_id}}` | Campo del elemento actual dentro de una sección repetida (prefijo `row.`) |
| `{{rowIndex}}` (inferido del texto cortado `{{rowInd…`) | Número del renglón |
| `{{sum("qty_ordered")}}` (inferido del texto cortado) | Agregado sobre el arreglo, en una sección de totales |
| `{{sys.pageNumber}}` / `{{sys.totalPages}}` | Variables del sistema para paginación |

Los bindings también se usan **dentro** de componentes que no son texto: el código de barras toma `{{row.tracking_id}}` y la imagen por renglón toma `row.image_url`.

**Qué implica para DocumentStudio**

1. El modelo de secciones de la Fase 1 queda confirmado: `header`, `sections[]` (con `dataSource` opcional) y `footer`.
2. Contexto de expresiones con tres raíces: `data`, `row` (con `rowIndex`) y `sys` (`pageNumber`, `totalPages`). Hoy nuestro resolver solo conoce la raíz del JSON.
3. Funciones en expresiones: mínimo `ifNull`, `sum`, `count` y formatos de fecha y moneda.
4. Cada elemento necesita `id`/`name`, `zIndex`, `locked` y `visible` (para el editor) además de X, Y, W, H.
5. El editor necesita un **JSON de prueba por plantilla** (pestaña Data), del que se deriva el árbol de campos. Hay que agregarlo a `TemplateVersion`.
6. `{{sys.totalPages}}` exige render en dos pasadas o soporte nativo. QuestPDF lo trae: `CurrentPageNumber()` y `TotalPages()`.

### 2.5 Plataforma

- Workspaces y equipos con permisos.
- Planes con cuota mensual de "créditos" (1 PDF = 1 crédito; operaciones menores, 0.5; firma, 10), con avisos al 80% y 100%.
- Archivos en S3 con URL prefirmada que expira. Logs de API retenidos 14 días.
- Firma electrónica (sobres, roles, orden de firma, estados), con un motor de terceros.
- Integraciones no-code (Zapier, Make, n8n, etc.).

---

## 3. Comparación con DocumentStudio hoy

| Capacidad | DocumentStudio (2026-11-25) | Brecha |
|---|---|---|
| Endpoint de render | `POST /api/render` con plantilla y datos en el body | Falta render por `templateId` / versión |
| Modelo de plantilla | JSON con `page` y elementos polimórficos (`type`) | Base correcta, extensible con `[JsonDerivedType]` |
| Elementos | Solo `text` | Faltan imagen, tabla, línea/rectángulo, QR, código de barras |
| Posicionamiento | Se guardan `X/Y/Width/Height`, pero el renderer **los ignora** (columna) | **Brecha principal**: render con posición absoluta |
| Tamaño de página | Se guarda, pero no se usa | Aplicar `page.Size(...)` |
| Expresiones | `{{a.b.c}}` sin formato | Faltan formateadores, condicionales y tablas sobre arreglos |
| Plantillas guardadas | Entidades `Template` / `TemplateVersion` y `ITemplateRepository` (sin implementación) | Persistencia + CRUD + versiones |
| Capa de aplicación | Proyecto vacío | Casos de uso (Render, CRUD) |
| Salida | Binario directo | Falta URL con expiración, almacenamiento, imagen |
| Asíncrono / webhook | No | Cola de trabajos + webhook |
| Manipulación de PDF | No | Merge, marca de agua, contraseña |
| Autenticación / cuotas | No | API keys, workspaces, consumo |
| Errores | 500 con stack trace | `400 ProblemDetails` |
| Editor visual | No | Diseñador web que produzca el JSON de plantilla |

**Lo que ya sirve como base:**

- el modelo JSON polimórfico, que encaja con un editor de lienzo (cada elemento tiene posición y tamaño);
- el resolver de expresiones;
- QuestPDF, que soporta capas, traslación absoluta, imágenes, tablas, SVG y generación de imágenes;
- las entidades de dominio con versionado.

---

## 4. Hoja de ruta propuesta

Cada fase se puede entregar y probar por separado. El orden prioriza lo que vuelve útil el producto lo antes posible.

### Fase 1 — Motor de render fiel a la plantilla ✅ (0.3.0, 2026-11-25)

> Hecha, junto con la parte básica de la Fase 6 (convertidor CFDI, plantilla de factura, QR del SAT, importe con letra). Los detalles están en la guía de cambios 0.3.0. Quedan fuera: altura automática del texto y alternar color de renglones.
0. **Modelo de secciones**: `TemplateDefinition` pasa de una lista plana de elementos a `header`, `footer` y `sections[]`. Cada sección tiene altura y elementos con posición relativa a ella, y opcionalmente `dataSource: "{{conceptos}}"` para repetirse por cada elemento del arreglo; dentro de ella, `{{item.campo}}`. Unidades en **mm** (se convierten a puntos al renderizar). Con QuestPDF: `page.Header()` / `page.Footer()` y una `Column` de secciones, cada una con capas absolutas. QuestPDF pagina automáticamente.
1. Renderer con **posición absoluta**: `page.Size(width, height)` en puntos y cada elemento en una capa con `TranslateX/TranslateY` y tamaño fijo.
2. Nuevos elementos:
   - `image` (URL o base64; ajuste *fit* o *fill*);
   - `line` y `rect` (borde, relleno, grosor);
   - `table` ligada a un arreglo (`{{conceptos}}`), con columnas, encabezado y salto de página;
   - `barcode` y `qr`, con ZXing.Net generando SVG.
3. Expresiones: raíces `data`, `row`, `rowIndex` y `sys` (página actual y total de páginas); funciones `ifNull`, `sum`, `count`, `formatDate`, `formatCurrency`; propiedad `visibleIf` por elemento.
4. Errores de validación como `400 ProblemDetails`.
5. Pruebas: snapshot del árbol de render y pruebas de los formateadores.

### Fase 2 — Plantillas y versiones
1. `Persistence` con EF Core (SQL Server o PostgreSQL): tablas `Templates` y `TemplateVersions`, con la definición en JSON.
2. `Application`: casos de uso `CreateTemplate`, `UpdateTemplate` (crea versión), `GetTemplate`, `ListVersions`, `RenderDocument`.
3. Rutas propias (REST):
   - `POST/GET/PUT/DELETE /api/v1/templates`
   - `GET /api/v1/templates/{id}/versions`
   - `POST /api/v1/documents` con `{ templateId, version?, data, output }`
4. Romper la dependencia `TemplateEngine → Renderer` con un modelo intermedio en `TemplateEngine` (o en un proyecto `Rendering.Abstractions`).

### Fase 3 — Entrega y trabajos asíncronos
1. `output: "file" | "url"`: almacenamiento en Azure Blob, S3 o disco local, con URL firmada y expiración.
2. Modo asíncrono: cola (Channels o Hangfire), estado del trabajo y `webhookUrl` al terminar.
3. Salida como imagen (QuestPDF `GenerateImages`).
4. Operaciones sobre PDF: unir varias plantillas en un documento, marca de agua y contraseña. **Verificar** que la versión de QuestPDF incluya `DocumentOperation` (merge y cifrado); si no, usar PdfSharp o qpdf.

### Fase 4 — Cuentas, API keys y consumo
1. Workspaces (multi-tenant) y API keys con hash, regenerables. Autenticación por header.
2. Registro de transacciones (cada render) y endpoint de consumo.
3. Cuotas por plan y límites de tasa (`AddRateLimiter`).
4. Retención configurable de archivos y logs.

### Fase 5 — Editor visual
1. Diseñador web (React o Blazor): lienzo con arrastrar y soltar, panel de propiedades (X, Y, W, H, binding, estilo) y lista de componentes. Su resultado es el mismo JSON de `TemplateDefinition`.
2. Vista previa en vivo: `POST /api/v1/preview` con plantilla y datos de prueba.
3. Editor embebible con token de sesión de corta duración.
4. Dashboard: plantillas, consumo y API keys.

### Fase 6 — Diferenciador: CFDI (mercado mexicano)
La referencia soporta facturas electrónicas europeas (Factur-X / ZUGFeRD). El equivalente para nuestro mercado:
1. `data` puede ser un **XML CFDI 4.0**, que se convierte a JSON con un modelo conocido (emisor, receptor, conceptos, impuestos, complemento de timbre).
2. Elementos especiales: QR del SAT (URL de verificación con UUID, RFCs, total y sello) y cadena original del timbre.
3. Plantillas base en una **galería** (como la de la referencia): factura, nota de crédito, complemento de pago y nómina.
4. Opción de anexar el XML al PDF.
5. Más adelante: **PDF existente como fondo** (formatos oficiales) con campos encima. La detección automática de campos con IA queda como mejora posterior.

Encaja con el resto del ecosistema (CFDI Manager) y la referencia no lo ofrece para México.

### Fuera de alcance por ahora
- **Firma electrónica:** en la referencia depende de un proveedor externo. Conviene evaluarla después como integración.
- **Regiones múltiples e integraciones no-code:** cuando haya clientes que lo pidan.

---

## 5. Decisiones abiertas

| Tema | Opciones | Recomendación |
|---|---|---|
| Base de datos | SQL Server / PostgreSQL | La que ya usa la infraestructura de ACCloud |
| Almacenamiento de archivos | Azure Blob / S3 / disco | Interfaz `IFileStorage` con disco local para desarrollo |
| Editor | React / Blazor | Depende del equipo de frontend; el contrato JSON es el mismo |
| Cola asíncrona | Channels (en proceso) / Hangfire / servicio de colas | Channels en Fase 3; migrar cuando haya más de una instancia |
