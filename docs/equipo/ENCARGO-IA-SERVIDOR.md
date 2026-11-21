# Encargo para Denzel: IA en el servidor — «la regla decide, la IA explica»

**Para qué:** el jurado puntúa Innovación con 40 y el premio incluye una maestría en IA. Hoy la app no usa IA por
dentro. Con esto sí, y de una forma que se puede defender: **si el paso está bien o mal lo sigue decidiendo una regla
fija en el visor** (nunca un modelo), y la IA solo **explica** el error con palabras de la máquina y de lo que hizo
ese estudiante. Si la IA falla o no hay internet, sale el texto fijo de siempre.

**Tiempo estimado:** 2 a 3 horas. **No toca el visor ni Unity:** el visor ya muestra el texto que devuelve el servidor.

---

## 1. Qué llega del visor (ya funciona)

`POST /api/attempts` con este JSON (detalle en [`../CONTRATO.md`](../CONTRATO.md)):

```json
{
  "session": "sesion-2026-11-21",
  "student": "Estudiante 3",
  "ok": false,
  "errorCode": "TOUCH_WITHOUT_VERIFY",
  "step": 3,
  "seconds": 14.2,
  "actions": ["LeverOff", "LockOn", "TouchMachine"],
  "clientTime": "2026-11-21T22:05:31Z",
  "machine": "PRENSA"
}
```

- `machine` es **nuevo y opcional**: `"MOLINO"`, `"PRENSA"` o `"NEUMÁTICA"`. Si no viene, es el molino.
- `actions` es lo que hizo, en orden: `LeverOff` (bajó la palanca), `LockOn` (puso el candado), `Verify` (verificó),
  `TouchMachine` (tocó), `LockOff` (sacó el candado), `LeverOn` (subió la palanca).

## 2. Qué tiene que responder

```json
{ "id": "…", "explanation": "Pusiste el candado pero tocaste la prensa sin verificar. …" }
```

- `explanation`: **2 o 3 frases, menos de 220 caracteres** (el panel del visor corta lo que sobra), en español,
  tuteo, sin markdown. `null` si `ok` es `true`.
- **Responder en menos de 4 segundos.** El visor espera 5 y después sigue con su texto fijo. Poné un tiempo límite de
  3,5 s a la llamada al modelo; si se pasa o falla, devolvé el **texto de respaldo** de la tabla de `CONTRATO.md` §3.
- **Nunca un 500** por culpa del modelo: el intento se guarda igual.

## 3. Qué tiene que saber el modelo (prompt de sistema)

```text
Sos el instructor de seguridad de un simulador de bloqueo y etiquetado (LOTO) para estudiantes de institutos técnicos
de Bolivia. Un estudiante acaba de cometer un error y la simulación se frenó. Explicale en 2 o 3 frases cortas, en
español y tuteando: qué hizo mal según su secuencia, qué le habría pasado en un taller real con ESTA máquina, y cuál
es el paso que le faltó. Menos de 220 caracteres. Sin markdown, sin listas, sin saludar.

Los pasos correctos, en orden: 1) bajar la palanca (apagar), 2) poner su candado con su tarjeta, 3) verificar que no
arranca (en la prensa y la neumática, antes PURGAR la presión hasta que el manómetro marque 0), 4) recién tocar.

Máquinas:
- MOLINO: molino de martillos. Energía eléctrica y la inercia de la polea, que sigue girando unos segundos.
- PRENSA: prensa hidráulica. Energía eléctrica de la bomba y presión de aceite guardada: el pistón puede bajar
  aunque la bomba esté apagada.
- NEUMÁTICA: estación neumática de una envasadora. Aire comprimido guardado en la línea: el cabezal sellador puede
  bajar aunque el compresor esté apagado.

No inventes normas ni cifras. No digas que la IA decidió el error: el error ya lo decidió el simulador.
```

Mensaje de usuario (armado por el servidor con los datos del intento):

