# Encargo: dos máquinas más (prensa hidráulica y estación neumática)

Segunda tanda, **después** del molino ([`MODELO-MAQUINA.md`](MODELO-MAQUINA.md)). Rigen las mismas reglas
(§2 de ese archivo): metros, **+Y arriba**, **frente hacia −Z**, origen en el centro de la base al nivel del piso,
**nada transparente**, colores sólidos de la paleta, menos de 30.000 triángulos por máquina, nombres exactos y FBX.

Estas dos máquinas enseñan lo que el molino no: que **apagar no alcanza**. La energía queda **almacenada**
(presión hidráulica, aire comprimido, gravedad) y hay que **liberarla y ver el manómetro en 0** antes de tocar.
Por eso tienen **piezas que se mueven**: el pistón, la aguja del manómetro y las manijas de las válvulas.

---

## Modelo 4 — Prensa hidráulica de taller (tipo H)

Real: **1,00 m de ancho (X) × 0,60 m de fondo (Z) × 1,80 m de alto (Y)**. Bastidor en H, bomba eléctrica con motor
al costado, cilindro hidráulico arriba que empuja un pistón (ram) hacia abajo sobre una mesa de trabajo.

```
Prensa                      ← raíz. Origen: centro de la base, en el piso
├── Frame                   ← bastidor en H: dos columnas + travesaño superior + base
├── Bed                     ← mesa de trabajo (travesaño a ~0,70 m de alto), donde se apoya la pieza
├── Cylinder                ← cuerpo del cilindro hidráulico, fijo en el travesaño superior
├── RamPivot                ← vacío. Origen en el centro de la cara SUPERIOR del pistón
│   └── Ram                 ← pistón + plato de presión. SE MUEVE en vertical (Y)
├── PumpUnit                ← bomba + motor eléctrico + tanque de aceite, al costado (lado +X), sobre la base
├── Hose                    ← manguera hidráulica de la bomba al cilindro (puede ser un tubo curvo simple)
├── GaugeBody               ← cuerpo del manómetro (Ø 10 cm real), en la bomba, mirando al frente (−Z)
├── GaugeNeedlePivot        ← vacío. Origen en el CENTRO del dial
│   └── GaugeNeedle         ← aguja. GIRA sobre el eje Z (el que apunta al frente)
├── BleedValvePivot         ← vacío. Origen en el eje de la perilla de purga
│   └── BleedValve          ← perilla roja de purga (Ø 5 cm). GIRA sobre Z
├── SafetyBlock             ← taco de seguridad (bloque amarillo y negro, 10 × 10 × 30 cm). OBJETO SUELTO
├── BlockPoint              ← vacío: dónde va el taco cuando está puesto (debajo del pistón, sobre la Bed)
├── LockPoint               ← vacío: agujero de bloqueo de la palanca de la bomba
└── LabelPoint              ← vacío: centro de una placa frontal plana para el cartel «PRENSA»
```

**Articulaciones:**

| Pieza | Movimiento | Rango | Cómo comprobarlo |
|---|---|---|---|
| `RamPivot` / `Ram` | **Traslación en Y** (sube y baja) | En el archivo, el pistón **arriba** (retraído). La app lo baja hasta 0,25 m | Mové `RamPivot` −0,25 m en Y: el plato tiene que bajar recto sin atravesar la Bed |
| `GaugeNeedlePivot` | **Rota sobre Z** | En el archivo, la aguja en **0** (apuntando abajo a la izquierda, como un manómetro real). La app la gira hasta 240° | Rotá el pivote −240° en Z: la aguja recorre la escala sin salirse del dial |
| `BleedValvePivot` | **Rota sobre Z** | 0° cerrada; la app la gira 90° al purgar | Rotá 90° en Z: gira en el lugar |
| `SafetyBlock` | Ninguno (la app lo agarra y lo encastra en `BlockPoint`) | — | Su origen en el centro del taco |

- La **escala del manómetro** (marcas, 0 a 250 bar) se puede dibujar como relieve simple o dejar el dial blanco liso:
  la app dibuja la escala encima si hace falta. **Nada de vidrio** delante del dial.
- Colores: bastidor gris oscuro `#3A3F45`, cilindro y pistón acero `#CCD1D9`, bomba verde `#2E6B4F`, manguera negra
  `#151515`, perilla de purga roja `#C8141E`, taco amarillo `#F2B705` con franjas negras.

---

## Modelo 5 — Estación neumática de una envasadora

Real: **0,90 m (X) × 0,50 m (Z) × 1,20 m (Y)**. Compresor chico con tanque, una válvula de aislamiento (llave de
bola) con manómetro, y un cilindro neumático vertical que baja un cabezal sellador sobre una cinta corta.

```
Neumatica                   ← raíz. Origen: centro de la base, en el piso
├── Base                    ← bastidor o mesa baja
├── Compressor              ← compresor (motor + cabezal) sobre un tanque horizontal (Ø 0,25 m, largo 0,50 m)
├── AirLine                 ← tubería de aire del tanque a la válvula y de la válvula al cilindro (tubos simples)
├── ValveBody               ← cuerpo de la llave de bola, en la línea, a la altura de la mano, mirando al frente
├── ValveHandlePivot        ← vacío. Origen en el eje de la manija de la llave
│   └── ValveHandle         ← manija roja tipo palanca. GIRA sobre Y
├── ExhaustValve            ← válvula de escape/purga al costado de la llave (botón o perilla)
├── GaugeBody               ← manómetro (Ø 8 cm), después de la llave, mirando al frente
├── GaugeNeedlePivot        ← vacío. Origen en el CENTRO del dial
│   └── GaugeNeedle         ← aguja. GIRA sobre Z
├── CylinderBody            ← cilindro neumático vertical (Ø 8 cm, largo 0,40 m), fijo arriba
├── RodPivot                ← vacío. Origen en la cara superior del vástago
│   └── Rod                 ← vástago + cabezal sellador. SE MUEVE en vertical (Y)
├── Conveyor                ← cinta corta debajo del cabezal (puede ser simple)
├── LockPoint               ← vacío: dónde va el candado sobre la manija CERRADA de la llave
└── LabelPoint              ← vacío: placa frontal plana para el cartel «NEUMÁTICA»
```

