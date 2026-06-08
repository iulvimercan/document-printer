# Document Print Automation

A .NET application that collapses a tedious, multi-step document-preparation chore into a
single plain-language chat request. A local LLM agent decides *which* document is wanted;
deterministic C# does everything else — browsing a configured source website, downloading the
PDF, stripping a text-overlay watermark, and printing it double-sided through a guided
manual-flip workflow.

The agent's job is deliberately narrow (understand the request and pick the right link). All
fetching, PDF editing, and printing is plain, testable C# — no model is in the loop once the
target document is chosen.

## How it works

1. **Chat** — the user describes what they want in natural language.
2. **Download** — Playwright drives a headless browser to the configured source site; the agent
   picks the correct link; the file is saved to a working folder.
3. **Clean** — a deterministic remover empties the watermark overlay's content stream via
   PDFsharp, leaving the underlying page content untouched.
4. **Analyze** — the PDF's page structure is inspected so the user can optionally choose which
   sections to print.
5. **Print** — double-sided output with a *guided flip*: one set of pages is printed, the app
   pauses and prompts the user to flip the stack, then prints the rest.

## Architecture

A layered solution with a strict inward-only dependency rule — nothing depends back toward the
web layer, and the core has no dependencies of its own:

```
Web → Agent → { Downloader, Watermark, Printing } → Core
```

| Project | Type | Responsibility |
| --- | --- | --- |
| `*.Core` | class lib | Models + interfaces only; no dependencies. All cross-cutting contracts live here. |
| `*.Downloader` | class lib | Browser automation (Playwright): navigate, locate, download. |
| `*.Watermark` | class lib | Deterministic text-overlay watermark removal (PDFsharp). |
| `*.Printing` | class lib | Guided-flip duplex printing and page-range selection. |
| `*.Agent` | class lib | Semantic Kernel + Ollama; registers the libraries as tool-calling plugins and orchestrates the pipeline. |
| `*.Agent.Console` | console | Dev REPL for driving the agent end-to-end before the web UI. |
| `*.Web` | Blazor Server | Chat UI, live pipeline status, and the flip-confirmation panel. |

Implementations are written against the `Core` interfaces and wired up via dependency
injection. The LLM only chooses *what* to fetch and *which* link; deterministic C# handles the
fetching, PDF editing, and printing.

## Technology

- **.NET 9** (`net9.0`, set centrally in `Directory.Build.props`)
- **Blazor Server** — chat, status, and flip-confirmation UI
- **Semantic Kernel + Ollama** — a single local, tool-calling model used purely as an
  orchestrator (configurable model id; defaults overridable via environment variables)
- **Playwright** — browser automation, used hybrid: deterministic navigate/click/download with
  the model only choosing the right link
- **PDFsharp** — fully deterministic PDF inspection and watermark removal (keys off PDF
  structure, so no vision model is required)
- **SumatraPDF** — silent, scriptable printing (see below)
- **xUnit** — unit tests, with live/integration tests gated behind an environment flag

## Build & run

```powershell
dotnet build                                         # build the solution
dotnet test                                          # run tests (live tests gated by an env flag)
dotnet run --project src/DocPrinter.Agent.Console    # chat with the agent (needs a local Ollama)
dotnet run --project src/DocPrinter.Web              # launch the Blazor web app
```

The Playwright Chromium browser **auto-installs on the first download** (so a fresh clone works
out of the box; the first run may pause while it downloads). To pre-install it manually, run
`playwright.ps1 install chromium` from a project whose output contains `Microsoft.Playwright.dll`.

The local model runtime (Ollama) must be running with the configured model pulled before the
agent can chat.

## Configuration (user secrets)

The source location is **not committed**. The downloader reads it from configuration under the
`Downloader` key, supplied via [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
so it never lands in the repository. Set it once (the Web and console projects share one secret
store):

```powershell
dotnet user-secrets set "Downloader:EntryUrl"     "<your source entry page URL>" --project src/DocPrinter.Web
dotnet user-secrets set "Downloader:DocumentHost" "<your document host, e.g. files.example.org>" --project src/DocPrinter.Web
```

`EntryUrl` is the page the browser starts from (it must expose the year selector). `DocumentHost`
is an optional host substring used to recognize download links; if omitted, links are matched by
their `.pdf` suffix. Without `EntryUrl` set, the downloader has nothing to open and fails fast.

## SumatraPDF (printing dependency)

Printing shells out to **SumatraPDF**, which is not committed (it's GPLv3, so it isn't bundled in
this repo). Download the portable build from <https://www.sumatrapdfreader.org/> and place the
executable at `tools/SumatraPDF/SumatraPDF.exe`. The Printing project copies it next to the app
output on build; if it's missing, printing throws `SumatraPdfNotFoundException` at runtime. You
can also point at an existing install via the `Printing:SumatraPath` setting.