```text
Máquina: PRENSA. Error: TOUCH_WITHOUT_VERIFY (tocó con el candado puesto pero sin verificar).
Lo que hizo, en orden: bajó la palanca, puso el candado, tocó la máquina. Tardó 14,2 s.
```

## 4. Ejemplo en Node (sin dependencias, Node 18 o más)

Elegí el proveedor que quieras. Este ejemplo usa la API de Anthropic con un modelo rápido (`claude-haiku-5-5`).
La clave va en la variable de entorno `ANTHROPIC_API_KEY` (o en `.env`, que está en `.gitignore`). **Nunca en el
repositorio.**

```js
const SYSTEM_PROMPT = `…el prompt de sistema de §3…`;
const FALLBACK = { /* los textos de respaldo de CONTRATO.md §3, por código */ };
const ACTIONS_ES = { LeverOff: 'bajó la palanca', LockOn: 'puso el candado', Verify: 'verificó',
  TouchMachine: 'tocó la máquina', LockOff: 'sacó el candado', LeverOn: 'subió la palanca' };

async function explain(attempt) {
  if (attempt.ok) return null;
  const fallback = FALLBACK[attempt.errorCode] ||
    'Ese paso no va en este momento. Revisa el orden: apagar, candado, verificar y recién tocar.';
  if (!process.env.ANTHROPIC_API_KEY) return fallback;
  const user = `Máquina: ${attempt.machine || 'MOLINO'}. Error: ${attempt.errorCode}.\n` +
    `Lo que hizo, en orden: ${(attempt.actions || []).map(a => ACTIONS_ES[a] || a).join(', ')}. ` +
    `Tardó ${attempt.seconds} s.`;
  try {
    const res = await fetch('https://api.anthropic.com/v1/messages', {
      method: 'POST',
      signal: AbortSignal.timeout(3500),
      headers: {
        'content-type': 'application/json',
        'x-api-key': process.env.ANTHROPIC_API_KEY,
        'anthropic-version': '2023-06-01',
      },
      body: JSON.stringify({ model: 'claude-haiku-5-5', max_tokens: 200, system: SYSTEM_PROMPT,
        messages: [{ role: 'user', content: user }] }),
    });
    if (!res.ok) return fallback;
    const data = await res.json();
    const text = (data.content && data.content[0] && data.content[0].text || '').trim();
    return text ? text.slice(0, 220) : fallback;
  } catch (e) {
    return fallback;   // sin internet, tiempo límite o error del proveedor: el texto fijo
  }
}
```

En el `POST /api/attempts`: guardás el intento, `attempt.explanation = await explain(attempt)` y respondés.

## 5. Extra para el panel del docente (si sobra tiempo, 1 hora)

`GET /api/summary` → `{ "summary": "…" }`: el modelo lee los intentos de la sesión y escribe 2 frases para el
docente, por ejemplo *«5 de 8 estudiantes fallaron en verificar, sobre todo en la prensa: repasen purgar la presión
antes de tocar.»* Con los conteos calculados por el servidor (no por el modelo), y el modelo solo los redacta. El de
web lo muestra arriba del panel con un botón «Resumen de la clase». Mismo tiempo límite y respaldo («Sin resumen
por ahora.»).

## 6. Cómo probarlo

```powershell
curl.exe -X POST http://localhost:3000/api/attempts -H "Content-Type: application/json" --data-binary "@intento.json"
```

Con `intento.json` = el JSON de §1. Tiene que volver la explicación de la prensa en menos de 4 s. Después, sin
internet (o sin la variable de la clave): tiene que volver el texto de respaldo, sin error.

## 7. Qué decir en el pitch (solo si anda)

> «La regla decide, la IA explica. Si el paso está bien o mal lo decide una regla fija, porque en seguridad la nota no
> puede depender de un modelo que se equivoca. La IA le explica a cada estudiante su error con su máquina, y le
> resume al docente en qué falla la clase.»

Si no llegó a andar: no se menciona. Se dice solo lo que la app hace.
