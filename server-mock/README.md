# server-mock — servidor de PRUEBA

**Esto no es el servidor del proyecto.** Es un servidor de referencia para que el visor
tenga con quién hablar antes del evento y para que el contrato se pueda probar.
El definitivo lo hace Denzel, en el lenguaje que elija; solo tiene que cumplir
[`docs/CONTRATO.md`](../docs/CONTRATO.md).

## Correr

```
node server.js            # puerto 3000
PORT=8080 node server.js  # otro puerto (PowerShell: $env:PORT=8080; node server.js)
```

Sin `npm install`: usa solo módulos nativos de Node (probado con v24.19 el 20/11/2026:
todas las rutas de la tabla, JSON inválido, cuerpo demasiado grande y borrado).
Si el puerto está ocupado avisa y termina; se elige otro con `PORT`.

- Panel del docente: `http://localhost:3000/`
- Desde el visor: `http://<IP-de-la-laptop>:3000` (misma red wifi; la IP sale con `ipconfig`).
  Si no conecta, revisar el Firewall de Windows para Node en redes privadas.

## Qué hace

| Ruta | Qué hace |
|---|---|
| `POST /api/attempts` | Guarda un intento. Responde `201 { id, explanation }` |
| `GET /api/attempts` | Lista de intentos, el más nuevo primero |
| `GET /api/attempts.csv` | La misma lista en CSV |
| `DELETE /api/attempts` | Borra todo (botón del panel) |
| `GET /api/errors` | Títulos y textos en español por código (lo usa el panel) |
| `GET /api/health` | `{ ok: true }` para probar la conexión |
| `GET /` | Panel del docente (se actualiza cada 2 s) |

## Qué NO hace

- No guarda en disco: al reiniciar se pierde todo.
- No tiene usuarios ni contraseña.
- No llama a ningún modelo de IA. La explicación sale de un diccionario fijo.
  El lugar donde iría la llamada está marcado en `server.js` con `AI HOOK`:
  clave por variable de entorno, tiempo límite, y el texto fijo como respaldo.
