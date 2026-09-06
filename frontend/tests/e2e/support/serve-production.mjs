// Serves the production build the way a deployment serves it, so the budgets measure the product
// rather than a development server.
//
// The Angular development server does two things that make L2-104 unmeasurable against it: it
// serves the bundle uncompressed, and it adds its own module machinery on top. A deployment puts
// the built files behind something that gzips them — 483 KB becomes 109 KB — and that difference
// is most of the budget. Measuring the dev server would fail a target the deployment meets several
// times over, and would be measuring the build tool.
//
// So: static files with gzip, an SPA fallback to index.html, and /api proxied to the API exactly
// as proxy.conf.json does for the ordinary suite. Nothing here is used outside the budget run.

import { createReadStream } from 'node:fs';
import { readFile, stat } from 'node:fs/promises';
import { createServer, request as httpRequest } from 'node:http';
import { extname, join, normalize, resolve } from 'node:path';
import { createGzip } from 'node:zlib';

// Resolved and normalised, so the prefix check below compares like with like. A relative root
// keeps forward slashes while `join` produces backslashes on Windows, and every asset then failed
// the check and fell through to index.html - which looked like a working server serving nothing.
const root = resolve(process.argv[2] ?? join('dist', 'barnabas', 'browser'));
const port = Number(process.argv[3] ?? 4302);
const apiPort = Number(process.argv[4] ?? 5003);

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.ico': 'image/x-icon',
  '.svg': 'image/svg+xml',
  '.woff2': 'font/woff2',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
};

/** What a deployment would compress. Fonts and images are already compressed. */
const compressible = new Set(['.html', '.js', '.css', '.json', '.svg']);

const server = createServer(async (incoming, outgoing) => {
  const url = new URL(incoming.url ?? '/', `http://localhost:${port}`);

  if (url.pathname === '/api' || url.pathname.startsWith('/api/')) {
    proxy(incoming, outgoing, url);

    return;
  }

  // Normalised and prefix-checked, so a request for ../../etc/passwd reads nothing.
  const candidate = normalize(join(root, decodeURIComponent(url.pathname)));

  const file = candidate.startsWith(root) && (await isFile(candidate))
    ? candidate

    // Anything that is not a file is a route the client owns, and the client is index.html.
    : join(root, 'index.html');

  const extension = extname(file);

  outgoing.setHeader('Content-Type', types[extension] ?? 'application/octet-stream');

  // Hashed file names, so everything but the entry document is immutable.
  outgoing.setHeader(
    'Cache-Control',
    extension === '.html' ? 'no-cache' : 'public, max-age=31536000, immutable',
  );

  const wantsGzip = (incoming.headers['accept-encoding'] ?? '').includes('gzip');

  if (wantsGzip && compressible.has(extension)) {
    outgoing.setHeader('Content-Encoding', 'gzip');
    outgoing.setHeader('Vary', 'Accept-Encoding');

    createReadStream(file).pipe(createGzip({ level: 9 })).pipe(outgoing);

    return;
  }

  const bytes = await readFile(file);

  outgoing.setHeader('Content-Length', bytes.length);
  outgoing.end(bytes);
});

function proxy(incoming, outgoing, url) {
  const forwarded = httpRequest(
    {
      host: 'localhost',
      port: apiPort,
      method: incoming.method,

      // The same rewrite proxy.conf.json applies: the API does not know it is behind /api.
      path: url.pathname.replace(/^\/api/, '') + url.search,
      headers: { ...incoming.headers, host: `localhost:${apiPort}` },
    },
    (answer) => {
      outgoing.writeHead(answer.statusCode ?? 502, answer.headers);
      answer.pipe(outgoing);
    },
  );

  forwarded.on('error', () => {
    outgoing.writeHead(502);
    outgoing.end();
  });

  incoming.pipe(forwarded);
}

async function isFile(path) {
  try {
    return (await stat(path)).isFile();
  } catch {
    return false;
  }
}

server.listen(port, () => console.log(`Serving ${root} on http://localhost:${port}`));
