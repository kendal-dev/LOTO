# Encargo: la máquina real en 3D (Fusion 360)

Para quien modela. Hoy la máquina del simulador es un modelo genérico (Kenney, CC0). La idea es reemplazarlo por
una **máquina que exista en los talleres e industrias de Santa Cruz**, sin tocar la lógica: solo cambia la malla.
**Hora límite para entregar el archivo: sábado 14:15** (a las 15:00 se congelan las funciones). Si no llega, se
queda la de Kenney y no pasa nada.

## Qué máquina

**Un molino de martillos para granos con motor eléctrico y transmisión por correa.** Por qué esta:

- Santa Cruz es el centro agroindustrial del país (granos, alimento balanceado, ingenios): es una máquina que el
  estudiante de electromecánica va a encontrar en su trabajo.
- Su peligro se entiende de un vistazo: la **polea y la correa giran** y la **tolva** lleva a los martillos. Es
  exactamente lo que el bloqueo evita.
- Es fácil de modelar con formas simples (cajas, cilindros, un tronco de pirámide).

Si no sale el molino, segunda opción: **motor eléctrico con transmisión por correa sobre una base** (lo tiene
cualquier taller de electromecánica).

## Piezas (cada una un **cuerpo separado**, con este nombre exacto)

| Nombre | Qué es | Notas |
|---|---|---|
| `Body` | La carcasa del molino (la caja grande) | Una cara frontal **plana** de al menos 18 × 5 cm (a escala real ~ 60 × 17 cm) para el cartel «MÁQUINA» |
| `Hopper` | La tolva arriba (tronco de pirámide abierto) | Sin paredes de grosor cero |
| `Motor` | El motor eléctrico al costado (cilindro con aletas simples) | |
| `Rotor` | **La polea grande con la correa: lo que gira** | **Componente aparte, con su origen en el eje de giro.** Es lo que la app hace girar cuando hay energía |
| `Guard` | La guarda de la correa (puede ser parcial, para que se vea girar la polea) | Opaca |
| `Base` | El bastidor o patas | |
| `LampPoint` | Un punto o cuerpo mínimo donde va la lámpara de energía (arriba, en el frente) | Solo marca la posición |

## Reglas técnicas (importantes)

- **Escala real en metros** (el molino real mide ~ 1,2 m de largo). La app lo achica sola para la mesa.
- **Eje Y hacia arriba** al exportar. **El frente (donde va el cartel) mirando hacia −Z** (hacia quien lo mira).
- **Todo opaco: nada de vidrio ni transparencias** (en el visor abren agujeros en la imagen de la cámara).
- **Colores sólidos por pieza**, sin texturas: carcasa verde o azul industrial, motor gris, polea y guarda amarillo
  seguridad, tolva gris claro.
- **Liviano:** menos de 30.000 triángulos en total. Sin tornillos ni detalles chicos.
- Exportar **FBX** (preferido) o **OBJ**, con las piezas como objetos separados y los nombres de arriba.

## Prompt para pegar en Claude con el MCP de Fusion 360

```text
Modelá en Fusion 360 un molino de martillos industrial para granos, estilo simple y limpio (low-poly), a escala
real en metros (~1,2 m de largo, ~1,0 m de alto). Cuerpos separados y con estos nombres exactos: Body (carcasa,
con una cara frontal plana grande), Hopper (tolva arriba, tronco de pirámide abierto), Motor (motor eléctrico al
costado, cilindro con aletas simples), Rotor (polea grande con su correa, componente aparte con el origen en el eje
de giro), Guard (guarda parcial de la correa), Base (bastidor o patas) y LampPoint (un cubo mínimo arriba en el
frente). Todo opaco, colores sólidos: carcasa verde industrial, motor gris, polea y guarda amarillo seguridad,
tolva gris claro. Menos de 30.000 triángulos, sin detalles chicos. Eje Y hacia arriba, el frente hacia −Z.
Exportá un FBX con las piezas como objetos separados.
```

## Cómo se entrega

Mandar el `.fbx` a Daniel (chat o USB). Se integra en `unity/Assets/ThirdParty/` con el mismo código que hoy viste
la máquina de Kenney: escala sola, el `Rotor` pasa a ser lo que gira y la lámpara va a `LampPoint`. Si es
modelo propio, decí si lo podemos publicar en el repo y con qué licencia.
