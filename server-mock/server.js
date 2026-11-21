// LOTO simulator - MOCK / REFERENCE server.
// This is a TEST server so the headset has something to talk to before the event.
// The real server is Denzel's, in whatever language he picks. It only has to
// respect docs/CONTRATO.md. No dependencies: node server.js
'use strict';

const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const PORT = Number(process.env.PORT) || 3000;
const HOST = '0.0.0.0';
const MAX_BODY = 64 * 1024; // bytes
const INDEX_FILE = path.join(__dirname, '..', 'web', 'index.html');
const MACHINE_FILE = path.join(__dirname, '..', 'public', 'Motoreductor 3D — Componente LOTO.html');

// In-memory store, newest first. Restarting the server clears it.
const attempts = [];

// Fixed Spanish texts per error code. Codes: Loto.Core.Referee. The "explain" texts are
// the same ones the headset shows (Loto.Core.ErrorTexts.Explain); the titles are the
// teacher's view (third person) and exist only here.
const ERROR_TEXTS = {
  TOUCH_BEFORE_OFF: {
    title: 'Tocó la máquina encendida',
    explain: 'Metiste la mano sin bajar la palanca: la máquina seguía con energía. En un taller real podías perder la mano o recibir una descarga.'
  },
  TOUCH_WITHOUT_LOCK: {
    title: 'Tocó la máquina sin candado',
    explain: 'Apagaste la máquina pero no pusiste tu candado. En un taller real un compañero puede volver a encenderla mientras tienes las manos adentro.'
  },
  TOUCH_WITHOUT_VERIFY: {
    title: 'Tocó la máquina sin verificar',
    explain: 'Pusiste el candado pero no verificaste que la máquina quedó sin energía. En un taller real puede quedar energía guardada y la máquina arranca igual.'
  },
  LOCK_BEFORE_OFF: {
    title: 'Candado con la palanca encendida',
    explain: 'Quisiste poner el candado con la palanca todavía encendida. El candado solo protege si bloquea la palanca en apagado: primero baja la palanca.'
  },
  VERIFY_WITHOUT_LOCK: {
    title: 'Verificó sin candado',
    explain: 'Apretaste verificar sin tener el candado puesto. Sin candado cualquiera puede encender la máquina después de tu prueba: primero pon el candado.'
  },
  LEVER_BLOCKED: {
    title: 'Palanca bloqueada por el candado',
    explain: 'La palanca no sube porque tiene un candado puesto. Para eso sirve el bloqueo: nadie puede encender la máquina hasta que el dueño del candado lo retire.'
  }
};

function fixedExplanation(errorCode) {
  if (!errorCode) return null;
  const t = ERROR_TEXTS[errorCode];
  return t ? t.explain : 'Ese paso no va en este momento. Revisa el orden: apagar, candado, verificar y recién tocar.';
}

// ---------------------------------------------------------------------------
// AI HOOK (Denzel): this is the ONE place where a model call would go.
// Rules: the key comes from an environment variable (e.g. process.env.AI_API_KEY),
// never from the code or the repo; the call has a hard time limit; and if it
// fails, is slow, or there is no key, the fixed text above is the answer.
// The headset must never wait on the model.
//
//   async function aiExplanation(attempt) { ...fetch to the model... return text; }
//
//   const AI_TIMEOUT_MS = 2500;
//   if (process.env.AI_API_KEY) {
//     try {
//       const text = await Promise.race([
//         aiExplanation(attempt),
//         new Promise((_, reject) => setTimeout(() => reject(new Error('timeout')), AI_TIMEOUT_MS))
//       ]);
//       if (text) return text;
//     } catch (e) { /* fall through to the fixed text */ }
//   }
// ---------------------------------------------------------------------------
async function buildExplanation(attempt) {
  return fixedExplanation(attempt.errorCode);
}

function send(res, status, body, type) {
  res.writeHead(status, {
    'Content-Type': type || 'application/json; charset=utf-8',
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET, POST, DELETE, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Cache-Control': 'no-store'
  });
  res.end(body);
}

function sendJson(res, status, obj) {
  send(res, status, JSON.stringify(obj));
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on('data', (c) => {
      size += c.length;
      // Too large: stop keeping the data but let the request finish, so the 413 reaches the client.
      if (size > MAX_BODY) { chunks.length = 0; return; }
      chunks.push(c);
    });
    req.on('end', () => {
      if (size > MAX_BODY) reject(new Error('too large'));
      else resolve(Buffer.concat(chunks).toString('utf8'));
    });
    req.on('error', reject);
  });
}

