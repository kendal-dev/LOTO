# Encargo: los modelos 3D del simulador (Fusion 360 o Blender)

Para quien modela. Leé entero (10 minutos) antes de abrir el programa: la mitad del trabajo es que **los pivotes y los
ejes queden bien**, porque la app mueve piezas por código.

**Hora límite: sábado 14:15** para el modelo 1 (la máquina). A las 15:00 se congelan las funciones. Si algo no
llega, la app sigue con los modelos actuales (Kenney, CC0) y no se rompe nada: cada pieza se reemplaza por separado.

---

## 0. Qué hace la app con tus modelos (para que entiendas por qué piden lo que piden)

El simulador enseña **bloqueo y etiquetado (LOTO)**: el estudiante, con el Meta Quest 3 en realidad mixta, tiene
que (1) bajar la palanca del seccionador, (2) poner su candado con una tarjeta, (3) apretar VERIFICAR, y (4) recién
ahí tocar la máquina. Mientras hay energía, **una parte de la máquina gira**; al bajar la palanca, se detiene.

La app **ya hace todo el comportamiento**. Tus modelos solo cambian **cómo se ve**. Por eso:

- La app **escala sola** el modelo para que entre en la mesa (la máquina termina midiendo ~25 cm). Vos modelá en
  **escala real**.
- La app **hace girar** la pieza `Rotor` y **mueve** la palanca `Handle`. Esas piezas tienen que estar **separadas**
  y con el **origen en su eje de giro**. Si el origen queda en otro lado, la pieza «orbita» en lugar de girar.
- Las zonas que se tocan (la máquina, los botones) son cajas invisibles que pone la app: no hace falta modelar nada
  para eso.

---

## 1. Prioridades

| # | Modelo | Para qué | ¿Obligatorio? |
|---|---|---|---|
| **1** | **Molino de martillos con motor y transmisión por correa** | La máquina peligrosa (la del bloqueo) | **Sí, lo más importante** |
| 2 | Seccionador (caja con palanca) | El interruptor que corta la energía | Si sobra tiempo |
| 3 | Candado de bloqueo con tarjeta | El candado personal del estudiante | Si sobra tiempo |

### Por qué un molino de martillos

- Santa Cruz es el centro agroindustrial de Bolivia (granos, alimento balanceado, ingenios): es una máquina que un
  estudiante de electromecánica va a encontrar en su trabajo.
- Su peligro se ve: la **polea y la correa giran** a la vista y la **tolva** lleva a los martillos. Es justo lo que
  el bloqueo evita (atrapamiento).
- Se modela con formas simples: cajas, cilindros y un tronco de pirámide.

Plan B si el molino se complica: **un motor eléctrico con transmisión por correa sobre una base** (mismas reglas, sin
tolva).

---

## 2. Reglas para TODOS los modelos

| Regla | Valor | Por qué |
|---|---|---|
| Unidades | **Metros**, escala real | La app escala sola; con escala real las proporciones salen bien |
| Eje vertical | **+Y hacia arriba** (en el archivo exportado) | Unity usa Y arriba |
| Frente | **El frente mira hacia −Z** (hacia el estudiante) | La app pone al estudiante en −Z |
| Derecha | +X a la derecha mirando el frente | Convención de Unity |
| Origen del modelo completo | **En el centro de la base, al nivel del piso** (y = 0) | Así «apoya» sobre la mesa |
| Transparencias | **Ninguna. Nada de vidrio, alfa ni materiales translúcidos** | En el visor, lo translúcido abre agujeros en la imagen de la cámara |
| Materiales | **Colores sólidos por pieza**, sin texturas (opcional: un solo atlas de color) | Rápido y se ve limpio |
| Triángulos | Máquina **< 30.000**; seccionador y candado **< 8.000** cada uno | El Quest lo mueve con margen |
| Detalle | Sin tornillos, roscas, texto modelado ni piezas de menos de 1 cm (a escala real) | Se pierden a escala de mesa |
| Normales | Hacia afuera, sin caras de grosor cero | Si no, se ven agujeros |
| Nombres | **Exactamente** los de las tablas (mayúsculas incluidas) | La app busca las piezas por nombre |
| Formato | **FBX** (preferido) u **OBJ** con piezas como objetos separados | Unity lo importa directo |

### Paleta (colores sólidos)

