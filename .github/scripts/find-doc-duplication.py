#!/usr/bin/env python3
"""Report near-duplicate prose across the documentation tree.

Documentation drifts when one concept is explained in two places: the copies are
rarely identical, so no diff catches them, and the one that is not updated
becomes quietly wrong. This finds those pairs by comparing word shingles, which
catches paraphrase — the kind of duplication a substring search misses.

Usage:
    python3 .github/scripts/find-doc-duplication.py [--threshold 0.28] [--top 20]

Exits non-zero when a pair scores at or above the threshold and is not listed in
ALLOWED_PAIRS, so it can gate a release.
"""

from __future__ import annotations

import argparse
import itertools
import pathlib
import re
import sys

# Overlap that is expected, because the two documents serve different readers.
# A guide teaches a writer the syntax; a design note records why the construct
# has that shape. Both legitimately show the same example.
ALLOWED_PAIRS = {
    ("docs/guide/structure-and-flow.md",
     "docs/contributing/design-notes/Random Choice.md"),
    ("docs/guide/structure-and-flow.md",
     "docs/contributing/design-notes/Conditional Choice.md"),
    # The corpus README is the language-neutral specification a runtime implementer reads — it
    # ships beside the fixtures and is meant to be enough on its own, for someone writing a
    # runner in another language who will never open this repository's design notes. The note
    # is the design record for the corpus itself. Both must state the matching rules.
    ("conformance/README.md",
     "docs/contributing/design-notes/Conformance Corpus.md"),
}

# Generated or vendored trees. `/bin/` and `/obj/` matter because a test project copies the
# conformance corpus — README and all — into its build output for every target framework, so a
# single authored document reappears as an exact duplicate of itself once per framework.
SKIP = ("node_modules", "/_site/", "/api/", "/web/", "/bin/", "/obj/")
MIN_PARAGRAPH_CHARS = 160
SHINGLE_SIZE = 6


def documents(root: pathlib.Path) -> list[pathlib.Path]:
    return [
        p for p in sorted(root.rglob("*.md"))
        if not any(s in str(p) for s in SKIP) and not str(p).endswith(".dialogue.md")
    ]


def paragraphs(path: pathlib.Path) -> list[str]:
    """Prose paragraphs only — code, tables, and callouts carry their own repetition."""
    text = path.read_text()
    text = re.sub(r"```.*?```", "", text, flags=re.S)
    text = re.sub(r"^\|.*$", "", text, flags=re.M)
    text = re.sub(r"^>.*$", "", text, flags=re.M)
    out = []
    for para in re.split(r"\n\s*\n", text):
        p = " ".join(para.split())
        p = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", p)   # keep link text, drop the URL
        p = re.sub(r"`[^`]*`", "CODE", p)                # identifiers are not prose
        if len(p) >= MIN_PARAGRAPH_CHARS and not p.startswith(("#", "-")):
            out.append(p)
    return out


def shingles(text: str) -> set[tuple[str, ...]]:
    words = text.lower().split()
    return {tuple(words[i:i + SHINGLE_SIZE])
            for i in range(max(0, len(words) - SHINGLE_SIZE + 1))}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--threshold", type=float, default=0.28)
    parser.add_argument("--top", type=int, default=20)
    parser.add_argument("--root", default=".")
    args = parser.parse_args()

    root = pathlib.Path(args.root)
    docs = documents(root)
    items = [
        (str(f.relative_to(root)), text, shingles(text))
        for f in docs
        for text in paragraphs(f)
    ]

    findings = []
    for (f1, p1, s1), (f2, p2, s2) in itertools.combinations(items, 2):
        if f1 == f2 or not s1 or not s2:
            continue
        overlap = len(s1 & s2) / len(s1 | s2)
        if overlap >= args.threshold:
            findings.append((overlap, f1, f2, p1, p2))

    findings.sort(reverse=True, key=lambda t: t[0])
    allowed = {tuple(sorted(pair)) for pair in ALLOWED_PAIRS}
    flagged = [f for f in findings if tuple(sorted((f[1], f[2]))) not in allowed]

    print(f"scanned {len(docs)} documents, {len(items)} paragraphs")
    print(f"near-duplicate pairs at or above {args.threshold:.0%}: "
          f"{len(findings)} ({len(flagged)} unexpected)\n")

    for overlap, f1, f2, p1, p2 in flagged[:args.top]:
        print(f"[{overlap:.0%}] {f1}\n      {f2}")
        print(f"   A: {p1[:160]}")
        print(f"   B: {p2[:160]}\n")

    if flagged:
        print("Move the shared explanation into the document that owns the concept, "
              "and link to it from the other. If the overlap is intended — a guide "
              "and a design note addressing different readers — add the pair to "
              "ALLOWED_PAIRS with a reason.")
        return 1

    print("no unexpected duplication")
    return 0


if __name__ == "__main__":
    sys.exit(main())