// Returns a clean attempt, or a string with the validation error.
function parseAttempt(raw) {
  let b;
  try { b = JSON.parse(raw); } catch (e) { return 'invalid JSON'; }
  if (!b || typeof b !== 'object' || Array.isArray(b)) return 'body must be a JSON object';
  if (typeof b.ok !== 'boolean') return '"ok" must be true or false';
  const str = (v, max) => (typeof v === 'string' ? v.slice(0, max) : '');
  return {
    session: str(b.session, 80),
    student: str(b.student, 80) || 'sin nombre',
    ok: b.ok,
    errorCode: typeof b.errorCode === 'string' && b.errorCode ? b.errorCode.slice(0, 40) : null,
    step: Number.isFinite(b.step) ? b.step : 0,
    seconds: Number.isFinite(b.seconds) ? b.seconds : 0,
    actions: Array.isArray(b.actions) ? b.actions.slice(0, 200).map((a) => String(a).slice(0, 40)) : [],
    clientTime: str(b.clientTime, 40)
  };
}

function csvCell(v) {
  let s = v === null || v === undefined ? '' : String(v);
  // Keep spreadsheets from running a cell as a formula.
  if (/^[=+\-@\t\r]/.test(s)) s = "'" + s;
  return /[",\r\n]/.test(s) ? '"' + s.replace(/"/g, '""') + '"' : s;
}

function toCsv() {
  const head = ['id', 'serverTime', 'clientTime', 'session', 'student', 'ok', 'errorCode', 'errorTitle', 'step', 'seconds', 'actions'];
  const rows = attempts.map((a) => [
    a.id, a.serverTime, a.clientTime, a.session, a.student, a.ok, a.errorCode,
    a.errorCode && ERROR_TEXTS[a.errorCode] ? ERROR_TEXTS[a.errorCode].title : '',
    a.step, a.seconds, a.actions.join(' ')
  ].map(csvCell).join(','));
  // BOM so Excel reads the accents.
  return String.fromCharCode(0xFEFF) + [head.join(',')].concat(rows).join('\r\n') + '\r\n';
}

const server = http.createServer(async (req, res) => {
  try {
    // Path only. Not new URL(): a request line like "//" makes it throw.
    const pathname = String(req.url || '/').split('?')[0];
    const route = req.method + ' ' + pathname;
    if (req.method === 'OPTIONS') return send(res, 204, '');

    if (route === 'POST /api/attempts') {
      let raw;
      try { raw = await readBody(req); } catch (e) { return sendJson(res, 413, { error: 'body too large' }); }
      const attempt = parseAttempt(raw);
      if (typeof attempt === 'string') return sendJson(res, 400, { error: attempt });
      attempt.id = crypto.randomUUID();
      attempt.serverTime = new Date().toISOString();
      attempt.explanation = await buildExplanation(attempt);
      attempts.unshift(attempt);
      console.log('[attempt] ' + attempt.student + ' ok=' + attempt.ok + ' error=' + attempt.errorCode + ' step=' + attempt.step);
      return sendJson(res, 201, { id: attempt.id, explanation: attempt.explanation });
    }

    if (route === 'GET /api/attempts') return sendJson(res, 200, attempts);

    if (route === 'GET /api/attempts.csv') {
      res.setHeader('Content-Disposition', 'attachment; filename="intentos-loto.csv"');
      return send(res, 200, toCsv(), 'text/csv; charset=utf-8');
    }

    if (route === 'DELETE /api/attempts') {
      const deleted = attempts.length;
      attempts.length = 0;
      return sendJson(res, 200, { deleted: deleted });
    }

    if (route === 'GET /api/errors') return sendJson(res, 200, ERROR_TEXTS);
    if (route === 'GET /api/health') return sendJson(res, 200, { ok: true, attempts: attempts.length });

    if (route === 'GET /' || route === 'GET /index.html') {
      return send(res, 200, fs.readFileSync(INDEX_FILE), 'text/html; charset=utf-8');
    }

    if (route === 'GET /maquina.html') {
      return send(res, 200, fs.readFileSync(MACHINE_FILE), 'text/html; charset=utf-8');
    }

    return sendJson(res, 404, { error: 'not found' });
  } catch (e) {
    console.error(e);
    return sendJson(res, 500, { error: 'server error' });
  }
});

server.on('error', (e) => {
  if (e.code === 'EADDRINUSE') {
    console.error('Port ' + PORT + ' is already in use. Try another one: PORT=3001 node server.js');
    process.exit(1);
  }
  throw e;
});

server.listen(PORT, HOST, () => {
  console.log('LOTO mock server (TEST only) on http://localhost:' + PORT);
  console.log('Page: http://localhost:' + PORT + '/   panel: /#estadisticas   pitch: /?proyector=1');
  console.log('From the headset use http://<laptop-ip>:' + PORT);
});