| Uso | Color | HEX |
|---|---|---|
| Carcasa de máquina | Verde industrial | `#2E6B4F` |
| Motor | Gris acero | `#7A8088` |
| Partes que giran y guardas | **Amarillo seguridad** | `#F2B705` |
| Tolva y chapa | Gris claro | `#B9BEC4` |
| Base / bastidor | Gris oscuro | `#3A3F45` |
| Correa | Negro goma | `#151515` |
| Caja del seccionador | Amarillo ocre | `#D9A60D` |
| Mango de la palanca | Rojo | `#C8141E` |
| Candado | Rojo | `#D61A1A` |
| Arco del candado | Acero claro | `#CCD1D9` |
| Tarjeta | Blanco | `#F7F7F2` |

---

## 3. Modelo 1 — Molino de martillos (obligatorio)

Medidas aproximadas del real: **1,20 m de largo (X) × 0,70 m de fondo (Z) × 1,10 m de alto (Y)**.

### Jerarquía (así tiene que quedar en el FBX)

```
Molino                      ← vacío/raíz. Origen: centro de la base, en el piso
├── Base                    ← bastidor o patas
├── Body                    ← carcasa del molino (caja principal)
├── Hopper                  ← tolva arriba
├── Motor                   ← motor eléctrico al costado (lado −X o +X)
├── Guard                   ← guarda de la correa (parcial: que se vea girar la polea)
├── RotorPivot              ← vacío. Origen EXACTAMENTE en el eje de la polea grande
│   └── Rotor               ← polea grande + tramo de correa visible. GIRA
├── LampPoint               ← vacío o cubito de 1 cm: dónde va la lámpara de energía
└── LabelPoint              ← vacío o cubito de 1 cm: centro de donde va el cartel «MÁQUINA»
```

### Pieza por pieza

| Pieza | Forma y medidas (real) | Notas |
|---|---|---|
| `Base` | Bastidor de perfiles o 4 patas + travesaños, 1,20 × 0,70 m, alto 0,25 m | Gris oscuro |
| `Body` | Caja 0,70 (X) × 0,60 (Z) × 0,55 (Y), apoyada sobre la base | **La cara frontal (−Z) plana y sin agujeros**, de al menos 0,60 × 0,17 m: ahí la app pone el cartel |
| `Hopper` | Tronco de pirámide abierto arriba: boca 0,50 × 0,45 m, base 0,25 × 0,20 m, alto 0,30 m, sobre el Body | Paredes con grosor (≥ 2 cm). Sin tapa |
| `Motor` | Cilindro horizontal Ø 0,25 m, largo 0,40 m, con 6–8 aletas simples y una caja de bornes arriba; eje **paralelo a Z** | Al costado del Body |
| `Rotor` | **Polea grande Ø 0,40 m**, ancho 0,08 m, con 4–6 rayos, **más la correa** que la une a una polea chica del motor | Amarillo (polea) y negro (correa) |
| `Guard` | Media carcasa que cubre la mitad de atrás de la correa | Amarillo. Dejá ver la polea de frente |
| `LampPoint` | Punto arriba del Body, en el frente, a ~5 cm del borde superior izquierdo | Solo marca posición |
| `LabelPoint` | Centro de la cara frontal plana del Body | Solo marca posición |

### La articulación del `Rotor` (lo más importante)

1. El `Rotor` (polea + correa visible) es **un objeto separado**.
2. Va **dentro de** `RotorPivot`. El **origen de `RotorPivot` está exactamente en el centro del eje** de la polea.
3. **La polea gira alrededor del eje Z del modelo** (el eje de la polea apunta hacia el frente, −Z/+Z), así el
   estudiante ve la polea girar de frente.
4. El `Rotor` **no** tiene rotación aplicada raro: en la posición del archivo, la polea ya está «armada» en su lugar.
5. Prueba: rotá `RotorPivot` 90° sobre Z en el programa. Si la polea gira **en el lugar** (no se desplaza), está bien.
   Volvé a 0° antes de exportar.

> La correa en la vida real no gira «rígida» con la polea, pero para el simulador alcanza con que gire el conjunto
> visible de la polea con sus rayos. Si querés, la correa puede ir en `Guard` (quieta) y solo la polea en `Rotor`.

---

## 4. Modelo 2 — Seccionador con palanca (si sobra tiempo)

Caja de interruptor de seguridad, como las de tablero industrial. Real: **0,30 (X) × 0,40 (Y) × 0,20 (Z)**.

```
Seccionador                 ← raíz. Origen: centro de la base de la caja, abajo
├── Box                     ← caja (amarillo ocre)
├── HandlePivot             ← vacío. Origen EXACTAMENTE en el eje de giro de la palanca (bisagra)
│   └── Handle              ← brazo + pomo rojo. GIRA
└── LockPoint               ← vacío: dónde se engancha el candado (el agujero del candado)
```

**Articulación de la palanca (como la usa la app):**

- La palanca **gira alrededor del eje X** (izquierda-derecha), en `HandlePivot`, que va en la parte de arriba de la
  caja.