**Articulaciones:**

| Pieza | Movimiento | Rango | Cómo comprobarlo |
|---|---|---|---|
| `ValveHandlePivot` / `ValveHandle` | **Rota sobre Y** (la manija gira en horizontal) | En el archivo, la manija **paralela a la tubería = ABIERTA**. La app la gira 90° para cerrar (perpendicular) | Rotá 90° en Y: la manija queda cruzada a la tubería y el `LockPoint` coincide con el agujero de la manija |
| `RodPivot` / `Rod` | **Traslación en Y** | En el archivo, el vástago **arriba**. La app lo baja hasta 0,15 m | Mové −0,15 m en Y: baja recto sin atravesar la cinta |
| `GaugeNeedlePivot` | **Rota sobre Z** | En el archivo, en **0**. La app la gira hasta 200° | Rotá −200° en Z |

- Colores: tanque y compresor azul `#1F4E8C`, tuberías acero `#CCD1D9`, manija de la llave roja `#C8141E`, cilindro
  acero, cabezal amarillo `#F2B705`, base gris oscuro `#3A3F45`.

---

## Prompts para Claude con Fusion 360

**Prensa:**

```text
Modelá en Fusion 360 (metros, Y arriba, frente a −Z) una prensa hidráulica de taller tipo H, low-poly: 1,00 m de
ancho (X), 0,60 m de fondo (Z), 1,80 m de alto. Componentes separados y con estos nombres exactos: Frame (bastidor en H),
Bed (mesa de trabajo a 0,70 m), Cylinder (cilindro fijo arriba), RamPivot (componente vacío con origen en la cara
superior del pistón) que contiene a Ram (pistón + plato, con el pistón ARRIBA), PumpUnit (bomba + motor + tanque al
costado +X), Hose (manguera), GaugeBody (manómetro Ø 10 cm mirando a −Z), GaugeNeedlePivot (vacío en el centro del
dial) con GaugeNeedle (aguja en 0), BleedValvePivot (vacío en el eje) con BleedValve (perilla roja de purga),
SafetyBlock (taco 10x10x30 cm amarillo con franjas negras, suelto, origen en su centro), BlockPoint (vacío debajo del
pistón sobre la Bed), LockPoint (vacío en el agujero de bloqueo de la palanca de la bomba) y LabelPoint (vacío en una
placa frontal plana). Todo opaco, colores sólidos (bastidor #3A3F45, cilindro y pistón #CCD1D9, bomba #2E6B4F,
manguera #151515, perilla #C8141E, taco #F2B705). Menos de 30.000 triángulos. Probá con joints temporales que
RamPivot baja 0,25 m recto y que GaugeNeedlePivot gira en Z sobre el centro del dial; dejalos en 0 y exportá FBX.
```

**Estación neumática:**

```text
Modelá en Fusion 360 (metros, Y arriba, frente a −Z) una estación neumática de envasadora, low-poly: 0,90 (X) x 0,50
(Z) x 1,20 m (Y). Componentes con estos nombres exactos: Base, Compressor (compresor sobre tanque horizontal Ø 0,25 x
0,50 m), AirLine (tuberías), ValveBody (llave de bola a la altura de la mano, mirando a −Z), ValveHandlePivot (vacío
en el eje de la manija) con ValveHandle (manija roja, ABIERTA = paralela a la tubería), ExhaustValve (válvula de
purga), GaugeBody (manómetro Ø 8 cm), GaugeNeedlePivot (vacío en el centro del dial) con GaugeNeedle (en 0),
CylinderBody (cilindro neumático vertical Ø 8 cm x 0,40 m), RodPivot (vacío en la cara superior del vástago) con Rod
(vástago + cabezal sellador, ARRIBA), Conveyor (cinta corta debajo), LockPoint (vacío en el agujero de la manija
cerrada) y LabelPoint (vacío en placa frontal). Todo opaco, colores sólidos (tanque #1F4E8C, tuberías y cilindro
#CCD1D9, manija #C8141E, cabezal #F2B705, base #3A3F45). Menos de 30.000 triángulos. Probá que ValveHandlePivot gira
90° en Y y que RodPivot baja 0,15 m recto; dejalos en 0 y exportá FBX.
```

Para Blender, mismos nombres y jerarquía con Empties, y los ajustes de exportación de `MODELO-MAQUINA.md` §7.

## Lista antes de mandar (además de la del molino)

- [ ] El pistón y el vástago están **arriba** en el archivo, y sus pivotes en la cara superior.
- [ ] Las agujas de los manómetros están en **0** y su pivote en el centro exacto del dial.
- [ ] La manija de la llave neumática está **abierta** (paralela a la tubería).
- [ ] El `SafetyBlock` es un objeto suelto con el origen en su centro.
- [ ] Ningún vidrio delante de los diales.
