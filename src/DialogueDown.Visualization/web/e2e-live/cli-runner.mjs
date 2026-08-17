import { spawn } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const CLI_PROJECT = resolve(here, "../../../DialogueDown.Cli/DialogueDown.Cli.csproj");

/**
 * The framework the CLI is actually built for, read from its project rather than repeated here.
 * A copy would go stale the next time the CLI's target framework moves, and the failure would
 * arrive as a missing file rather than as the version mismatch it really is.
 */
function cliTargetFramework() {
    const csproj = readFileSync(CLI_PROJECT, "utf8");
    const target = /<TargetFramework>([^<]+)<\/TargetFramework>/.exec(csproj);
    if (!target) throw new Error(`No <TargetFramework> in ${CLI_PROJECT}`);
    return target[1].trim();
}

const CLI_DLL = resolve(
    here,
    `../../../DialogueDown.Cli/bin/Release/${cliTargetFramework()}/DialogueDown.Cli.dll`,
);

export function cliInvocation(args) {
    return {
        command: "dotnet",
        args: [CLI_DLL, ...args],
    };
}

export function spawnCli(args) {
    if (!existsSync(CLI_DLL)) {
        throw new Error(
            `The live E2E CLI is not built: ${CLI_DLL}\nRun "npm run build:cli" first.`,
        );
    }

    const invocation = cliInvocation(args);
    const server = spawn(invocation.command, invocation.args, { stdio: "inherit" });
    const stop = () => server.kill("SIGTERM");
    process.on("SIGTERM", stop);
    process.on("SIGINT", stop);
    server.on("exit", (code) => process.exit(code ?? 0));
    return server;
}