- **En el archivo, la palanca va hacia ARRIBA (+Y): esa es la posición ENCENDIDA (0°).**
- La app la baja **90° hacia el frente (−Z)** para APAGADA. Comprobalo: rotá `HandlePivot` −90° sobre X; el brazo
  tiene que quedar apuntando al estudiante (−Z) sin atravesar la caja. Volvé a 0° antes de exportar.
- Brazo de **0,18 m** de largo desde el eje, Ø 3–4 cm, con pomo rojo Ø 6 cm en la punta.
- `LockPoint` va **donde queda el agujero de bloqueo cuando la palanca está ABAJO**: una pestaña con agujero (Ø 1 cm
  real) abajo y adelante de la caja, unos 13 cm adelante del eje y 3 cm debajo. Ahí la app cuelga el candado.

---

## 5. Modelo 3 — Candado de bloqueo con tarjeta (si sobra tiempo)

Candado de seguridad LOTO (cuerpo de plástico rojo, arco de acero). Real: **cuerpo 4 × 3,5 × 1,6 cm, arco 3,8 cm**.

```
Candado                     ← raíz. Origen: CENTRO del candado completo
├── Body                    ← cuerpo rojo
├── Shackle                 ← arco (U invertida), acero
└── Tag                     ← tarjeta colgando: 6,5 × 8,5 cm, 3 mm, con su alambre
    └── TagFace             ← la cara frontal de la tarjeta, PLANA (la app escribe el texto encima)
```

- El origen en el centro porque la app **lleva ese centro** al `LockPoint` del seccionador.
- La tarjeta cuelga **debajo** del cuerpo, con la cara `TagFace` mirando a −Z, **lisa y blanca**: la app escribe
  «PELIGRO · NO OPERAR · Bloqueado por: Estudiante N · hora».
- Opcional: `Shackle` con su origen en la bisagra del arco, por si después queremos animarlo abriéndose (no hace
  falta hoy).

---

## 6. Paso a paso en Fusion 360

1. **Preferencias → Unidades: metros** (o modelá en mm y exportá en m; ver punto 7).
2. Activá **«Y arriba»**: Preferencias → General → *Default modeling orientation* → **Y up**.
3. Un **Componente por pieza** (no solo cuerpos): clic derecho en el cuerpo → *Create Components from Bodies*.
   Renombrá cada componente con el nombre exacto de la tabla.
4. **Pivotes:** para `RotorPivot` y `HandlePivot`, creá un componente vacío y movelo (*Move/Copy* →
   *Point to Point*) para que **su origen** quede en el centro del eje. Después **arrastrá** `Rotor`/`Handle` dentro
   de ese componente en el navegador.
5. **Probá la articulación:** creá un *Revolute Joint* temporal en el eje y movelo. Si gira en el lugar, está bien.
   Borrá o dejá el joint en 0°.
6. **Apariencias:** colores sólidos de la paleta (*Appearance* → *Paint* → *Gloss* o *Matte*). Nada de vidrio.
7. **Exportar:** *File → Export → **FBX***. Si el FBX sale gigante o chico en Unity, se corrige solo (la app
   escala), pero **la orientación sí importa**: si al abrirlo en Blender o en un visor la máquina aparece acostada,
   reexportá con Y arriba.
8. Si Fusion no deja exportar FBX con la licencia que tenés: exportá **OBJ** (uno solo, con grupos por componente).

**Prompt para Claude con el MCP de Fusion 360:**

```text
Modelá en Fusion 360, en metros y con Y hacia arriba, un molino de martillos industrial simple (low-poly):
1,20 m de largo en X, 0,70 m de fondo en Z, 1,10 m de alto. El frente mira a −Z.
Componentes separados con estos nombres exactos y jerarquía:
- Molino (raíz, origen en el centro de la base al nivel del piso)
  - Base: bastidor de perfiles 1,20 x 0,70 m, alto 0,25 m, gris oscuro #3A3F45
  - Body: caja 0,70 (X) x 0,60 (Z) x 0,55 (Y) sobre la base, verde #2E6B4F, cara frontal −Z plana y sin agujeros
  - Hopper: tolva tronco de pirámide abierta, boca 0,50 x 0,45, base 0,25 x 0,20, alto 0,30, sobre el Body, gris claro #B9BEC4, paredes de 2 cm
  - Motor: cilindro horizontal Ø 0,25 m, largo 0,40, eje paralelo a Z, con 6 aletas simples y caja de bornes, al costado +X del Body, gris #7A8088
  - Guard: guarda que cubre la mitad trasera de la correa, amarillo #F2B705
  - RotorPivot: componente vacío con su origen EXACTAMENTE en el eje de la polea grande; adentro:
    - Rotor: polea Ø 0,40 m, ancho 0,08, 5 rayos, amarillo #F2B705, con la correa negra #151515 que la une a una polea chica del motor. La polea gira sobre el eje Z.
  - LampPoint: cubo de 1 cm arriba del Body, en el frente, cerca del borde superior izquierdo
  - LabelPoint: cubo de 1 cm en el centro de la cara frontal del Body
Todo opaco (sin vidrio ni transparencias), menos de 30.000 triángulos, sin tornillos ni detalles chicos.
Verificá con un Revolute Joint temporal que RotorPivot gira en el lugar y dejalo en 0°. Exportá un FBX.
```

