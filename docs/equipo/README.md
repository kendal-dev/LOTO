# Bienvenida al equipo — Simulador LOTO en realidad mixta

Para quien se suma hoy (sábado 21/11). Leé esto entero (5 minutos), después [`MODULOS.md`](MODULOS.md) y tu lista
de tareas. Si sos el desarrollador web: [`TAREAS-WEB.md`](TAREAS-WEB.md).

## Qué es

Un simulador de **bloqueo y etiquetado** (LOTO, *lockout/tagout*) para estudiantes de electromecánica, en **Meta
Quest 3 con realidad mixta**: el estudiante ve una máquina en el aula real y tiene que hacer cuatro pasos en orden.

1. Bajar la palanca (cortar la energía).
2. Poner su candado (cuelga una tarjeta PELIGRO con quién bloqueó y la hora).
3. Apretar VERIFICAR (prueba de arranque: «no arranca»).
4. Recién ahí, tocar la máquina.

Si toca la máquina antes, la simulación **se frena** y le explica el error; desde ese error, un **alfiler** le
señala el paso que falta. **Cada intento llega a un panel web del docente**: quién completó, en qué paso se equivoca
la clase y cuánto tarda cada uno.

Hackathon **Abstracthon 2026** (Santa Cruz). Criterios: Innovación 40 · Viabilidad técnica 30 · Impacto 30. Pitch de
4 minutos el domingo 22/11 a las 17:00.

## Las horas que mandan

| Cuándo | Qué |
|---|---|
| **Sábado 15:00** | Se congelan las funciones. Después solo se arreglan errores |
| Sábado 17:00 | Pruebas con 3 personas (el panel tiene que registrar los intentos) |
| Sábado 18:00 | Video de respaldo (se graba también el panel) |
| **Domingo 12:30** | Se congela el código |
| **Domingo 16:00** | Entrega: repo público en GitHub |

## Quién es quién

| Persona | Qué hace |
|---|---|
| Daniel | El visor (Unity), el video y el pitch. Decide |
| Denzel | El servidor (API) y la entrega |
| Desarrollador web (nuevo) | El panel del docente y la página del proyecto ([`TAREAS-WEB.md`](TAREAS-WEB.md)) |
| Orlando | Pruebas con personas, tiempos, fotos |
| Claude (IA) | Código del visor, documentos, presentación. Codex revisa en contra |

## Reglas de la casa (cortas)

1. **Cada uno escribe solo en su carpeta** ([`MODULOS.md`](MODULOS.md)). Lo que une las partes es
   [`docs/CONTRATO.md`](../CONTRATO.md): si algo de la API tiene que cambiar, se cambia primero ahí y se avisa.
2. **Se dice solo lo que la app hace.** Nada de datos inventados en pantalla: si no hay intentos, se muestra vacío.
3. **Ninguna clave en el repo** (van en `.env`, que está ignorado).
4. **Commits chicos**, con mensaje que diga qué se hizo. Sin coautores de IA en el mensaje.
5. **Sin librerías pesadas ni dependencias nuevas** salvo que haga falta de verdad: el panel corre sin internet
   (en el evento el WiFi puede fallar).

## Cómo correr lo tuyo en 1 minuto

```powershell
node server-mock/server.js
```

Abre `http://localhost:3000/` (el panel). Para mandar intentos de prueba sin el visor, los `curl` de
[`docs/CONTRATO.md`](../CONTRATO.md) §4. No hace falta Unity ni el visor para trabajar en la web.
