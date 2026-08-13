# Third-party bundled libraries

The compilation report is built by the `web/` Vite project into a single,
self-contained HTML file (`web/dist/report.html`) with all JavaScript and CSS
inlined, so a generated report works fully offline with no CDN or network
access. The libraries below are bundled into that file. Exact versions are
pinned by `web/package-lock.json`; **none** of them is a NuGet or runtime
dependency of the DialogueDown packages — they ship only inside generated
report HTML.

| Library                                                                    | Version | License      |
| -------------------------------------------------------------------------- | ------- | ------------ |
| [D3.js](https://d3js.org)                                                  | 7.9.0   | ISC          |
| [DOMPurify](https://github.com/cure53/DOMPurify)                           | 3.4.13  | MIT          |
| [Mermaid](https://mermaid.js.org)                                          | 11.16.1 | MIT          |
| [Pico.css](https://picocss.com)                                            | 2.1.1   | MIT          |
| [marked](https://marked.js.org)                                            | 12.0.2  | MIT          |
| [marked-gfm-heading-id](https://github.com/markedjs/marked-gfm-heading-id) | 3.2.0   | MIT          |
| [highlight.js](https://highlightjs.org)                                    | 11.11.1 | BSD-3-Clause |
| [Tippy.js](https://atomiks.github.io/tippyjs/)                             | 6.3.7   | MIT          |
| [Popper](https://popper.js.org) (bundled by Tippy.js)                      | 2.11.8  | MIT          |
| [Fira Code](https://github.com/tonsky/FiraCode) (via Fontsource)           | 5.3.0   | OFL-1.1      |

To update a library, bump it in `web/package.json`, run `npm install` in
`web/`, then rebuild (`npm run build`) and commit the refreshed
`web/dist/report.html` and `web/package-lock.json`.

## Licenses

### D3.js — ISC

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

### DOMPurify, Mermaid, Pico.css, marked, marked-gfm-heading-id, Tippy.js, and Popper — MIT

These are distributed under the MIT License (Copyright, respectively: Mario
Heiderich and DOMPurify contributors; Mermaid contributors; 2019-2024 Pico.css
contributors; 2011-2024 Christopher Jeffrey; 2023 marked contributors
(marked-gfm-heading-id); 2017-2021 atomiks (Tippy.js); 2019 Federico Zivolo
(Popper)). The MIT License permits use, copy, modification, and distribution
provided the copyright and permission notice are retained; the full notice is
preserved in each package's distribution and at the projects' repositories.

### highlight.js — BSD-3-Clause

Copyright (c) 2006, Ivan Sagalaev. Redistribution and use in source and binary
forms, with or without modification, are permitted provided that the copyright
notice, the list of conditions, and the disclaimer are retained, and the name of
the author may not be used to endorse products derived from this software without
specific prior written permission. The full license text ships in the package
(`highlight.js/LICENSE`) and at the project's repository.

### Fira Code — SIL Open Font License 1.1

Copyright 2014-2020 The Fira Code Project Authors
(<https://github.com/tonsky/FiraCode>). DialogueDown embeds the unmodified Latin
400 WOFF2 supplied by `@fontsource/fira-code`; the full license is preserved in
[`FIRA_CODE_LICENSE.txt`](FIRA_CODE_LICENSE.txt).
