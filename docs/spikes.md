# Technical spike findings (P0-T6)

Spike code: `spike/AiMentor.Spike` (throwaway, not in the solution). Run `dotnet run -- <command>` from that folder; commands are listed at the top of `Program.cs`. Date of runs: 2026-11-14.

## Summary: go / no-go per item in PLAN.md section 0

| # | Item | Result | Decision |
|---|---|---|---|
| S1 | `SqlVector<float>` + `EF.Functions.VectorDistance` on SQL Server 2025 | **Passed** on SQL Server 2025 RTM-CU3 Express (LocalDB) | **Go** (D3, D8). Re-run on the Docker container once Docker works |
| S2 | Georgian chat + embeddings via GitHub Models | **Not run**: needs a GitHub token with Models access | Pending |
| S3 | Azure OpenAI v1 endpoint with the plain OpenAI client | **Not run**: needs an Azure OpenAI resource and key | Pending |
| S4 | PdfPig on three real lecture PDFs (Unicode output) | **Not run**: needs the lecture PDFs in `spike/data/` | Pending |
| S5 | Agent Framework 1.28 API names | **Passed** (offline, fake chat client) | **Go**, with the renamed APIs below |
| S6 | M.E.AI 10.11 vs 11.0 | **Checked** by restoring both graphs | **Go on 10.11.0**; do not move to 11.0 |
| S7 | Unverified versions from section 0.2 (Markdig, coverlet, SQL Server tag, `MSSQL_PID=Express`) | Versions resolved; Express confirmed to support `vector` | **Go**, with a test-stack change (see S7) |
| S8 | Georgian token cost (risk table) | **Measured** with o200k | Confirms the risk; report chunk sizes in tokens and words |
| S9 | `docker compose up` (P0-T5 done-when) | **Not run**: Docker Desktop fails to start on this machine | Pending; compose file validated with `docker compose config` only |

## S1. SQL Server 2025 vector search with EF Core 10

Setup: EF Core 10.0.15, `SqlVector<float>` property mapped with `HasColumnType("vector(1024)")`, `EnsureCreated`, three hand-made vectors in two courses, then 5,000 random 1024-dimension vectors.

Generated SQL for "top 5 in course 1, nearest first, project without the vector":

```sql
SELECT TOP(@p) [c].[Id], [c].[Text], VECTOR_DISTANCE('cosine', [c].[Embedding], @query) AS [Distance]
FROM [Chunks] AS [c]
WHERE [c].[CourseId] = 1
ORDER BY VECTOR_DISTANCE('cosine', [c].[Embedding], @query)
```

Results:

- Ranking correct: `near` distance 0.0050, `far` 1.0000. The course 2 row with an identical vector to the query was excluded by the `CourseId` filter (isolation works at the query level).
- A projection with `Select(new { ... })` does not read the `Embedding` column, so D8 (explicit projections) is enough to avoid loading 4 KB per row.
- Round-trip: a stored vector reads back with length 1024 and the same values.
- Inserting 5,000 vectors in batches of 1,000 took 0.7 s. An exact top-5 query over about 5,000 rows took **11.6 ms** on average (LocalDB, laptop). Exact search is fine at the planned scale; no vector index needed.
- Edition reported `Express Edition (64-bit)`, so the `vector` type and `VECTOR_DISTANCE` work in Express. `MSSQL_PID=Express` in compose is fine.

Findings for the plan:

- **SqlClient version.** EF Core 10.0.15 resolves `Microsoft.Data.SqlClient` **6.1.7**, not 7.x. `SqlVector<T>` is present in 6.1.x. Keep it transitive, as section 0.2 says, but the note "latest stable 7.1.2" is irrelevant: do not pin 7.x on top of EF 10.
- `ToQueryString()` prints the vector parameter as `sql_variant`; that is only the debug printer. The real command sends a vector parameter.

## S2. GitHub Models (Georgian chat and embeddings)

Not run. The spike command is ready:

```bash
GITHUB_TOKEN=<token with models:read> dotnet run -- github
```

