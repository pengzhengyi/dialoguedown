import { describe, it, expect } from "vitest";
import { summaryRuns, type SummaryRole } from "./summary-runs";

/** Real summaries, as the projection writes them for the shipped examples. */
const REAL: [string, string][] = [
    ["control", "ShowBackground(tavern, firelit); PlayMusic(tavern_lute)"],
    ["control", "(fade in)"],
    ["control", "⇒ The Mountain Road"],
    ["control", "CONTINUE"],
    ["control", "IF Hero.IsBrave THEN (fade out)"],
    ["line", "<anonymous>: Rain hammers the shutters of the Salted Hart."],
    ["line", "Keeper: You have the look of someone chasing trouble."],
    ["line", "Guide: What will you do, {playerName}?"],
    ["line", "IF Alice.HasMap THEN Alice: And I still have the old map — good."],
    ["line", "Alice: <no speech>"],
    ["line", "<unknown>: Hello."],
    ["choice", "Turn back to the crossroads || Follow the moonlight ahead"],
    ["choice", "Brave the west road IF Alice.HasMap || Stay put"],
    ["choice", "Go left || <no label>"],
    ["random-choice", "DRAW 1 FROM 2: {Hero.Attack} || {Dragon.Fury}"],
    ["random-choice", "DRAW 1 FROM 3: 50% || 25% || 25%"],
    ["random-choice", "DRAW 1 FROM 2: 50% IF Hero.HasMap || evenly"],
    ["branch", "IF Alice.HasMap THEN 16 ELSE 18"],
    ["branch", "IF A THEN 5 ELSE IF B THEN 9 ELSE 14"],
    ["end", "END"],
];

/** The runs' texts, joined — which must always be the summary they came from. */
function rejoin(summary: string, kind: string): string {
    return summaryRuns(summary, kind)
        .map((run) => run.text)
        .join("");
}

/** The roles a summary's runs carry, in order, with consecutive duplicates collapsed. */
function roles(summary: string, kind: string): SummaryRole[] {
    return summaryRuns(summary, kind)
        .map((run) => run.role)
        .filter((role, index, all) => role !== all[index - 1]);
}

/** The text of every run carrying a role. */
function textOf(summary: string, kind: string, role: SummaryRole): string[] {
    return summaryRuns(summary, kind)
        .filter((run) => run.role === role)
        .map((run) => run.text);
}

describe("summaryRuns", () => {
    // Splitting must never lose or invent a character: the cell shows the runs, so anything the
    // split drops is a word the reader never sees.
    it.each(REAL)("puts %s summaries back together exactly", (kind, summary) => {
        expect(rejoin(summary, kind)).toBe(summary);
    });

    it("names the speaker and leaves the speech whole", () => {
        expect(textOf("Keeper: You have the look.", "line", "speaker")).toEqual(["Keeper"]);
        expect(textOf("Keeper: You have the look.", "line", "plain")).toContain(
            "You have the look.",
        );
    });

    it("treats a nameless speaker as still standing in the speaker's place", () => {
        expect(textOf("<anonymous>: Rain hammers.", "line", "speaker")).toEqual(["<anonymous>"]);
    });

    it("picks out a query inside speech", () => {
        expect(textOf("Guide: What will you do, {playerName}?", "line", "query")).toEqual([
            "{playerName}",
        ]);
    });

    it("leads with the node's own condition as keywords", () => {
        expect(roles("IF Alice.HasMap THEN Alice: And I still have it.", "line")).toEqual([
            "keyword",
            "plain",
            "keyword",
            "plain",
            "speaker",
            "separator",
            "plain",
        ]);
    });

    it("names each command a control performs", () => {
        expect(
            textOf("ShowBackground(tavern, firelit); PlayMusic(tavern_lute)", "control", "command"),
        ).toEqual(["ShowBackground(tavern, firelit)", "PlayMusic(tavern_lute)"]);
    });

    it("reads a branch's chain as keywords around its targets", () => {
        expect(textOf("IF A THEN 5 ELSE IF B THEN 9 ELSE 14", "branch", "keyword")).toEqual([
            "IF",
            "THEN",
            "ELSE",
            "IF",
            "THEN",
            "ELSE",
        ]);
    });

    // The count belongs to the phrase around it, not to the odds: how many arms there are is
    // already visible in the odds themselves, so it steps back with the words it sits among.
    it("reads a random choice's whole draw phrase as one keyword", () => {
        expect(textOf("DRAW 1 FROM 3: 50% || 25% || 25%", "random-choice", "keyword")).toEqual([
            "DRAW 1 FROM 3",
        ]);
    });

    it("names the odds a random choice draws against", () => {
        expect(
            textOf("DRAW 1 FROM 2: {Hero.Attack} || {Dragon.Fury}", "random-choice", "query"),
        ).toEqual(["{Hero.Attack}", "{Dragon.Fury}"]);
    });

    it("marks an absent value apart from a value that is there", () => {
        expect(textOf("Alice: <no speech>", "line", "absent")).toEqual(["<no speech>"]);
        expect(textOf("Go left || <no label>", "choice", "absent")).toEqual(["<no label>"]);
    });

    // The reason this is safe to do in the client: a writer's own words are taken whole and never
    // scanned, so punctuation they typed cannot be read as syntax.
    it("never reads a writer's brackets as a command", () => {
        const summary = "Keeper: He said (quietly) that ShowBackground(x) means nothing here.";

        expect(textOf(summary, "line", "command")).toEqual([]);
        expect(rejoin(summary, "line")).toBe(summary);
    });

    it("never splits a line's speech on a separator a writer typed", () => {
        const summary = "Keeper: Choose: north || south; either way, go.";

        expect(textOf(summary, "line", "separator")).toEqual([":"]);
        expect(rejoin(summary, "line")).toBe(summary);
    });

    // The cap can cut a summary mid-construct, so the split has to survive an unfinished one.
    it.each(REAL)("survives a %s summary cut short", (kind, summary) => {
        const cut = `${summary.slice(0, Math.max(1, summary.length - 6))}…`;

        expect(() => summaryRuns(cut, kind)).not.toThrow();
        expect(rejoin(cut, kind)).toBe(cut);
    });

    it("gives a kind it does not know one plain run", () => {
        expect(summaryRuns("something else", "no-such-kind")).toEqual([
            { text: "something else", role: "plain" },
        ]);
    });

    it("gives an empty summary no runs to draw", () => {
        expect(rejoin("", "line")).toBe("");
    });
});
