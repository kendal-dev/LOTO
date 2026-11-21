# Tareas del desarrollador web

En orden de prioridad. Cada una dice dónde, qué y cuándo está terminada. **Hora límite de funciones: sábado
15:00.** Todo sin internet (sin CDNs) y sin cambiar la API ([`docs/CONTRATO.md`](../CONTRATO.md)).

Antes de empezar: hablar 5 minutos con **Denzel** para saber si el panel vive en su servidor (`server/`) o en
`server-mock/public/index.html`. Si `server/` no existe todavía, se trabaja sobre el mock (es un solo archivo).

---

## W1 · Panel del docente con cara de producto — **lo más importante** (≈ 2 h)

**Dónde:** `server-mock/public/index.html` (o el panel de `server/`).
**Por qué:** es lo que el jurado ve en pantalla grande en la demo (lámina 4) y en el video. Hoy funciona, pero se
ve como una tabla.

- Arriba, **tres números grandes**: intentos · completados sin error (con %) · el error que más intentos frenó.
- Cada intento como una **tarjeta o fila clara**: estudiante, verde «Completo» o rojo con el paso donde falló y el
  texto del error, segundos y hora. Lo más nuevo arriba, con una animación corta al entrar.
- **Estado vacío:** «Esperando el primer intento del visor» (nada inventado).
- Colores de la app: fondo oscuro, verde `#7CE08A` solo para completo, rojo `#FF6B6B` solo para error, ámbar
  `#FFA61A` de acento. Letra grande: se mira en una videollamada.
- Se mantiene «Descargar CSV» y «Borrar» (este con confirmación dentro de la página, no `confirm()`).

**Terminado cuando:** con 5 intentos mandados por `curl` (CONTRATO §4) se entiende en 5 segundos quién lo hizo
bien y dónde falla la clase, y se ve bien a 1280×720 compartido en Meet.

## W2 · Gráfico «en qué paso falla la clase» (≈ 45 min)

**Dónde:** el mismo panel.
- Barras por paso (1 apagar · 2 candado · 3 verificar · 4 tocar), contando los intentos con `ok: false` según su
  `step`. SVG o canvas a mano, sin librerías.
- Etiquetas con el número real; si no hay errores, la sección dice «Sin errores todavía».

**Terminado cuando:** con los intentos de prueba las barras coinciden con la lista.

## W3 · Modo proyector para el pitch (≈ 30 min)

**Dónde:** el mismo panel, con `?proyector=1` en la URL.
- Oculta botones y deja solo los tres números, el gráfico y los últimos 5 intentos, en letra todavía más grande.
- Así Daniel lo pone a la derecha de la transmisión del visor sin que se vea «la parte de administración».

**Terminado cuando:** a pantalla completa en 1280×720 todo se lee desde 2 metros.

## W4 · Página pública del proyecto (≈ 1,5 h, puede seguir el domingo hasta las 12:30)

**Dónde:** carpeta nueva `web/` (un `index.html` + estilos), para publicar en GitHub Pages desde el repo.
- Secciones: el problema (solo los datos de `docs/PLAN.md` §2.3.2, con su fuente y país), cómo funciona (los 4
  pasos), la demo (espacio para el video del domingo y capturas del visor), qué tiene de distinto, el equipo, y
  enlaces al repo y a la presentación.
- Textos: copiar de `README.md` y `docs/GUION-PITCH.md`, **sin agregar cifras ni adjetivos** («único», «primeros»
  y «no existe» están prohibidos; ver `docs/PLAN.md` §2.3.1).
- Imágenes: capturas reales del visor cuando las haya; mientras, sin imágenes inventadas.

**Terminado cuando:** se abre desde el celular, se entiende en 30 segundos y no dice nada que la app no haga.

## W5 · Si sobra tiempo: QR en el panel (≈ 20 min)

Un QR chico en una esquina del panel con la URL de la página del proyecto, para el cierre del pitch. Generado sin
servicios externos (un generador de QR en JS incluido en el repo, o una imagen hecha una vez y guardada).

---

## Cómo se entrega cada tarea

1. Rama propia (`web/w1-panel`, etc.) o commits chicos en `main` si lo acordás con Daniel.
2. Probar con `node server-mock/server.js` y los `curl` del contrato.
3. Avisar a Daniel con una captura para revisarlo antes de juntar.
