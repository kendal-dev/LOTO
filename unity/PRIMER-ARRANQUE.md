# Primer arranque de Unity — LOTO XR

Escrito el viernes 20/11, 15:00. **Unity todavía no abrió este proyecto ni una vez.**

## 1. Qué está probado y qué no

| Parte | Estado |
|---|---|
| Lógica (`Loto.Core`) | Probada: `dotnet run --project core-tests` |
| Scripts de `Runtime/` y `Editor/` | **Compilan fuera de Unity** (compilador de C# contra las DLL de Unity 6000.4.6f1 y las del SDK de Meta ya compiladas en Walkback), con las tres combinaciones: todo, sin manos, sin manos ni rig. **Nunca compilaron dentro de Unity.** Después de ese chequeo se tocaron 4 archivos (sección 5) que no se volvieron a compilar |
| Shader `Loto/UI` | Sin compilar ni ver |
| Menú `Loto > Build Scene` | **Nunca se ejecutó.** La escena `Loto.unity` no existe todavía |
| Play con teclas | Nunca se probó |
| Manos, agarre, passthrough, visor | Nunca se probó nada |
| Build del APK y `Tools/build.ps1` | Nunca se ejecutaron |
| Envío al servidor desde Unity | Nunca se probó |

## 2. Pasos

1. **Cerrar** el navegador y todas las apps. **Enchufar** la laptop (no desenchufar con Unity abierto).
2. Unity Hub → Add → carpeta `C:\Users\kevin\abstracthon-loto\unity` → abrir con **6000.4.6f1**.
   Si Unity se cae con `d3d12: device removed`, abrirlo con el argumento `-force-d3d11` (pasó en Walkback).
3. Esperar la importación: **10 a 20 minutos** (no hay carpeta `Library`). No cambiar de ventana.
   Si aparece un cartel de Meta (Project Setup Tool, OpenXR), cerrarlo sin aplicar nada.
4. Mirar la consola. Si hay errores rojos `error CS…`, ir a la sección 3.
5. Menú **Loto > Build Scene**. En la consola tiene que salir
   `[Loto] Scene built: … | Meta rig: yes | hands: yes`.
6. **Play.** Sin visor se prende la cámara de escritorio. Teclas:
   `1` bajar palanca · `2` poner candado · `3` verificar · `4` tocar la máquina · `5` sacar candado ·
   `6` subir palanca · `R` reintentar (solo cuando terminó) · `N` siguiente estudiante.
   Probar el orden bueno (1 2 3 4 → panel verde) y uno malo (4 de entrada → panel rojo).
   En Play sin visor Meta escribe avisos amarillos (manos sin datos, "ground could not be found"): se esperan.
7. Servidor (opcional en esta prueba): en otra terminal `node server.js` dentro de `server-mock`;
   cada intento que termina tiene que aparecer en `http://localhost:3000/`.
8. Build al Quest: menú **Loto > Build APK** (sale `Builds\loto.apk`), o con Unity cerrado
   `powershell -ExecutionPolicy Bypass -File Tools\build.ps1 -Install`.
   Instalar a mano: `adb install -r Builds\loto.apk`.
9. Dirección del servidor en el visor (la app se abre una vez antes, para que exista la carpeta):
   `adb shell "echo http://IP-DE-LA-LAPTOP:3000 > /sdcard/Android/data/com.equipo.lotoxr/files/server.txt"`
   La app lee `server.txt` una sola vez, al arrancar (`AttemptSender.Awake`): después de escribirlo, cerrarla con
   `adb shell am force-stop com.equipo.lotoxr` y abrirla de nuevo.
   El registro de la app: `adb pull /sdcard/Android/data/com.equipo.lotoxr/files/loto.log`. La línea
   `[Loto] Server:` tiene que mostrar la IP de la laptop.

La escena se rehace siempre con el menú. No se edita `Loto.unity` a mano.

## 3. Los 5 lugares donde más puede fallar la compilación

Los cinco ya pasaron el compilador fuera de Unity; estos son los puntos que dependen de algo que
ese chequeo no cubre.

| # | Dónde | Síntoma | Arreglo |
|---|---|---|---|
| 1 | `Editor/LotoSceneBuilder.Hands.cs` (todo lo del Interaction SDK) | `error CS…` en ese archivo | Comentar su **primera línea** (`#define LOTO_HANDS`). La escena sale sin agarre y con dos botones de toque, PALANCA y CANDADO |
| 2 | `Editor/LotoSceneBuilder.Rig.cs` (rig y passthrough) | `error CS…` en ese archivo | Comentar su primera línea (`#define LOTO_RIG`) y usar **Loto > Build Scene (Desktop only)** para seguir probando el flujo con teclas |
| 3 | `Runtime/FingerTip.cs`, línea `FindObjectsByType<Hand>()` | `error CS…` en esa línea | Volver a `FindObjectsByType<Hand>(FindObjectsSortMode.None)` (es solo un aviso de obsoleto) |
| 4 | `Runtime/Lever.cs` y `Runtime/Padlock.cs` (`OneGrabRotateTransformer`, `Grabbable`) | No encuentra `Oculus.Interaction` | Los paquetes de Meta no terminaron de importar: Window > Package Manager, esperar, y revisar que `Packages/manifest.json` diga 209.0.0 |
| 5 | `Shaders/LotoUI.shader` | `Shader not found: Loto/UI` al armar la escena, o texto rosado | En `LotoSceneBuilder.cs` cambiar `"Loto/UI"` por `"UI/Default"` (el borde de las letras puede dejar ver el cuarto en passthrough) |

Fallos que no son de compilación, ya previstos:

- **La palanca solo gira para el lado equivocado en el visor:** en el objeto `Lever`, campo `direction`, cambiar `-1` por `1`.
- **El rig de manos no se conecta a la cámara:** la consola dice `Auto-wiring failed` o `Wired by hand`. El constructor ya lo conecta a mano; si igual falla, es el plan B.
- **Giro o teletransporte sin querer:** el rig de Meta trae locomoción. Desactivar a mano el objeto `Locomotor` dentro de `OVRComprehensiveInteractionRig`.

## 4. Plan B si las manos fallan

En orden, del más barato al más caro:

1. **Botones en vez de agarre (2 minutos).** En `LotoSceneBuilder.cs` poner
   `TouchButtonsForLeverAndPadlock = true` y repetir **Loto > Build Scene**. Aparecen PALANCA y CANDADO en la
   mesa; se tocan con la punta del índice y hacen lo mismo que las teclas 1/6 y 2/5. El agarre sigue estando.
2. **Sin Interaction SDK.** Comentar la primera línea de `LotoSceneBuilder.Hands.cs`. Salen los mismos dos
   botones, pero las esferas de los dedos siguen a los **mandos** (sin ese SDK no hay puntas de dedo).
   **Se usa con los mandos en la mano:** con las manos libres la esfera queda en la muñeca, no en el índice.
   Si hay que usarlo sin mandos, en los objetos `FingerTipLeft` y `FingerTipRight` subir `fallbackOffset.z`
   de `0.06` a unos `0.15` (sin probar).
3. **Puntero de pellizco como en Walkback** (30 minutos o más, no está hecho): copiar `Pointer.cs` y
   `PointerButton.cs` de `walkback-v1.1\Assets\Walkback\App\` y `SceneBuilder.Pointer.cs`, y hacer que el
   pellizco sobre un botón llame a `VerifyButton.Press()`. Solo si 1 y 2 no alcanzan.

Regla de 30 minutos: si el agarre no sale en 30 minutos, se queda la opción 1 y se dice en el pitch que la
palanca y el candado se accionan con un toque.

## 5. Revisión del 20/11 (tarde): qué se corrigió y qué sigue abierto

Corregido en el código, **sin volver a pasar por el compilador** (son cambios de pocas líneas) y sin probar en el visor:

- `FingerTip.cs`: el respaldo de los mandos nunca se activaba (miraba un ancla que no se mueve). Ahora mira
  al padre del ancla. Falta ver en el visor que la esfera amarilla sigue al mando.
- `AttemptSender.cs`: se callaron los dos avisos amarillos CS0649 de la clase `Reply`.
- `StatusPanel.cs`: la explicación del servidor se corta a 220 caracteres para que no tape «Toca REINTENTAR».

🆕 **20/11, 16:20 — A, B y C arreglados en el código, y D nuevo** (Claude). Compilado con `dotnet build` del
`Assembly-CSharp-Editor.csproj` que generó Unity, contra sus DLL y las del SDK de Meta: **0 errores, 0
avisos**. Pruebas de la lógica: 174 de 174. **Nada de esto se probó todavía en el visor.**

| # | Qué se cambió | Qué mirar en el visor |
|---|---|---|
| A | `Lever.cs` tiene `grabbable` (lo conecta `LotoSceneBuilder.Hands.cs`). Si REINTENTAR llega con la palanca agarrada, la palanca espera a que la mano la suelte y recién ahí vuelve a ENCENDIDA; mientras tanto no informa nada | Agarrar la palanca, apretar REINTENTAR con la otra mano, soltar: tiene que volver arriba y el intento nuevo empieza limpio |
| B | La caja de agarre de la palanca quedó solo en el pomo: `center (0, LeverArm + 0.03, 0)`, `size (0.07, 0.085, 0.07)` | Que el pomo rojo se agarre fácil. Si cuesta, agrandar el `size` en Y de a 1 cm |
| C | `AttemptSender.cs` numera los envíos: una explicación del servidor solo se muestra si es del último intento y el intento sigue terminado | Con el servidor de prueba no se nota (responde al instante). En consola puede salir `Late explanation ignored` |
| D | 🆕 `LotoSceneBuilder.Hands.cs`: con `DisableLocomotion = true` (por defecto), al armar la escena se apagan los objetos del rig cuyo nombre dice `Locomot`, `Teleport` o `Turner`, salvo los que tengan interactores de agarre o de toque | La consola lista lo que apagó: `[Loto] Locomotion switched off (N): …`. Si después algo de las manos no anda, poner `DisableLocomotion = false` y volver a armar la escena |

## 6. 🆕 20/11 noche — el primer APK falló, y 4 funciones nuevas

### 6.1 Por qué falló el APK y cómo quedó arreglado

**Síntoma:** cartel *Build failure … MergeNativeLibsTask*. En el registro:
`2 files found with path 'lib/arm64-v8a/libopenxr_loader.so'` (uno del paquete OpenXR de Unity y otro dentro de OVRPlugin).

**Causa** (leída en `com.meta.xr.sdk.core/Editor/OVRGradleGeneration.cs`): el paso de Meta que deshabilita el loader
de Unity mira `EditorUserBuildSettings.selectedBuildTargetGroup`, la plataforma **elegida en el editor**. El
proyecto recién abierto estaba en **Windows**: Meta salteó su paso y quedaron dos loaders. Walkback no lo sufrió
porque ya estaba en Android.

**Arreglo:** `Loto > Build APK` ahora pasa el proyecto a Android antes de compilar (`SwitchActiveBuildTarget`). La
primera vez Unity reimporta varios minutos. A mano es lo mismo: *File > Build Profiles > Android > Switch Platform*.
En el registro tiene que aparecer `[Meta] libopenxr_loader.so from other packages will be disabled …`.

### 6.2 Las 4 funciones nuevas (elegidas por un juez entre 3 propuestas; revisadas por 3 lentes + Codex)

Compiladas contra Unity: 0 errores, 0 avisos. Pruebas de la lógica: 174 de 174. **Sin probar en Play ni en el visor.**

| Función | Archivos | Cómo se prueba en Play |
|---|---|---|
| **Repaso del error** en el panel: el paso saltado en rojo «(TE LO SALTASTE)» y la línea «Tu orden: …» | `StatusPanel.cs`, caja Steps a 268 px | `4` → línea 1 en rojo y «Tu orden: Tocar». `R`, `1 2 3 4` → verde con «Tu orden: Apagar > Candado > Verificar > Tocar» |
| **Tarjeta PELIGRO · NO OPERAR** colgada del candado, con «Bloqueado por: Estudiante N» y la hora (la «T» de LOTO, solo visual) | `LockoutTag.cs` (nuevo), grupo `Tag` en `BuildPadlock` | `1 2` → aparece. `5` → se va. `2` sin `1` → no aparece |
| **Energía que se ve y se oye:** zumbido mientras hay energía, lámpara roja/gris, pitido por paso, doble bip en los avisos, «clac» + «PRUEBA DE ARRANQUE / NO ARRANCA» al verificar, tres notas al completar y zumbador al frenar | `ToneSynth.cs` y `EnergyFeedback.cs` (nuevos), lámpara, cartel y 2 AudioSource en `BuildMachine` | Con parlantes: zumbido al arrancar · `1` se apaga · `2` pitido · `3` clac y cartel 2,5 s · `4` tres notas. `4` de entrada: zumbador |
| **Guía al reintentar:** un alfiler sobre el objeto del paso que falta, rojo al fallar y ámbar al reintentar. Apagado en el primer intento. Tecla `G` = siempre prendido | `StepPointer.cs` (nuevo), `BuildStepPointer` | `4` → alfiler rojo sobre el pomo. `R` → ámbar; sigue el paso. `N` lo apaga. `G` lo deja prendido |

Arreglados después de la revisión adversarial (3 de 3 verificadores): el cartel «PRUEBA DE ARRANQUE» se cortaba y
no mostraba «NO ARRANCA» (canvas a 760 px y 0,38 m), y el cartel y el parpadeo seguían si se volvía a dar energía
enseguida. Rechazados por los verificadores: 2 hallazgos (0 de 3).

**En el visor, mirar:** que el audio quede en la grabación del Quest (si satura: `sfxVolume` 0,4 y `humVolume` 0,25),
que la tarjeta no atraviese el poste amarillo y que el alfiler del paso 2 no se confunda con el pomo (si pasa:
`lift2` = 0,14).

---

### (de §5) Abierto antes del cambio de las 16:20 — A, B y C ya arreglados en el código (se deja escrito tal cual, regla 2):

| # | Qué pasa | Qué hacer en la demo | Arreglo si sobra tiempo |
|---|---|---|---|
| A | Si se aprieta REINTENTAR **con la palanca todavía agarrada**, la palanca queda desfasada: no vuelve a ENCENDIDA y puede pasarse de largo | **Soltar la palanca antes de REINTENTAR.** Si ya pasó: cerrar y abrir la app | En `Lever.cs`, una referencia al `Grabbable` y postergar `ForceAngle(0)` hasta que `SelectingPointsCount` sea 0 (hay que conectarla en `LotoSceneBuilder.Hands.cs`) |
| B | Con el candado puesto, la zona de agarre de la palanca y la del candado se pisan: al tironear la palanca se puede agarrar el candado, y eso lo saca y borra la verificación | Tironear la palanca **desde el pomo rojo**, no desde el brazo | En `LotoSceneBuilder.cs` (`BuildLever`), dejar la caja solo en el pomo: `center = (0, LeverArm + 0.03, 0)`, `size = (0.07, 0.085, 0.07)`. Queda a 1 cm del candado. Los números que propuso la revisión (`LeverArm - 0.02`, alto `0.10`) **no sirven**: esa caja sigue pisando el candado. Probar en el visor que la palanca todavía se agarra bien |
| C | Si el estudiante falla, reintenta y vuelve a fallar en menos de 5 s, la explicación del servidor del primer error puede salir bajo el título del segundo | Con el servidor de prueba (responde al instante) no pasa. Esperar 5 s entre intentos si la explicación la escribe un modelo | En `AttemptSender.Send`, guardar `FailCode` y `studentNumber` y mostrar la explicación solo si siguen iguales al llegar la respuesta |
