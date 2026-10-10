import type { Locator } from "@playwright/test";

/** A color as red, green, and blue from 0 to 255, and alpha from 0 to 1. */
export type Rgba = readonly [number, number, number, number];

/** Read a CSS color in either notation Chrome may compute: `rgb()/rgba()` or `color(srgb ...)`. */
export function parseColor(value: string): Rgba | null {
    const srgb = /color\(srgb\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?\)/.exec(value);
    if (srgb) {
        const [r, g, b] = [srgb[1], srgb[2], srgb[3]].map((c) => Number(c) * 255);
        return [r, g, b, srgb[4] === undefined ? 1 : Number(srgb[4])];
    }

    const rgb = /rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([\d.]+))?\)/.exec(value);
    if (!rgb) return null;
    return [
        Number(rgb[1]),
        Number(rgb[2]),
        Number(rgb[3]),
        rgb[4] === undefined ? 1 : Number(rgb[4]),
    ];
}

/** The opaque color a translucent ink shows as once painted over a backdrop. */
export function over(ink: Rgba, backdrop: Rgba): Rgba {
    const blend = (i: number, b: number) => i * ink[3] + b * (1 - ink[3]);
    return [blend(ink[0], backdrop[0]), blend(ink[1], backdrop[1]), blend(ink[2], backdrop[2]), 1];
}

function relativeLuminance([r, g, b]: Rgba): number {
    const channel = (value: number) => {
        const v = value / 255;
        return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    };
    return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}

/** The WCAG contrast ratio of two opaque colors, from 1 (none) to 21 (black on white). */
export function contrastRatio(a: Rgba, b: Rgba): number {
    const [high, low] = [relativeLuminance(a), relativeLuminance(b)].sort((x, y) => y - x);
    return (high + 0.05) / (low + 0.05);
}

/**
 * The WCAG contrast ratio of an element's text over the background it is drawn on.
 *
 * The background is the nearest one, on the element or an ancestor, that is not transparent;
 * white when there is none. The text's color is dimmed by the element's own opacity first, so a
 * faded label is measured as it is seen.
 */
export async function textContrastOf(locator: Locator): Promise<number> {
    const rendered = await locator.evaluate((element) => {
        const own = getComputedStyle(element);
        let node: Element | null = element;
        let background = "rgb(255, 255, 255)";
        while (node !== null) {
            const color = getComputedStyle(node).backgroundColor;
            // Transparent computes as `rgba(…, 0)` or `color(srgb … / 0)`.
            if (color !== "transparent" && !/[,/]\s*0\)$/.test(color)) {
                background = color;
                break;
            }
            node = node.parentElement;
        }

        return { color: own.color, background, opacity: Number(own.opacity) };
    });

    const ink = parseColor(rendered.color);
    const backdrop = parseColor(rendered.background);
    if (!ink || !backdrop) {
        throw new Error(`Unreadable color: ${rendered.color} over ${rendered.background}`);
    }

    return contrastRatio(
        over([ink[0], ink[1], ink[2], ink[3] * rendered.opacity], backdrop),
        backdrop,
    );
}
