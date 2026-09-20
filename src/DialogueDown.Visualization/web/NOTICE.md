# Third-party bundled libraries

The compilation report is built by the `web/` Vite project into a single,
self-contained HTML file (`web/dist/report.html`) with all JavaScript and CSS
inlined, so a generated report works fully offline with no CDN or network
access. The principal direct libraries and separately attributed assets below
are bundled into that file. Exact direct and transitive versions are pinned by
`web/package-lock.json`; **none** of them is a NuGet or runtime dependency of
the DialogueDown packages — they ship only inside generated report HTML.

| Library                                                                    | Version      | License    |
| -------------------------------------------------------------------------- | ------------ | ---------- |
| [CodeMirror](https://codemirror.net/)                                      | 6.x packages | MIT        |
| [D3.js](https://d3js.org)                                                  | 7.9.0        | ISC        |
| [DOMPurify](https://github.com/cure53/DOMPurify)                           | 3.4.13       | Apache-2.0 |
| [GitHub Slugger](https://github.com/Flet/github-slugger)                   | 2.0.0        | ISC        |
| [Lezer Highlight](https://github.com/lezer-parser/highlight)               | 1.2.3        | MIT        |
| [Mermaid](https://mermaid.js.org)                                          | 11.16.1      | MIT        |
| [Pico.css](https://picocss.com)                                            | 2.1.1        | MIT        |
| [TanStack Table Core](https://tanstack.com/table/)                         | 9.1.2        | MIT        |
| [VS Code Codicons](https://github.com/microsoft/vscode-codicons)           | 0.0.46-24    | CC-BY-4.0  |
| [marked](https://marked.js.org)                                            | 18.0.9       | MIT        |
| [marked-gfm-heading-id](https://github.com/markedjs/marked-gfm-heading-id) | 4.1.4        | MIT        |
| [Tippy.js](https://atomiks.github.io/tippyjs/)                             | 6.3.7        | MIT        |
| [Popper](https://popper.js.org) (bundled by Tippy.js)                      | 2.11.8       | MIT        |
| [Fira Code](https://github.com/tonsky/FiraCode) (via Fontsource)           | 5.3.0        | OFL-1.1    |

To update a library, bump it in `web/package.json`, run `npm install` in
`web/`, then rebuild (`npm run build`) and commit the refreshed
`web/dist/report.html` and `web/package-lock.json`.

## Licenses

### D3.js and GitHub Slugger — ISC

```text
Copyright 2010-2023 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
```

GitHub Slugger carries the same ISC terms, copyright 2015 Dan Flettre.

### DOMPurify — Apache-2.0

DialogueDown uses DOMPurify under the Apache License 2.0 option of its
`(MPL-2.0 OR Apache-2.0)` dual license. The complete license is preserved in
[`DOMPURIFY_LICENSE.txt`](DOMPURIFY_LICENSE.txt).

### CodeMirror, Lezer, Mermaid, Pico.css, TanStack Table, marked, Tippy.js, and Popper — MIT

These libraries and their directly listed companion packages are distributed
under the MIT License. The MIT License permits use, copy, modification, and
distribution provided the copyright and permission notice are retained; the
full notices are preserved in each package's distribution and at the projects'
repositories.

### VS Code Codicons — CC-BY-4.0

Codicons are copyright Microsoft Corporation and contributors, licensed under
Creative Commons Attribution 4.0 International. DialogueDown uses the packaged
font and CSS without modifying the icon artwork. The complete terms are
preserved in `node_modules/@vscode/codicons/LICENSE` and at the
[Codicons repository](https://github.com/microsoft/vscode-codicons).

### Fira Code — SIL Open Font License 1.1

Copyright 2014-2020 The Fira Code Project Authors
(<https://github.com/tonsky/FiraCode>). DialogueDown embeds the unmodified Latin
400 WOFF2 supplied by `@fontsource/fira-code`; the full license is preserved in
[`FIRA_CODE_LICENSE.txt`](FIRA_CODE_LICENSE.txt).
