# Tareas de Denzel — servidor y entrega

Leé antes [`README.md`](README.md) (5 min) y [`docs/CONTRATO.md`](../CONTRATO.md). Hora límite de funciones:
**sábado 15:00**. Entrega: **domingo 16:00**.

## Dónde estamos (sábado, mediodía)

- **La cadena completa ya anduvo:** a las 12:01 el Quest mandó un intento real (Estudiante 1, completo, 8,4 s) y
  llegó al servidor de prueba en la laptop de Daniel (`http://10.10.32.61:3000`). O sea: el contrato funciona tal
  cual y no hay que tocarlo.
- El servidor que corre es `server-mock/server.js`: Node puro, sin dependencias, 208 líneas. Guarda en memoria (si
  se reinicia, se pierde todo), atiende el panel en `/` y tiene CORS abierto.
- Se sumó un **desarrollador web** que se encarga de la vista del panel ([`TAREAS-WEB.md`](TAREAS-WEB.md)). Vos sos
  dueño de **todo lo que pasa detrás** de la API.

## D1 · Decidir el servidor (10 min, **ya**)

Recomendación: **no reescribirlo.** Ya anda con el visor y quedan 3 horas hasta el congelado.
- Copiá `server-mock/` a `server/` (tu carpeta) y desde ahí es tuyo: el README del repo pasa a decir que el
  servidor es `server/`.
- Si preferís otro lenguaje o tu propio servidor, la condición es una sola: **la prueba de punta a punta con el
  Quest a las 13:00**. Si a esa hora no pasa, queda el que ya funciona (PLAN, plan B).

**Terminado cuando:** está decidido y se lo dijiste a Daniel y al web.

## D2 · Ponerse de acuerdo con el web (5 min)

- El panel es `public/index.html` de tu servidor; él lo edita y vos no lo tocás. Vos no tocás su HTML; él no toca
  tu `server.js`.
- Hoy el servidor sirve **solo** `index.html`. Si él necesita otro archivo (CSS, imágenes, un JS aparte), agregás
  una ruta de archivos estáticos para `public/`, o él mete todo dentro del HTML.
- Él lee `GET /api/attempts` cada 2 s y `GET /api/errors`. Nada más cambia.

## D3 · Que no se pierdan los intentos al reiniciar (≈ 30 min) — lo más útil

Hoy un reinicio borra todo, y a la tarde hay pruebas con personas y el domingo se muestra el panel.
- Al recibir cada intento, además de guardarlo en memoria, **escribirlo en `data/attempts.json`** (o una línea por
  intento en `data/attempts.jsonl`). Al arrancar, leer ese archivo si existe.
- `DELETE /api/attempts` sigue borrando todo (memoria y archivo), para limpiar entre tandas.
- `data/` va al `.gitignore` (tiene nombres de la prueba).
- **No cambia la API ni el contrato.**

**Terminado cuando:** mandás 3 intentos con `curl` (CONTRATO §4), reiniciás el servidor y siguen en el panel.

## D4 · La red del evento (≈ 20 min)

- La IP de la laptop de Daniel en el WiFi del evento es `10.10.32.61` (puede cambiar si se reconecta:
  `ipconfig`, la línea «IPv4» de la red WiFi).
- Si Windows pregunta si Node puede usar la red: **permitir** (privadas y públicas).
- Probar desde el celular, en el mismo WiFi: abrir `http://<IP>:3000/api/health` tiene que dar `{"ok":true,...}`.
- **Plan B:** si el WiFi del evento aísla los dispositivos (el celular no llega), hotspot del celular de Daniel
  para la laptop y el Quest.
- La dirección que usa el visor está en el Quest, en `server.txt`. Si cambia la IP, avisale a Daniel o a Claude:
  se reescribe y se reabre la app.

**Terminado cuando:** el celular abre `/api/health` por WiFi, y está anotado qué hacer si cambia la IP.

## D5 · Las pruebas con personas (sábado 17:00–17:45)

- **Antes de las 17:00:** borrar los intentos de práctica (botón Borrar o `DELETE /api/attempts`).
- Durante: mirar que cada intento aparezca. Si uno no llega, anotar la hora (Daniel mira el registro del visor).
- **17:45: exportar el CSV** (`/api/attempts.csv`) y guardarlo en dos lugares. Son los números reales del pitch.

## D6 · Domingo: correr en la laptop de Daniel (18:45 del sábado, dejarlo probado)

El domingo todo corre en casa de Daniel. Dejá escrito en tu `server/README.md` el único comando para levantarlo y
cómo probarlo con `curl`, y probalo en su laptop antes de irte.

## D7 · La entrega (domingo, hasta las 16:00)

1. **12:30:** código congelado. Último commit tuyo antes de esa hora.
2. **15:30:** revisar el repo público desde otra cuenta o en una ventana privada: que se vea el README, que no haya
   claves (`.env` está ignorado) ni archivos pesados (los `.apk` están ignorados).
3. **Antes de las 16:00:** mandar el enlace por el canal oficial que dijo la organización y **guardar la captura del
   comprobante**.

## Lo que no se hace

- Cambiar la API o el contrato sin avisar a Daniel (el visor ya está compilado contra él).
- Subir claves, tokens o el CSV con nombres al repo.
- Agregar una IA que genere las explicaciones: cada error ya tiene su texto fijo, y solo entraba si había una clave
  andando antes de las 13:00. Si querés intentarlo, que sea opcional y que sin clave quede el texto fijo.

## Preguntas que probablemente tengas

| Pregunta | Respuesta |
|---|---|
| ¿El visor manda algo más que el intento final? | No: un `POST` por intento, al completar o cuando un error frena (CONTRATO §1). |
| ¿Qué `student` llega? | `"Estudiante N"`: no se piden nombres ni datos personales. |
| ¿Y si el servidor tarda? | El visor espera 5 s; si no hay respuesta, sigue con su propio texto y ese intento no queda registrado. |
| ¿Puedo usar Express o una base de datos? | Se puede, pero suma dependencias y riesgo a 3 horas del congelado. Un archivo JSON alcanza (D3). |
| ¿Quién arregla el panel si se rompe? | El web. Vos, si la API devuelve mal los datos. |
| ¿Dónde pruebo sin el Quest? | Con los `curl` de CONTRATO §4. El panel está en `http://localhost:3000/`. |
