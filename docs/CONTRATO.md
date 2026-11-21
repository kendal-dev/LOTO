# Contrato HTTP — visor ↔ servidor

Una página. El visor (Unity, Quest 3) manda cada intento; el servidor lo guarda,
devuelve una explicación y lo muestra en el panel del docente.
Servidor de referencia que ya lo cumple: [`server-mock/`](../server-mock/README.md).

`{base}` = dirección del servidor, por ejemplo `http://192.168.1.50:3000`.

## 1. Mandar un intento

`POST {base}/api/attempts` · `Content-Type: application/json`

Se manda **un intento por cada final**: cuando el estudiante completa la secuencia o
cuando un error frena la simulación.

| Campo | Tipo | Qué es |
|---|---|---|
| `session` | string | Identificador de la clase o tanda (lo define el visor) |
| `student` | string | Nombre o código del estudiante |
| `ok` | bool | `true` si completó la secuencia; `false` si un error la frenó |
| `errorCode` | string o `null` | Código del error que frenó (tabla de abajo). `null` si no hubo |
| `step` | int | Paso en el que estaba: 1 apagar · 2 candado · 3 verificar · 4 tocar la máquina · 5 completó |
| `seconds` | number | Segundos desde que empezó el intento |
| `actions` | [string] | Acciones en orden: `LeverOff`, `LockOn`, `Verify`, `TouchMachine`, `LockOff`, `LeverOn` |
| `clientTime` | string | Hora del visor en ISO 8601 |

```json
{
  "session": "taller-a-2026-11-21",
  "student": "Ana Rojas",
  "ok": false,
  "errorCode": "TOUCH_WITHOUT_LOCK",
  "step": 2,
  "seconds": 12.4,
  "actions": ["LeverOff", "TouchMachine"],
  "clientTime": "2026-11-21T14:05:31Z",
  "machine": "PRENSA"
}
```

Respuesta `200` o `201`:

```json
{ "id": "2d515756-406c-4199-852a-8636a0260dc7", "explanation": "Apagaste la máquina pero no pusiste tu candado. ..." }
```

`explanation` es el texto que el visor le muestra al estudiante; `null` si no hubo error.
Si el JSON viene mal o falta `ok`, `400 { "error": "..." }`. El servidor de prueba
responde `413` si el cuerpo pasa de 64 KB.

`machine` (agregado el 21/11 a la tarde) es **opcional**: la máquina del escenario, `"MOLINO"`, `"PRENSA"` o
`"NEUMÁTICA"`. Un servidor que no lo conoce lo ignora; uno que lo usa (la explicación con IA) adapta el texto a la
máquina. Si no viene, la máquina es el molino.

Cómo lo arma hoy el visor (`AttemptSender.BuildJson`): los ocho campos, siempre y en
ese orden, más `machine` al final cuando hay varias máquinas; `errorCode` es un `null` de verdad cuando no hubo error; `seconds` con un
decimal y punto (`12.4`); `clientTime` en UTC con el formato `2026-11-21T14:05:31Z`;
`student` es `"Estudiante N"` y `session`, si nadie la define, `"sesion-AAAA-MM-DD"`.
El visor espera 5 segundos; si no hay respuesta sigue con su propio texto.

## 2. Leer

- `GET {base}/api/attempts` → lista en JSON, **el más nuevo primero**. Cada elemento trae
  los campos de arriba más `id`, `serverTime` y `explanation`.
- `GET {base}/api/attempts.csv` → la misma lista en CSV.

El servidor de prueba agrega `DELETE /api/attempts`, `GET /api/errors` y `GET /api/health`.
Son para el panel; el visor no depende de ellas.

## 3. Códigos de error

Los códigos los decide el visor (clase `Referee`). El servidor no los calcula: los recibe.

**Frenan la simulación** (son los que llegan en `errorCode` con `ok: false`):

| Código | Qué pasó | Texto de respaldo |
|---|---|---|
| `TOUCH_BEFORE_OFF` | Tocó la máquina con la palanca encendida | Metiste la mano sin bajar la palanca: la máquina seguía con energía. En un taller real podías perder la mano o recibir una descarga. |
| `TOUCH_WITHOUT_LOCK` | Apagó, pero tocó sin candado | Apagaste la máquina pero no pusiste tu candado. En un taller real un compañero puede volver a encenderla mientras tienes las manos adentro. |
| `TOUCH_WITHOUT_VERIFY` | Candado puesto, tocó sin verificar | Pusiste el candado pero no verificaste que la máquina quedó sin energía. En un taller real puede quedar energía guardada y la máquina arranca igual. |

**No frenan** (el estudiante sigue; hoy el visor no los manda como intento, pero el
servidor tiene que aceptar el código si llega):

| Código | Qué pasó | Texto de respaldo |
|---|---|---|
| `LOCK_BEFORE_OFF` | Candado con la palanca encendida | Quisiste poner el candado con la palanca todavía encendida. El candado solo protege si bloquea la palanca en apagado: primero baja la palanca. |
| `VERIFY_WITHOUT_LOCK` | Verificó sin candado | Apretaste verificar sin tener el candado puesto. Sin candado cualquiera puede encender la máquina después de tu prueba: primero pon el candado. |
| `LEVER_BLOCKED` | Quiso encender con el candado puesto | La palanca no sube porque tiene un candado puesto. Para eso sirve el bloqueo: nadie puede encender la máquina hasta que el dueño del candado lo retire. |

Un código desconocido se guarda igual y se responde con un texto genérico. Nunca un 500.

Los textos de respaldo de las dos tablas son, letra por letra, los de
`Loto.Core.ErrorTexts.Explain` en el visor: si cambia uno, cambia el otro. El texto
genérico es *"Ese paso no va en este momento. Revisa el orden: apagar, candado,
verificar y recién tocar."* Los títulos cortos no forman parte del contrato: el visor
le habla al estudiante ("Tocaste la máquina sin candado") y el panel al docente
("Tocó la máquina sin candado").

## 4. Probar con curl

```
curl -X POST http://localhost:3000/api/attempts \
  -H "Content-Type: application/json" \
  -d '{"session":"s1","student":"Ana Rojas","ok":false,"errorCode":"TOUCH_BEFORE_OFF","step":1,"seconds":6.5,"actions":["TouchMachine"],"clientTime":"2026-11-21T14:05:31Z"}'

curl http://localhost:3000/api/attempts
curl -o intentos.csv http://localhost:3000/api/attempts.csv
```

En PowerShell las comillas simples con JSON adentro fallan con `curl.exe`: conviene
guardar el JSON en `intento.json` y usar `curl.exe --data-binary "@intento.json" ...`.

## 5. Lo que el servidor tiene que cuidar

- **Red.** El visor tiene que poder llegar al servidor: o los dos están en la **misma red
  wifi** y se usa la IP de la laptop (`http://192.168.x.x:3000`, escuchando en `0.0.0.0`,
  no solo en `localhost`), o el servidor está publicado con **https**. Android bloquea por
  defecto el `http` sin cifrar; del lado de Unity se habilita para la red local, pero
  una dirección pública tiene que ser https.
- **CORS abierto** (`Access-Control-Allow-Origin: *`) y responder al `OPTIONS`.
- **Responder rápido.** El visor espera pocos segundos y sigue sin la respuesta. Si la
  explicación la escribe un modelo de IA: tiempo límite corto y, si no llega, el texto
  de respaldo de la tabla. La clave del modelo va en una variable de entorno, nunca
  en el repositorio.
- **No confiar en los datos.** `student` es texto libre: escaparlo en el panel y en el CSV.
