# Módulos del proyecto y quién se encarga

```
  Meta Quest 3 (Unity)                       Laptop de Daniel                      Navegador (docente / jurado)
 ┌──────────────────────┐   POST /api/attempts  ┌─────────────────────┐   GET /api/attempts  ┌──────────────────────┐
 │ Escena en passthrough│ ───────────────────▶  │ Servidor (API)      │ ◀──────────────────  │ Panel del docente    │
 │ palanca · candado ·  │                       │ guarda · explica    │                      │ intentos · errores · │
 │ verificar · máquina  │ ◀───────────────────  │ CSV                 │                      │ gráfico · CSV        │
 │ Árbitro (Loto.Core)  │     explicación       └─────────────────────┘                      └──────────────────────┘
 └──────────────────────┘
```

| Módulo | Carpeta | Qué hace | Dueño | Estado (sáb 21/11, mediodía) |
|---|---|---|---|---|
| **Árbitro** (lógica) | `unity/Assets/Loto/Core/` | Decide si cada acción va en orden; C# puro, sin Unity | Daniel / Claude | Listo: 174 pruebas, 0 fallas |
| **Visor** (escena y scripts) | `unity/Assets/Loto/Runtime/`, `Editor/` | Palanca, candado, botones, máquina, panel en el visor, alfiler, sonido, envío al servidor | Daniel / Claude | Compila; probado en Play (pasos 1-2); falta el visor |
| **Mesa real + modelos 3D** | `unity/Assets/Loto/` (por aplicar) | La máquina se apoya en una mesa real (MRUK) y usa modelos de Kenney (CC0) | Claude | Escrito y compilado fuera de Unity; se aplica después del primer APK |
| **Contrato** | `docs/CONTRATO.md` | Qué viaja entre visor y servidor | Todos (cambia solo con aviso) | Fijo |
| **Servidor** (API) | `server/` (Denzel) o `server-mock/server.js` | `POST/GET/DELETE /api/attempts`, CSV, textos de error | Denzel | El de prueba anda; el definitivo lo decide Denzel |
| **Panel del docente** | `server-mock/public/index.html` (o el de `server/`) | Lo que el docente y el jurado ven en pantalla grande | **Desarrollador web** | Funciona, pero sin diseño de producto |
| **Página del proyecto** | `web/` (nueva) | Página pública para el jurado: problema, demo, equipo | **Desarrollador web** | No existe |
| **Presentación** | `pitch/presentacion/` | 8 láminas con el tablero en 3D (three.js, sin internet) | Claude | Lista; se ajusta con los resultados del sábado |
| **Pitch y documentos** | `docs/` | Guion, preguntas del jurado, diapositivas, video, pruebas | Claude / Daniel | Listos y auditados |

## Puntos de contacto entre módulos

- **Web ↔ Servidor:** solo por la API del contrato. El panel **no** cambia la API; si necesita un dato nuevo, lo
  calcula con lo que ya devuelve `GET /api/attempts` (alcanza para todo lo de [`TAREAS-WEB.md`](TAREAS-WEB.md)).
- **Web ↔ Visor:** ninguno directo. El visor manda; el panel lee cada 2 segundos.
- **Web ↔ Presentación:** el panel aparece en vivo en la lámina 4 y en el video; la página del proyecto puede
  enlazar la presentación y el video.
