import assert from "node:assert/strict";
import { createServer } from "node:http";
import test from "node:test";
import { dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { guardPortsIn } from "./guard-ports.mjs";

const fixtures = dirname(fileURLToPath(import.meta.url));

/** A stand-in server answering with `body`, on a port of its own choosing. */
async function serving(body, run) {
    const server = createServer((_request, response) => {
        response.writeHead(200, { "content-type": "text/html" });
        response.end(body);
    });
    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    try {
        return await run(server.address().port);
    } finally {
        await new Promise((resolve) => server.close(resolve));
    }
}

// The guard exists because `reuseExistingServer` adopts whatever holds a fixture port, so a
// server left behind by another worktree runs the suite against a different build in silence.
// Naming the port and the checkout is the whole point: the message has to send the reader to the
// other server rather than to the tests that failed because of it.
test("rejects a server that is not serving this checkout's fixtures", async () => {
    await serving("<!doctype html><body>a different build</body>", async (port) => {
        await assert.rejects(guardPortsIn([port], fixtures), (error) => {
            assert.match(error.message, new RegExp(String(port)), "names the port");
            assert.match(error.message, /not serving this checkout's fixtures/);
            assert.ok(error.message.includes(fixtures), "names the checkout it expected");
            return true;
        });
    });
});

test("accepts a server showing a document from this checkout", async () => {
    const page = `<!doctype html><body>Live visualization of ${fixtures}/.live-doc.dialogue.md`;
    await serving(page, (port) => assert.doesNotReject(guardPortsIn([port], fixtures)));
});

// Nothing listening is the ordinary case: Playwright is about to start the servers itself.
test("says nothing about a port no one is serving", async () => {
    const free = await serving("", (port) => port);

    await assert.doesNotReject(guardPortsIn([free], fixtures));
});
