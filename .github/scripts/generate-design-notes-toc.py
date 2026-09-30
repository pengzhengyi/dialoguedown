#!/usr/bin/env python3
"""Build the design-notes sidebar from the reading guide.

The reading guide in docs/contributing/design-notes/README.md lists every note, in
reading order, under a heading per area. The site sidebar, toc.yml, lists the same
notes in the same order. Writing both by hand lets them drift apart, so toc.yml is
generated from the guide: each `###` heading becomes a group, each `####` heading a
subgroup, and each table row that links a note becomes an entry.

Usage:
    python3 .github/scripts/generate-design-notes-toc.py           # rewrite toc.yml
    python3 .github/scripts/generate-design-notes-toc.py --check   # fail if it is stale
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys
import urllib.parse

NOTES = pathlib.Path("docs/contributing/design-notes")
GUIDE_HEADING = "## Reading guide"

# Guide headings that read as sentences get a short sidebar label.
SIDEBAR_NAMES = {
    "Core: the compiler pipeline": "Core",
    "Runtime: playing a compiled script": "Runtime",
}

NOTE_LINK = re.compile(r"\[([^\]]+)\]\(\./([^)]+\.md)\)")


def build_toc(readme: str) -> str:
    guide = readme[readme.index(GUIDE_HEADING):]
    lines = ["- name: All design notes", "  href: README.md"]
    in_group = False
    indent = "  "
    for line in guide.splitlines():
        if group := re.match(r"^### (.+)$", line):
            name = SIDEBAR_NAMES.get(group[1], group[1])
            lines += [f"- name: {name}", "  items:"]
            in_group, indent = True, "  "
        elif subgroup := re.match(r"^#### (.+)$", line):
            lines += [f"  - name: {subgroup[1]}", "    items:"]
            indent = "    "
        elif in_group and line.startswith("|") and (link := NOTE_LINK.search(line)):
            title = link[1].replace("\\<", "<").replace("\\>", ">")
            href = urllib.parse.unquote(link[2])
            lines += [f"{indent}- name: {title}", f"{indent}  href: {href}"]
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true",
                        help="report a stale toc.yml instead of rewriting it")
    args = parser.parse_args()

    toc_path = NOTES / "toc.yml"
    expected = build_toc((NOTES / "README.md").read_text(encoding="utf-8"))

    if not args.check:
        toc_path.write_text(expected, encoding="utf-8")
        return 0
    if toc_path.read_text(encoding="utf-8") == expected:
        return 0
    print(f"{toc_path} does not match the reading guide. Regenerate it with:\n"
          "    python3 .github/scripts/generate-design-notes-toc.py", file=sys.stderr)
    return 1


if __name__ == "__main__":
    sys.exit(main())
