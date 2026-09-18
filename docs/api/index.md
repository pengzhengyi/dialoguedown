# API reference

The generated reference for the **DialogueDown** core library — the
engine-agnostic C# library you integrate into a game. Browse the namespaces and
types in the sidebar.

Highlights:

- <xref:DialogueDown.Compilation.IScriptCompiler> — the single seam that compiles a
  script through the stages and returns a `CompilationResult`.
- <xref:Microsoft.Extensions.DependencyInjection.DialogueDownServiceCollectionExtensions> —
  `AddDialogueDown()` container registration.

> [!NOTE]
> This reference currently covers the core `DialogueDown` library. The
> visualization and CLI projects are documented in the
> [design notes](../contributing/design-notes/README.md).
>
> `IGameSystem` is a placeholder integration seam that ships in core today but
> nothing implements yet; the runtime's world read and effect protocol supersedes
> it (see the
> [runtime architecture](../contributing/design-notes/runtime/Dialogue%20Runtime%20Architecture.md)).
