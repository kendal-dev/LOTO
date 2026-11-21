# Simulador LOTO en realidad mixta

**Práctica de bloqueo y etiquetado (LOTO, *lockout/tagout*) para estudiantes de taller, en Meta Quest 3.**
La máquina aparece en el aula real, sin aislar al estudiante en un mundo virtual (realidad mixta) y el docente ve cada
intento en vivo desde un panel web.

Proyecto del hackathon **Abstracthon 2026** (Microsoft Learn Student Ambassadors Bolivia, 21-22/11/2026),
track Educación. Equipo de Santa Cruz de la Sierra: Daniel, Denzel y Orlando. [PENDIENTE: nombre del equipo]

---

## El problema

El bloqueo y etiquetado es el procedimiento que evita que una máquina se encienda mientras alguien la está
reparando o limpiando. Creemos que lo que falta es el hábito de hacer los pasos en orden, y el hábito se entrena.

- **Bolivia:** en 2024 el Ministerio de Trabajo recibió **12.210 denuncias de accidentes de trabajo**:
  **79 fatales**, **7 en Santa Cruz**. Es el total de accidentes, no solo los de falta de bloqueo.
- **EE. UU. (OSHA):** cumplir el estándar de bloqueo previene unas **120 muertes y 50.000 lesiones por año**
  ([hoja informativa de OSHA](https://www.osha.gov/sites/default/files/publications/OSHAFS3529.pdf)).
- **EE. UU.:** un estudio de 592 accidentes fatales de bloqueo en fábricas de EE. UU. (datos de 1984 a 1997) encontró que en el **58,8 %** **no se intentó ningún bloqueo** ([Bulzacchelli et al., 2008, estudio financiado por NIOSH](https://stacks.cdc.gov/view/cdc/186886/cdc_186886_DS1.pdf)).
- **Bolivia (COMIBOL):** su reglamento de seguridad de 2023 dice que *«el bloqueo de la fuente de energía será
  retirado por la persona que lo colocó»*
  ([reglamento](https://www.comibol.gob.bo/images/noticias/REGLAMENTO%20DE%20NORMAS%20BASICAS%20DE%20SST%20GESTION%202023%20Enviado%20.pdf)).

**Para quién es:** estudiantes de electromecánica, electricidad industrial y mecánica de institutos técnicos,
y sus docentes.

## Cómo funciona

El estudiante tiene que seguir el orden:

1. **Bajar la palanca** (apagar y aislar).
2. **Poner su candado** sobre la palanca.
3. **Apretar VERIFICAR** (la prueba de arranque: la máquina no arranca).
4. **Recién ahí, tocar la máquina.**

Si toca la máquina antes de tiempo, **la simulación se frena** y un cartel le explica qué hizo mal. Cada intento
llega a un **panel web** donde el docente ve quién completó, en qué paso se equivocan más y cuánto tardan.

### Lo que ve y oye el estudiante

| Qué | Cómo |
|---|---|
| **La máquina con energía** | Gira, zumba y tiene una lámpara roja. Al bajar la palanca el zumbido se apaga y la lámpara pasa a gris |
| **Cada paso** | Un pitido al acertar y un doble bip ante un error que deja seguir (por ejemplo, el candado antes de apagar) |
| **El candado con su tarjeta** | Al trabar el candado cuelga una tarjeta **PELIGRO · NO OPERAR** con «Bloqueado por: Estudiante N» y la hora (la «T» de LOTO) |
| **La prueba de arranque** | Al apretar VERIFICAR suena un «clac» seco, la lámpara parpadea y sale **PRUEBA DE ARRANQUE · NO ARRANCA** |
| **El final** | Tres notas y el panel en verde al completar. Un zumbador y el panel en rojo si toca la máquina antes |
| **El repaso del error** | El panel marca en rojo el paso que se saltó, «(TE LO SALTASTE)», y abajo muestra **«Tu orden: …»** con lo que hizo |
| **La guía después del error** | Antes de equivocarse no hay guía. Desde el primer error (el que frena o un aviso amarillo), un alfiler señala el paso que falta. |

Todo se arma con figuras simples de colores y sonidos generados por código: el proyecto **no usa modelos 3D,
texturas ni audio de terceros**. Los materiales son opacos para no abrir huecos en el passthrough.

### Lo que ve el docente

Un panel web que se actualiza solo, con cada intento: estudiante, si completó o en qué paso falló, el código del
error, el tiempo y la secuencia de acciones. Se puede descargar en CSV.

## Qué tiene de distinto

Revisamos los simuladores de bloqueo que hay en el mercado: Ludus Global (España), Yoy Simulators (Chile),
PIXO VR, EHS VR, iQ3Connect, NxtTech, Roundtable Learning, Linde LVA y uno en Steam. **Todos son de realidad
virtual cerrada** (el estudiante está en una planta virtual) y se venden a empresas por cotización.

- **Realidad mixta en el taller real:** en los que revisamos, ninguno pone la máquina en el aula real, sin aislar al estudiante en un mundo virtual.
- **La guía aparece solo después del error**, en vez de separar «modo guiado» y «modo examen».
- **El docente ve la clase en vivo**, sin un sistema de gestión de aprendizaje (LMS).
- **La tarjeta dice quién bloqueó y a qué hora**, porque el reglamento de COMIBOL (2023) dice que el bloqueo lo retira quien lo colocó.

Ya hay formación técnica inmersiva en la región: Yoy Simulators trabaja con Duoc UC (Chile) con más de 3.000
estudiantes y Meta Quest ([nota del 21/01/2026](https://www.mineriayfuturo.cl/noticias/chile-impulsa-el-mayor-proyecto-de-capacitacion-tecnica-virtual-de-latinoamerica-con-realidad-virtual/)),
y en Bolivia existen simuladores técnicos como los de Mi Teleférico e IA Technology. Lo nuestro no es ser los
primeros en usar simulación, sino **llevar la práctica al taller real y darle los datos al docente en vivo**.
En Santa Cruz no encontramos ningún simulador inmersivo de formación técnica.

**El equipo ya lo hizo una vez:** el 14-15/11/2026, en la misma UCB, el equipo MARSXR (del que forman parte Daniel
y Orlando) ganó el 1.er lugar y Best Mission Concept del NASA Space Apps Santa Cruz con Walkback, una app de
realidad mixta para Meta Quest 3.

**Lo que no hacemos y otros sí:** catálogo de muchas máquinas, gráficos realistas, alineación formal con normas
(OSHA o europeas) e integración con LMS empresariales.

## Arquitectura

```
   Meta Quest 3 (Unity)                         Laptop / servidor
 +------------------------+                 +-----------------------+
 |  Escena en passthrough |                 |  API                  |
 |  palanca · candado ·   |   POST          |  /api/attempts        |
 |  verificar · máquina   |  /api/attempts  |  /api/attempts.csv    |
 |           |            | --------------> |          |            |
 |   Referee (Loto.Core)  |                 |  texto de explicación |
 |   decide ok / error    | <-------------- |          |            |
 |           |            |   explicación   |  Panel del docente    |
 |  cartel al estudiante  |                 |  (navegador)          |
 +------------------------+                 +-----------------------+
```

La regla de qué está bien y qué está mal vive en el visor (`Loto.Core.Referee`, C# sin Unity, con pruebas). El
servidor guarda, explica y muestra. El contrato entre los dos está en [`docs/CONTRATO.md`](docs/CONTRATO.md).
Si el servidor no responde en 5 segundos, el visor sigue solo con su propio texto.

## Cómo se corre cada parte

### Requisitos

- **Visor:** Unity **6000.4.6f1**, Meta XR SDK **209.0.0**, OpenXR 1.16.1, URP 17.4 y un Meta Quest 3.
- **Servidor de prueba:** Node.js (probado con v24.19). Sin `npm install`.
- **Pruebas de la lógica:** SDK de .NET 10.

### Visor (Unity)

1. Unity Hub → **Add** → la carpeta `unity/` → abrir con **6000.4.6f1**. La primera importación tarda entre 10 y 20 minutos.
2. Menú **Loto > Build Scene**. La escena se arma por código (no se edita a mano). En la consola tiene que salir
   `[Loto] Scene built: … | Meta rig: yes | hands: yes`.
3. **Play** sin visor: se prende una cámara de escritorio y se juega con el teclado (tabla de abajo).
4. Menú **Loto > Build APK**: pasa el proyecto a Android si hace falta y deja `unity/Builds/loto.apk`.
   Con Unity cerrado: `powershell -ExecutionPolicy Bypass -File unity/Tools/build.ps1 -Install`.
5. Instalar: `adb install -r unity/Builds/loto.apk`.
6. Dirección del servidor en el visor (abrir la app una vez antes, para que exista la carpeta):

   ```
   adb shell "echo http://IP-DE-LA-LAPTOP:3000 > /sdcard/Android/data/com.equipo.lotoxr/files/server.txt"
   ```

   Sin ese archivo se usa el campo `baseUrl` del componente `AttemptSender` (por defecto `http://localhost:3000`,
   que desde el visor no sirve). El registro de la app: `adb pull /sdcard/Android/data/com.equipo.lotoxr/files/loto.log`.

Problemas conocidos al compilar o en el visor, y sus arreglos: [`unity/PRIMER-ARRANQUE.md`](unity/PRIMER-ARRANQUE.md).

**Teclas en Play (sin visor):**

| Tecla | Acción |
|---|---|
| `1` | Bajar la palanca |
| `2` | Poner el candado |
| `3` | Verificar |
| `4` | Tocar la máquina |
| `5` | Sacar el candado |
| `6` | Subir la palanca (con el candado puesto, queda bloqueada) |
| `R` | Reintentar (solo cuando el intento terminó) |
| `N` | Siguiente estudiante |
| `G` | Guía siempre prendida (para una demo guiada) |

Orden correcto: `1 2 3 4` (panel verde). Un error: `4` de entrada (panel rojo).

### Servidor de prueba (Node, sin instalar nada)

```
cd server-mock
node server.js
```

Panel del docente en `http://localhost:3000/`. Otro puerto: `PORT=3001 node server.js` (en PowerShell:
`$env:PORT=3001; node server.js`). Desde el visor: `http://<IP-de-la-laptop>:3000`, en la misma red wifi. Es un
servidor de referencia: ver [`server-mock/README.md`](server-mock/README.md).

**Servidor definitivo:** [PENDIENTE: lo escribe Denzel en `server/`, cumpliendo `docs/CONTRATO.md`]

### Pruebas de la lógica (sin abrir Unity)

Desde la raíz, con el SDK de .NET 10:

```
dotnet run --project core-tests
```

Resultado esperado: `174 checks, 0 failed`.

## Estructura

```
unity/                 Proyecto de Unity
  Assets/Loto/Core/      Lógica pura (árbitro de la secuencia, textos de error). Sin UnityEngine
  Assets/Loto/Runtime/   Scripts del visor: palanca, candado, botones, máquina, panel, envío, sonido, guía
  Assets/Loto/Editor/    Arma la escena por código (Loto > Build Scene) y el APK (Loto > Build APK)
  PRIMER-ARRANQUE.md     Pasos de Unity, errores conocidos y arreglos
core-tests/            Pruebas de la lógica (consola .NET)
server-mock/           Servidor de prueba en Node con el panel del docente
docs/CONTRATO.md       Contrato HTTP visor ↔ servidor
docs/PLAN.md           Plan del fin de semana, criterios del jurado y pitch
docs/GUION-PITCH.md    Guion del pitch de 4 minutos (y PREGUNTAS-JURADO, DIAPOSITIVAS, VIDEO-RESPALDO, PRUEBAS-USUARIOS)
docs/AUDITORIA-FABLE.md  Cómo auditar el proyecto y con qué herramientas
```

## Estado (20/11/2026, noche)

| Parte | Estado |
|---|---|
| Lógica (`Loto.Core`) | Probada: 174 comprobaciones, 0 fallas |
| Scripts del visor y armado de escena | **Compilan dentro de Unity** (y fuera, contra las DLL de Unity y de Meta: 0 errores, 0 avisos) |
| Escena | Se armó en Unity el 20/11 con el rig de Meta y las manos. Hay que volver a armarla para que entren las funciones de sonido, tarjeta, repaso y guía |
| APK | El primer intento falló por una librería duplicada (el proyecto estaba en Windows); `Loto > Build APK` ya lo resuelve. Falta compilarlo de nuevo |
| En el visor | **Todavía no se probó**: manos, agarre, sonido y envío al servidor |
| Servidor de prueba | Probado con pedidos reales. El panel no se vio todavía en un navegador |

[PENDIENTE: actualizar después de la primera prueba en el visor]

## Qué se preparó antes del evento y qué se hizo durante

La organización confirmó que se permite traer trabajo previo.

**Antes (19 y 20/11/2026).** La base de este repositorio se preparó el jueves 19 y el viernes 20 de noviembre,
antes de que empezara el evento:

- La idea y la secuencia de cuatro pasos.
- El contrato HTTP entre el visor y el servidor (`docs/CONTRATO.md`).
- La lógica del árbitro (`Loto.Core`) con sus pruebas (`core-tests/`).
- Un servidor de prueba con el panel del docente (`server-mock/`), probado con curl el 20/11.
- El proyecto de Unity (paquetes y ajustes copiados de un proyecto anterior para Quest 3) y los scripts del visor
  en `unity/Assets/Loto/Runtime/`.
- El 20/11 a la noche: los arreglos de la palanca, el candado y la locomoción del rig, y cuatro funciones (sonido y
  energía visible, tarjeta de bloqueo, repaso del error y guía después del error).
- Este README, el plan del fin de semana y las reglas de trabajo del repositorio.

Esa base se escribió con ayuda de asistentes de IA (ver abajo).

**Durante (21-22/11/2026).** [PENDIENTE]

## Uso de IA

- **Para escribir el código:** Claude Code escribió la mayor parte del código y de los documentos. El equipo eligió
  la idea, decidió qué entra y qué no, probó en Unity y aprobó cada cambio. La prueba en el visor está pendiente. Codex (OpenAI) hizo
  revisiones adversariales del plan y del código. [PENDIENTE: completar con lo que se use durante el evento]
- **Para investigar:** Gemini Deep Research (vía NotebookLM) y Grok para el panorama de simuladores existentes y los
  datos de impacto. Cada dato se revisó contra su fuente antes de usarlo; lo que no tenía fuente se descartó.
- **Dentro de la app:** hoy **ningún modelo**. La explicación que ve el estudiante es un texto fijo por cada código
  de error (`Loto.Core.ErrorTexts` en el visor y el mismo texto en el servidor de prueba). La IA nunca decide si un
  intento está bien o mal: eso lo decide siempre el árbitro.
- **Proveedor de IA para las explicaciones:** [POR DEFINIR]. Si se agrega, va del lado del servidor, con la clave en
  una variable de entorno, un tiempo límite corto y el texto fijo como respaldo. **Ninguna clave va al repositorio.**

## Límites conocidos

- **Una sola máquina y un solo procedimiento** (cuatro pasos). No hay catálogo de equipos ni fuentes de energía
  distintas (neumática, hidráulica).
- **La tarjeta de bloqueo es visual:** el árbitro no la cuenta como un paso.
- **No está alineado formalmente con ninguna norma** (OSHA, europea ni boliviana). Enseña el orden básico del bloqueo.
- **El servidor de prueba guarda en memoria:** al reiniciarlo se pierde todo (hay que exportar el CSV antes). No tiene
  usuarios ni contraseña.
- **Los estudiantes son «Estudiante N»:** no se piden nombres ni datos personales.
- Detalles abiertos del visor y su arreglo, en [`unity/PRIMER-ARRANQUE.md`](unity/PRIMER-ARRANQUE.md) §5 y §6.

## Créditos y licencias

- Meta XR SDK y Unity se usan bajo sus propias licencias.
- No se usan modelos 3D, texturas, fuentes ni sonidos de terceros: las figuras se arman con primitivas de Unity y
  los sonidos se generan por código (`ToneSynth.cs`).
- Licencia del código de este repositorio: [POR DEFINIR con el equipo]
