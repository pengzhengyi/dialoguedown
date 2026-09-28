# Served Client Packaging

> [!NOTE]
> Status: **implemented**. A served report links its client as immutable, content-hashed assets
> and fetches Mermaid only when a script draws a diagram; an exported report inlines what it needs
> and works offline.

## Goal and scope

Opening a script should cost the reader almost nothing beyond compiling it. This note owns how the
built client reaches the browser: which parts are linked, which are inlined, and how they are
cached. The routes are in [Served Shell](./Served%20Shell.md#http-surface); in-place switching,
which removes the remaining page load, is in [Explorer](./Explorer.md).

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Page** | The per-document HTML with its injected payload. Changes every open. |
| **Client** | The built `report.js` and `report.css`. The same for every document and session. |
| **Mermaid build** | Mermaid's self-contained `mermaid.js`, which assigns `globalThis.mermaid`. |
| **Export** | A report written by `ddown visualize <script> -o <path>`. |

## The three shapes

| Shape | Page | Client | Mermaid |
| --- | --- | --- | --- |
| Served | `no-store` | linked from `/assets/<hash>`, `immutable` | fetched from `/assets/<hash>` on the first diagram |
| Exported, no diagram | one file | inlined | absent |
| Exported, with a diagram | one file | inlined | inlined |

`ReportBundle` splits the built page into the page, the client script, the stylesheet (fonts and
icons already inlined), and the Mermaid build, and names each asset after a hash of its content.
`ReportAssets` serves them; `MermaidFence` decides whether an export needs Mermaid.

## Key design decisions

### D1 — Split the constant client from the variable page

Served HTML must be `no-store`: it is per-session and per-document. When the client was inlined in
that page, every open re-downloaded, re-parsed, and re-executed the whole client. Serving the
client separately under content-addressed names lets it be `public, max-age=31536000, immutable`:
a rebuilt client is a different URL, so a stale asset is never served. A stable URL also lets the
browser reuse its compiled code cache, not just the bytes.

| Warm open, 44-character script | Inlined client | Linked client |
| --- | --- | --- |
| Click to seven tabs | 990–1990 ms | 270–320 ms |
| Parse | 158–507 ms | 15–20 ms |
| Transfer per open | 1.86 MB | 4.7 kB |

### D2 — Fetch Mermaid on demand instead of bundling it

Mermaid (with cytoscape, KaTeX, and every diagram type) is most of the client's weight and almost
no script uses it. The client does not import Mermaid: `mermaid-loader.ts` returns the global when
the page already carries one, and otherwise fetches the URL the page names.

| Measure | Mermaid bundled | On demand |
| --- | --- | --- |
| Served first load (gzip) | 1,949 kB | 676 kB |
| Export, no diagram | 4,778 kB | 1,417 kB |
| Export, with a diagram | 4,778 kB | 4,898 kB |

The cost falls on the rare case: a script with a diagram fetches Mermaid's whole build, and an
export carrying it is slightly larger.

### D3 — No CDN

Loading the client or Mermaid from a third party would make a localhost tool call out, break
air-gapped use, allow version drift, and be slower behind a restrictive network. Everything ships
in `DialogueDown.Visualization` as embedded resources.

### D4 — When unsure, an export carries Mermaid

`MermaidFence` reads a fence's first info-string word, lowercased, as the preview does, but does
not track nesting. A false positive only makes a file larger; a false negative would export a
diagram that cannot draw.

## Error and boundary cases

| Case | Behavior |
| --- | --- |
| Asset name the client was not built from | `404`; names are matched whole, never resolved as paths. |
| Export with no diagram shows a Mermaid fence at runtime | Not possible for the exported text; a missing source is an error rather than a wait. |
| Exported report opened from `file://` | Renders with zero network requests. |

## Testability

- An exported report links nothing; a served page links its hashed assets.
- The asset route serves only known names, with the immutable header.
- `MermaidFence` answers yes for every preview-recognized fence and for doubtful nesting.
- `report-bundle-size.test.mjs` holds the client under 2,000,000 bytes and Mermaid under
  5,000,000 bytes, and reports the measured size on failure.
- Browser: a cold served shell fetches only the client and stylesheet; a script with a fence also
  fetches Mermaid and draws; an export with a diagram renders offline.