It uses `new OpenAIClient(new ApiKeyCredential(token), new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") })`, embeds a Georgian question and three passages with `text-embedding-3-small` at `Dimensions = 1024`, prints cosine similarity, then streams a grounded Georgian answer through `AsAIAgent` and measures time to first token, Georgian letter share, `[1]` citation presence and the "not covered" path. Model names can be changed with `SPIKE_CHAT_MODEL` and `SPIKE_EMBED_MODEL`.

What to record when it runs: does `dimensions: 1024` work through GitHub Models, is the relevant passage clearly ahead of the others (for example cosine gap above 0.1), is the answer in Georgian with a citation, and the rate-limit headers or errors seen.

## S3. Azure OpenAI v1 endpoint

Not run. Same code path as S2 with a different base URL, which is the point of the test:

```bash
AZURE_OPENAI_ENDPOINT=https://<resource>.openai.azure.com AZURE_OPENAI_API_KEY=<key> AZURE_OPENAI_CHAT=<deployment> AZURE_OPENAI_EMBED=<deployment> dotnet run -- azure
```

If it passes, `Azure.AI.OpenAI` is not needed at all (section 0.2 recommendation confirmed). `OpenAI` 2.14.0 is what M.E.AI.OpenAI 10.11.0 brings in transitively; 2.15.0 exists but is not needed.

## S4. PdfPig on real lecture PDFs

Not run. Copy three real lecture PDFs into `spike/data/` (git-ignored) and run `dotnet run -- pdf`. Per file it prints pages, empty pages, counts of Georgian (U+10D0 to U+10FF), Mtavruli (U+1C90 to U+1CBF), Latin, private-use and replacement characters, the words of page 2, and whether the text is already NFC-normalized.

Pass criteria: Georgian share matches what the page shows, private-use and replacement counts near zero, no empty pages for text slides. Many private-use characters mean the PDF uses a legacy font mapping (pre-Unicode Georgian fonts such as AcadNusx); the fallback in the risk table (ask for DOCX/PPTX originals) then applies to that file.

## S5. Agent Framework 1.28: API names

Read from the 1.28.0 assemblies by reflection and exercised with a fake `IChatClient`. Older samples and blog posts use names that **no longer exist**:

| Older samples | 1.28.0 |
|---|---|
| `AgentThread`, `agent.GetNewThread()` | `AgentSession`, `await agent.CreateSessionAsync()` (`ChatClientAgentSession` for chat-client agents) |
| `AgentRunResponse`, `AgentRunResponseUpdate` | `AgentResponse`, `AgentResponseUpdate` (`.Text`, `.Usage`, `.Messages`) |
| `chatClient.CreateAIAgent(...)` | `chatClient.AsAIAgent(instructions, name, description, tools, ...)` (also on `OpenAI.Chat.ChatClient` in `Microsoft.Agents.AI.OpenAI`) |
| `ChatClientAgentOptions.Instructions` | Not present. Instructions go in the `ChatClientAgent` constructor / `AsAIAgent` argument, or in `ChatClientAgentOptions.ChatOptions.Instructions` |

Behaviour observed:

- `RunAsync(string, session)` and `RunStreamingAsync(string, session)` work; streaming yields `AgentResponseUpdate` with `.Text`.
- Instructions reach the chat client as `ChatOptions.Instructions`, not as a system message in the list. A telemetry or test fake must read `options.Instructions`.
- The session keeps history in memory: the second turn sent 3 messages (user, assistant, user). Since this project stores history in SQL and builds the prompt itself (D4, P3-T1), the orchestrator should pass the trimmed history explicitly and use a fresh session per call, or plug in a custom `ChatHistoryProvider`. Decide in P1-T7; a fresh session per call is simpler.
- Per-call settings go through `new ChatClientAgentRunOptions(new ChatOptions { Temperature = ..., MaxOutputTokens = ... })`; both were honored.
- Structured output exists as `RunAsync<T>(...)` returning `AgentResponse<T>` with `.Result`; useful for the intent classifier (P4-T2).
- `Microsoft.Agents.AI` 1.28.0 depends on `Microsoft.Extensions.AI.Evaluation` 10.11.0 and `Microsoft.ML.Tokenizers` 2.0.0 (our pin of 3.0.0 wins; no conflict at restore).

