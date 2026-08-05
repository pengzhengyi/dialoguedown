import "@picocss/pico/css/pico.min.css";
import "tippy.js/dist/tippy.css";
import "@vscode/codicons/dist/codicon.css";
import "../src/styles.css";

import { createSourceView } from "../src/source-view";
import {
    createFakeDebugController,
    type FakeDebugProgram,
} from "../src/test-support/fake-debug-controller";

const source = `Entry
Branch
Left
Right
End
`;

const program: FakeDebugProgram = {
    id: "browser-harness",
    entryId: "entry",
    locations: [
        {
            id: "entry",
            anchor: "Entry",
            label: "Entry",
            paths: [{ id: "branch", label: "Branch", targetId: "branch" }],
        },
        {
            id: "branch",
            anchor: "Branch",
            label: "Branch",
            paths: [
                { id: "left", label: "Left", targetId: "left" },
                { id: "right", label: "Right", targetId: "right" },
            ],
        },
        { id: "left", anchor: "Left", label: "Left", paths: [] },
        { id: "right", anchor: "Right", label: "Right", paths: [] },
    ],
};

document.documentElement.dataset.servedMode = "edit";
const debug = createFakeDebugController(source, program);
const sourceView = createSourceView(source, { editable: true, debug });
document.body.appendChild(sourceView.element);
