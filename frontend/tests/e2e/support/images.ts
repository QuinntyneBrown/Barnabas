import { deflateSync } from 'node:zlib';

/**
 * Real image bytes, made rather than checked in.
 *
 * A binary fixture in the repository is a file nobody can read in a diff and nobody can adjust.
 * These are assembled here, so a test that needs a particular type or a particular size asks for
 * one.
 */

/**
 * The smallest valid JPEG the suite needs.
 *
 * A one-pixel image, byte for byte: the start marker, a baseline quantisation table, a frame and a
 * scan. The API decodes and re-encodes whatever arrives, so what matters is only that these bytes
 * really are a JPEG — which is exactly what the content sniffing they are fed to is checking.
 */
export function aJpeg(): Buffer {
  return Buffer.from(
    '/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0a' +
      'HBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/wAALCAABAAEBAREA/8QAHwAAAQUBAQEB' +
      'AQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1Fh' +
      'ByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZ' +
      'WmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXG' +
      'x8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/9oACAEBAAA/AP3wooooA//Z',
    'base64',
  );
}

/**
 * A PNG of a given size, with something in it worth looking at.
 *
 * The one-pixel JPEG above is all an assertion needs — it only has to *be* an image. A recording
 * of the product needs a picture that reads as a photograph on a placard, and a flat grey block
 * would be a visible blemish in every frame after it is posted.
 *
 * Assembled by hand rather than by a library: PNG is a signature and four chunks, and adding an
 * image-encoding dependency to the test tooling to draw a gradient would cost more than it is
 * worth. The result is a genuine PNG, which is what the upload endpoint sniffs it as before it
 * re-encodes it.
 */
export function aPhotograph(width = 1200, height = 900): Buffer {
  const raw = Buffer.alloc(height * (width * 3 + 1));

  let at = 0;

  for (let y = 0; y < height; y++) {
    // Each scanline is preceded by its filter type. Zero means "stored as-is", which is the
    // cheapest thing to write and the easiest thing to read.
    raw[at++] = 0;

    const down = y / height;

    for (let x = 0; x < width; x++) {
      const across = x / width;

      // A warm wash from the top left, with a soft band across it, so the picture has a subject
      // rather than being one flat colour.
      const band = Math.sin(across * 3.1 + down * 1.4) * 0.5 + 0.5;

      raw[at++] = clamp(196 + band * 44 - down * 92);
      raw[at++] = clamp(168 + band * 36 - down * 74);
      raw[at++] = clamp(132 + band * 24 - down * 52);
    }
  }

  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', header(width, height)),
    chunk('IDAT', deflateSync(raw, { level: 6 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

function header(width: number, height: number): Buffer {
  const ihdr = Buffer.alloc(13);

  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8; // Eight bits per channel.
  ihdr[9] = 2; // Truecolour, no alpha.

  return ihdr;
}

/** One PNG chunk: its length, its type, its data, and the CRC of the last two. */
function chunk(type: string, data: Buffer): Buffer {
  const length = Buffer.alloc(4);

  length.writeUInt32BE(data.length, 0);

  const typed = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);

  crc.writeUInt32BE(crc32(typed), 0);

  return Buffer.concat([length, typed, crc]);
}

const CRC_TABLE = (() => {
  const table = new Uint32Array(256);

  for (let index = 0; index < 256; index++) {
    let value = index;

    for (let bit = 0; bit < 8; bit++) {
      value = value & 1 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
    }

    table[index] = value >>> 0;
  }

  return table;
})();

function crc32(bytes: Buffer): number {
  let crc = 0xffffffff;

  for (const byte of bytes) {
    crc = CRC_TABLE[(crc ^ byte) & 0xff]! ^ (crc >>> 8);
  }

  return (crc ^ 0xffffffff) >>> 0;
}

function clamp(value: number): number {
  return Math.max(0, Math.min(255, Math.round(value)));
}