Decision: keep Agent Framework behind one orchestrator class (risk table), and read API names from this file or the assemblies, not from older samples.

## S6. Microsoft.Extensions.AI 10.11.0 vs 11.0.0

Restoring `Microsoft.Agents.AI.OpenAI` 1.28.0 together with `Microsoft.Extensions.AI.OpenAI` 11.0.0 resolves to a **split family**: `Microsoft.Extensions.AI.Abstractions` and `.OpenAI` at 11.0.0 while `Microsoft.Extensions.AI` stays at 10.11.0 (Agent Framework's minimum). No `Microsoft.Extensions.*` 11 runtime packages were pulled, so the earlier concern was smaller than expected, but mixing majors in one family gains nothing and risks binding mismatches.

Decision: **stay on 10.11.0 for the whole M.E.AI family** (including Evaluation). Revisit only if Agent Framework moves its minimum to 11.

## S7. Versions resolved in this phase

| Package / image | Version | Note |
|---|---|---|
| Markdig | 1.6.0 | Latest stable |
| xunit.v3 | 4.0.3 | Runs on Microsoft Testing Platform (MTP) |
| coverlet | **coverlet.MTP 10.1.0** | `coverlet.collector` is a VSTest data collector; xunit.v3 4.x + .NET 10 `dotnet test` need MTP |
| xunit.runner.visualstudio, Microsoft.NET.Test.Sdk | not used | Replaced by MTP; `global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }` |
| SQL Server image | `mcr.microsoft.com/mssql/server:2025-CU11-ubuntu-24.04` | Latest CU tag on MCR; LocalDB on this machine is RTM-CU3 |
| `MSSQL_PID=Express` | works | `vector` and `VECTOR_DISTANCE` run on Express (S1) |
| actions/checkout, actions/setup-dotnet | v7, v6 | Latest major tags |
| Microsoft.AspNetCore.Mvc.Testing, Identity.EntityFrameworkCore, EF Design | 10.0.15 | Same patch as EF Core |

PLAN.md section 0.2 is updated to match (xunit runner and coverlet lines, SqlClient line).

## S8. Georgian token cost

`o200k_base` (GPT-4o family tokenizer), sample lecture-style sentences:

| Text | Words | Tokens | Tokens per word |
|---|---|---|---|
| Georgian passage | 20 | 50 | 2.50 |
| Same content in English | 22 | 27 | 1.23 |
| Georgian question | 9 | 16 | 1.78 |

A Georgian passage costs about **twice** the tokens of the English equivalent. A 300-token chunk holds about 120 Georgian words and a 600-token chunk about 240. This confirms the risk-table entry; experiment 1 should report both units. Embedding models use their own tokenizers (bge-m3 uses XLM-R SentencePiece), so these numbers apply to generation cost, not to embedding limits.

## S9. Docker

`docker compose config` validates the file (it correctly refuses to run without `MSSQL_SA_PASSWORD`). Docker Desktop 4.59.0 on this machine quits at startup with:

> listening on unix://...\Docker\run\dockerInference: remove ...\Docker\run\dockerInference: The file cannot be accessed by the system.

A stale socket file in `%LOCALAPPDATA%\Docker\run` blocks startup. Usual fixes: quit Docker Desktop, run `wsl --shutdown`, delete that `run` folder's stale entry (or reboot), then start Docker Desktop again. After that, run the P0-T5 check:

```bash
cp .env.example .env    # then set MSSQL_SA_PASSWORD
docker compose up --build
```

Expected: `sqlserver` and `web` both healthy, app at http://localhost:8080, `/health` returns 200. Also confirm that `/opt/mssql-tools18/bin/sqlcmd` exists in the 2025 image (used by the health check).