---

## 7. Paso a paso en Blender (también sirve para convertir lo de Fusion)

1. *Scene Properties → Units*: **Metric, Unit Scale 1.0**. Modelá en metros.
2. Blender usa **Z arriba** y el frente es **−Y**. Modelá así (frente hacia −Y en Blender); el exportador lo convierte.
3. Un **objeto por pieza**, con el nombre exacto.
4. **Pivotes:** para `RotorPivot`/`HandlePivot` usá un **Empty** (*Add → Empty → Plain Axes*) en el centro del eje
   (Shift+S → *Cursor to Selected* sobre el eje, después agregá el Empty). **Emparentá** `Rotor` al Empty
   (seleccioná Rotor, después el Empty, **Ctrl+P → Object (Keep Transform)**).
5. **Origen del modelo:** el objeto raíz `Molino` (otro Empty) en el centro de la base, a Z = 0. Todo emparentado a él.
6. **Aplicá transformaciones** a las mallas (Ctrl+A → *All Transforms*) **antes** de emparentar a los pivotes, para
   que la escala de cada malla quede en 1.
7. **Probá:** rotá el Empty `RotorPivot` sobre el eje de la polea (en Blender es su eje −Y/+Y, el que apunta al
   frente); la polea tiene que girar en el lugar. Volvé a 0.
8. **Materiales:** *Principled BSDF* con solo *Base Color* (la paleta), **Alpha = 1**, *Blend Mode: Opaque*.
9. **Exportar FBX** (*File → Export → FBX*):
   - *Limit to*: lo que quieras exportar (o todo).
   - *Object Types*: **Empty + Mesh**.
   - *Transform*: **Scale 1.0**, *Apply Scalings*: **FBX All**, **Forward: −Z Forward**, **Up: Y Up**,
     **Apply Unit** ✔, **Apply Transform** ✔ (experimental, deja la orientación limpia para Unity).
   - *Geometry*: *Apply Modifiers* ✔, *Smoothing*: **Face**.
   - *Armature/Animation*: desactivado.

**Prompt para Claude con Blender MCP** (si usa Blender en vez de Fusion): el mismo de Fusion, cambiando la línea
de ejes por: *«En Blender: metros, Z arriba, el frente mira a −Y; usá Empties para Molino, RotorPivot (en el eje de la
polea) y emparentá con Keep Transform; aplicá transformaciones; materiales opacos; exportá FBX con Forward −Z, Up Y,
Apply Scalings FBX All y Apply Transform»*.

---

## 8. Lista para revisar ANTES de mandar

- [ ] Nombres exactos: `Base`, `Body`, `Hopper`, `Motor`, `Guard`, `RotorPivot`, `Rotor`, `LampPoint`, `LabelPoint`.
- [ ] Escala real en metros (el molino mide ~1,2 m de largo).
- [ ] Abierto en un visor (Blender, el visor 3D de Windows o https://3dviewer.net), **está parado, no acostado**, y
      el frente plano del Body mira hacia el que lo mira.
- [ ] `RotorPivot` gira en el lugar (probado y vuelto a 0°).
- [ ] Nada transparente; todos los materiales con color sólido.
- [ ] Menos de 30.000 triángulos.
- [ ] Un solo archivo `.fbx` (y, si hay, su carpeta de texturas al lado).
- [ ] Decí si es **modelo propio** y si lo podemos publicar en el repo (y con qué licencia: CC0 o CC-BY con tu
      nombre). Si es de internet, mandá el enlace y la licencia.

## 9. Cómo se entrega

Mandá el `.fbx` a Daniel (chat o USB) con una captura de cómo se ve. Se integra en `unity/Assets/ThirdParty/` con el
mismo código que hoy viste la máquina de Kenney: la app lo escala, pone el cartel en `LabelPoint`, la lámpara en
`LampPoint` y hace girar `RotorPivot`. Cada modelo se puede integrar por separado, en cualquier orden.
