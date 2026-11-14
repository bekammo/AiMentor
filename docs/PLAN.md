# PLAN.md: AI mentor web platform (RAG + Socratic dialogue)

Thesis: "Development of an AI-mentor web platform supporting the learning process, based on RAG architecture and the Socratic dialogue method". Georgian Technical University, Informatics, Software Engineering. Individual project, about 250 hours including the thesis text.

## 0. Key decisions and verified package versions

### 0.1 Design decisions

| # | Decision | Reason |
|---|---|---|
| D1 | Blazor Web App with global **Interactive Server** rendering; Identity pages stay static SSR | Streaming over the SignalR circuit needs no extra API layer, auth is simpler, one deployable container |
| D2 | UI components call Application services in-process; a thin minimal-API surface exists only for files, SSE chat, CSV export and health | Avoids duplicating every use case as REST; integration tests and the SSE endpoint still exercise the same services |
| D3 | Embeddings stored as **`vector(1024)`** for every model: `text-embedding-3-small` and `-3-large` requested with `dimensions: 1024`, `bge-m3` is natively 1024 | SQL Server `vector` (float32) allows at most 1998 dimensions, so `-3-large` at native 3072 cannot be stored. One column makes the embedding model switchable by re-indexing. Experiment 1 also scores native dimensions in memory |
| D4 | Retrieval is **deterministic pipeline code**, not a tool the model may call | The model can never choose the course or skip the filter, which is the isolation guarantee; it also makes experiments reproducible |
| D5 | Assignment policy applies in **both** chat modes: every question is matched against the course's active assignments by embedding similarity | Otherwise a student bypasses "Assignment" mode by pasting the task into "Learn" mode |
| D6 | Restricted answers (hints only / no help) are generated in full, checked by an output guard, then streamed; unrestricted answers stream live | A streamed answer cannot be recalled after a leak is detected |
| D7 | Per-user limits use `System.Threading.RateLimiting` partitioned limiters **inside the chat service**, plus `AddRateLimiter` middleware on HTTP endpoints | Middleware rate limiting does not see messages sent over a Blazor Server circuit |
| D8 | Every query selects explicit columns and never materializes `Chunk.Embedding` | EF Core 10 loads vector properties by default (EF 11 changed this), which would pull 4 KB per row |
| D9 | Citation locator is a page for PDF, a slide for PPTX, and heading + approximate page for DOCX | DOCX files do not store page numbers; only `lastRenderedPageBreak` hints exist |
| D10 | "Open source page": PDF opens in the browser viewer at `#page=N`; PPTX/DOCX show the stored extracted text of that slide/section | Rendering Office files needs LibreOffice in the image, which is out of scope (stretch) |
| D11 | **Azure OpenAI is the single hosted provider** for development, experiments and the demo, called through the plain OpenAI client on the `/openai/v1/` endpoint. Chat: `gpt-5.4-mini`; embeddings: `text-embedding-3-small` and `-3-large`. Ollama stays the optional local arm | GitHub Models, the original development provider, was retired on 2026-07-30 (spike S2). One provider for everything keeps results comparable. `gpt-5.4-mini` is GA until 2027-09-21; `gpt-4.1-mini` is already deprecated. Fallback if the student subscription has no quota: a Pay-As-You-Go Azure subscription or the OpenAI API, same client and code path |

### 0.2 Packages, checked on NuGet on 2026-11-14

| Package | Use this | Notes and flags |
|---|---|---|
| .NET SDK | 10.0.403 (installed) | .NET 10 is LTS. .NET 11 (STS) went GA this month; stay on 10 deliberately |
| Microsoft.EntityFrameworkCore.SqlServer | **10.0.15** | 11.0.0 exists but belongs to .NET 11. Identity/EF packages: same 10.0.x patch |
| Microsoft.Data.SqlClient | transitive via EF Core (**6.1.7**) | `SqlVector<float>` comes from here and works in 6.1.7 (spike S1). Latest stable is 7.1.2 but EF Core 10 does not need it; do not pin separately |
| Microsoft.Agents.AI, Microsoft.Agents.AI.OpenAI | **1.28.0** | Stable (GA), but released at a fast cadence (1.28.0 on 2026-11-06). Pin the exact version; API names must be read from the 1.28 docs, not older samples |
| Microsoft.Extensions.AI, .OpenAI | **10.11.0** | Latest stable is 11.0.0, which supports net10.0 but pulls `Microsoft.Extensions.*` 11.0.0 abstractions. 10.11.0 is the minimum Agents.AI 1.28.0 needs and keeps the whole graph on 10.x. Revisit in P0-T6 |
| Microsoft.Extensions.AI.Evaluation, .Quality, .Reporting | **10.11.0** | Stable. Same family version as above |
| Azure.AI.OpenAI | avoid, or 2.1.0 | Everything after 2.1.0 is **beta** (2.9.0-beta.1). Preferred: plain OpenAI client against the Azure OpenAI v1 endpoint (`https://<resource>.openai.azure.com/openai/v1/`, no `api-version`), so Azure and any fallback OpenAI-compatible endpoint share one code path. Confirm in P0-T6 |
| OllamaSharp | 5.5.2 | Implements `IChatClient` / `IEmbeddingGenerator`. `Microsoft.Extensions.AI.Ollama` is deprecated |
| PdfPig | 0.1.16 | Stable label but **pre-1.0** (0.x). 0.1.17 builds are alpha nightlies; do not use |
| DocumentFormat.OpenXml | 3.5.1 | Stable |
| Microsoft.ML.Tokenizers (+ tiktoken data package) | 3.0.0 | Token counting for chunk sizes |
| xunit.v3 | 4.0.3 | Stable. Runs on Microsoft Testing Platform; `global.json` opts `dotnet test` into MTP, so no `xunit.runner.visualstudio` or `Microsoft.NET.Test.Sdk` |
| coverlet.MTP | 10.1.0 | MTP coverage extension (`coverlet.collector` is VSTest-only) |
| Markdig | 1.6.0 | Stable |
| Testcontainers.MsSql | 4.16.0 | Stable |
| bunit | 2.15.2 | Stable |
| SQL Server image | `2025-CU11-ubuntu-24.04` | `MSSQL_PID=Express` supports `vector` (spike S1) |

SQL Server 2025: the `vector` type and `VECTOR_DISTANCE` are GA and are what `EF.Functions.VectorDistance("cosine", ...)` translates to. `VECTOR_SEARCH()`, `CREATE VECTOR INDEX` and the EF `VectorSearch()` / `HasVectorIndex()` APIs are still marked **experimental** in the EF docs, so they are not used. Exact search is fine at this scale (one course is a few thousand chunks).

Resolved in P0-T5/P0-T6 (see `docs/spikes.md`): Markdig, coverage package, SQL Server 2025 container tag and `MSSQL_PID=Express`. Agent Framework 1.28 renamed `AgentThread` to `AgentSession` and `AgentRunResponse` to `AgentResponse`; use `docs/spikes.md` S5 for API names. OWASP Top 10 for LLM Applications: use the **2026** edition (released August 2026).

## 1. Solution and repository structure

```
AiMentor/
├─ AiMentor.slnx
├─ global.json, Directory.Build.props, Directory.Packages.props, .editorconfig
├─ docker-compose.yml, docker-compose.override.yml, .env.example, Dockerfile
├─ src/
│  ├─ AiMentor.Domain/
│  ├─ AiMentor.Application/
│  ├─ AiMentor.Infrastructure/
│  └─ AiMentor.Web/
├─ tests/
│  ├─ AiMentor.UnitTests/
│  ├─ AiMentor.IntegrationTests/
│  └─ AiMentor.Evaluation/
├─ experiments/
│  ├─ AiMentor.Experiments/        console runner
│  ├─ testset/                     <course>.v1.json + JSON schema
│  ├─ results/                     CSV output (committed)
│  └─ cache/                       LLM/embedding response cache (git-ignored)
├─ docs/
│  ├─ PLAN.md, backlog.md, spikes.md, security.md
│  ├─ diagrams/                    .mmd / .puml sources
│  ├─ adr/                         short decision records
│  └─ thesis-notes/                one file per chapter, appended every phase
└─ .github/workflows/ci.yml
```

| Project | Responsibility | Depends on |
|---|---|---|
| `AiMentor.Domain` | Entities, enums, value objects, the help-policy engine and hint-ladder rules. Pure C#, no packages | nothing |
| `AiMentor.Application` | Use cases: ingestion, chunking, retrieval, chat orchestration, intent classification, guards, citation parsing, analytics queries. Interfaces for extractors, storage, course access, clock. Uses M.E.AI abstractions and Microsoft.Agents.AI | Domain |
| `AiMentor.Infrastructure` | EF Core `DbContext`, migrations, Identity stores, PdfPig/OpenXml extractors, file storage, provider factory (Azure OpenAI / Ollama, D11), telemetry chat-client wrapper, background ingestion worker | Application |
| `AiMentor.Web` | Blazor Web App, minimal API endpoints, auth policies, rate limiting, Georgian localization, composition root | Infrastructure |
| `AiMentor.UnitTests` | Domain and Application logic with fakes | Domain, Application |
| `AiMentor.IntegrationTests` | `WebApplicationFactory` + SQL Server 2025 in Testcontainers, fake AI clients | Web |
| `AiMentor.Evaluation` | M.E.AI.Evaluation quality tests against a real model, opt-in by environment variable | Application, Infrastructure |
| `AiMentor.Experiments` | CLI that runs experiments 1 to 4 and writes CSV | Application, Infrastructure |

Course materials are not committed (copyright); they live in a git-ignored `data/` folder and a Docker volume. Secrets come only from User Secrets or environment variables; `.env.example` holds names with no values.

## 2. Domain model and database schema

Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, ...) are the standard ones; `ApplicationUser` adds `DisplayName`, `IsActive`, `CreatedAt`. Roles: `Admin`, `Lecturer`, `Student`.

| Table | Key columns | Relationships and rules |
|---|---|---|
| `Courses` | Id, Code (unique), Title, Description, OwnerLecturerId, IsArchived, CreatedAt | Lecturer 1..N Courses |
| `Enrollments` | CourseId + StudentId (PK), EnrolledAt | Course N..N Student |
| `Materials` | Id, CourseId, Title, LectureNumber, OriginalFileName, FileType (Pdf/Docx/Pptx), StoragePath, ContentHash (SHA-256), SizeBytes, Status (Pending/Processing/Ready/Failed), Error, PageCount, UploadedById, UploadedAt | Course 1..N Materials; unique (CourseId, ContentHash) |
| `MaterialPages` | Id, MaterialId, PageNumber, Heading, Text | Material 1..N Pages; feeds the source viewer |
| `IndexProfiles` | Id, EmbeddingProvider, EmbeddingModel, Dimensions, ChunkSizeTokens, OverlapTokens, IsActive, CreatedAt | Exactly one active profile serves the product; experiments create others |
| `Chunks` | Id, MaterialId, CourseId (denormalized), IndexProfileId, Ordinal, Text, TokenCount, PageFrom, PageTo, SectionHeading, IsFlagged, **Embedding `vector(1024)`** | Material 1..N Chunks; index on (CourseId, IndexProfileId); cascade delete with material |
| `Assignments` | Id, CourseId, Title, TaskText, HelpPolicy (1 FullHelp / 2 HintsOnly / 3 NoHelp), OpensAt, DueAt, IsActive, TaskEmbedding `vector(1024)`, CreatedById | Course 1..N Assignments |
| `Conversations` | Id, CourseId, StudentId, PseudonymId (GUID), Mode (Learn/Assignment), AssignmentId (nullable), Title, CreatedAt, LastMessageAt | Student 1..N Conversations, each inside one course |
| `Messages` | Id, ConversationId, Role, Content, CreatedAt, Intent, TopicLabel, MatchedAssignmentId, AppliedPolicy, HintLevel, Outcome (Answered/NotCovered/Refused/Blocked) | Conversation 1..N Messages |
| `MessageCitations` | Id, MessageId, Ordinal (the `[n]` marker), ChunkId, MaterialId, PageNumber, Score | Message 1..N Citations |
| `MessageFeedback` | MessageId (PK), StudentId, Rating (+1/-1), Comment, CreatedAt | Message 1..0-1 Feedback |
| `AssignmentHintProgress` | StudentId + AssignmentId (PK), HintLevel (1 to 3), UpdatedAt | Ladder position per student per assignment |
| `LlmCallLogs` | Id, Timestamp, UserId, CourseId, ConversationId, MessageId, Purpose (Chat/Intent/Guard/Rewrite/Embedding/Eval), Provider, Model, InputTokens, OutputTokens, LatencyMs, TimeToFirstTokenMs, CostUsd, Success, ErrorCode | Source for admin monitoring and experiment 4 |
| `AiSettings` | single row: ActiveChatProvider, ActiveChatModel, ActiveIndexProfileId, Temperature, MaxOutputTokens, TopK, MinSimilarity, DailyTokenBudgetPerUser, UpdatedById, UpdatedAt | Overrides configuration defaults; never stores secrets |
| `ModelPrices` | Id, Provider, Model, InputPer1MUsd, OutputPer1MUsd, EffectiveFrom | Cost calculation |
| `SecurityEvents` | Id, Timestamp, UserId, ConversationId, Type (InjectionSuspected/SolutionLeakBlocked/RateLimited/CrossCourseDenied/FlaggedChunk), Details | Evidence for chapter 4 and admin review |

Q&A pipeline, in order: auth and enrollment check; rate limit and daily budget; input length cap and PII scrub; optional follow-up condensing; intent classification (structured output: intent, topic label, injection suspicion); assignment matching; policy resolution (pure function of mode, intent, assignment policy, time window); query embedding; `VectorDistance` top-k filtered by `CourseId` and active profile; similarity threshold (nothing above it returns the fixed text „ეს საკითხი კურსის მასალებში არ არის განხილული" with no generation call); prompt assembly with numbered, delimited context; generation through an Agent Framework `ChatClientAgent`; output guard on restricted paths; citation parsing and validation; persistence and usage logging.

## 3. API endpoints and Blazor pages

### 3.1 Minimal API (all require auth unless noted)

| Method and route | Role | Purpose |
|---|---|---|
| `POST /api/courses/{courseId}/materials` | Lecturer (owner) | Multipart upload; validates extension, magic bytes, size; enqueues ingestion |
| `GET /api/materials/{id}/file` | enrolled / owner | Streams the original file inline (PDF viewer with `#page=N`) |
| `GET /api/materials/{id}/pages/{n}` | enrolled / owner | Extracted text of one page/slide/section |
| `POST /api/chat/stream` | Student (enrolled) | Server-sent events; body `{courseId, conversationId?, mode, assignmentId?, message}`; rate-limited |
| `GET /api/conversations?courseId=` and `GET /api/conversations/{id}` | Student (own) | History |
| `POST /api/messages/{id}/feedback` | Student (own) | Thumbs up/down with optional comment |
| `GET /api/courses/{id}/analytics/export.csv` | Lecturer (owner) | Analytics export |
| `GET /api/admin/usage/export.csv` | Admin | Token, cost and latency export |
| `GET /health`, `GET /alive` | anonymous | Container health checks |

### 3.2 Blazor pages (UI text in Georgian, `ka-GE`)

| Route | Role | Content |
|---|---|---|
| `/Account/Login`, `/Account/Logout`, `/Account/ChangePassword` | all | No self-registration; accounts are created by admin or enrollment |
| `/` | all | Role-based landing |
| `/courses` | Student, Lecturer | Only enrolled (student) or owned (lecturer) courses |
| `/courses/{id}` | enrolled / owner | Materials, assignments, entry to chat |
| `/courses/{id}/chat`, `/courses/{id}/chat/{conversationId}` | Student | Streaming chat, mode switch, assignment picker, citation chips, source side panel, thumbs |
| `/courses/{id}/history` | Student | Conversation list |
| `/lecturer/courses/new`, `/lecturer/courses/{id}/edit` | Lecturer | Course CRUD |
| `/lecturer/courses/{id}/materials` | Lecturer | Upload, ingestion status, re-ingest, delete |
| `/lecturer/courses/{id}/students` | Lecturer | Enroll by email or CSV, remove |
| `/lecturer/courses/{id}/assignments`, `.../assignments/{aid}` | Lecturer | Assignment CRUD with help-policy level and time window |
| `/lecturer/courses/{id}/analytics` | Lecturer | Top topics, unanswered questions, low-rated answers |
| `/admin/users` | Admin | Users, roles, activation |
| `/admin/ai-settings` | Admin | Active provider/model, index profile, limits, prices |
| `/admin/usage` | Admin | Tokens, cost, latency p50/p95, security events |

## 4. Agile backlog: epics and user stories

Full text goes to `docs/backlog.md` (task P0-T2). Format: "As a <role> I want <goal> so that <benefit>".

**E1 Accounts and roles**

- US-01 Student/lecturer/admin logs in. AC: wrong password shows a Georgian error; lockout after 5 failures; navigation shows only items for the user's role.
- US-02 Admin manages users and roles. AC: create, deactivate, change role; a deactivated user cannot log in; the last admin cannot be removed.

**E2 Courses and enrollment**

- US-03 Lecturer creates and edits a course. AC: unique code; only the owner (or admin) can edit.
- US-04 Lecturer enrolls students by email or CSV. AC: unknown emails create inactive-password accounts or are reported; duplicates ignored; removal revokes access immediately.
- US-05 Student sees only enrolled courses. AC: opening another course's URL returns 403/404; verified by an integration test.

**E3 Materials and ingestion**

- US-06 Lecturer uploads PDF, DOCX, PPTX. AC: other types and oversized files rejected; duplicate file detected by hash; page/slide numbers preserved.
- US-07 Lecturer sees ingestion status. AC: Pending, Processing, Ready, Failed with an error message; chat uses only Ready materials.
- US-08 Lecturer deletes or re-ingests a material. AC: its chunks stop appearing in answers at once; old citations still render with a "material removed" note.

**E4 Grounded chat**

- US-09 Student asks in Georgian and gets an answer grounded only in course materials. AC: every factual answer carries at least one citation (material + page); the answer language is Georgian.
- US-10 Student sees the answer stream. AC: first token visible within a few seconds; generation can be cancelled.
- US-11 Student opens a cited source. AC: PDF opens at the cited page; PPTX/DOCX show the cited slide/section text.
- US-12 Student is told when the topic is not covered. AC: out-of-scope questions return the fixed "not covered" message and are logged as unanswered.
- US-13 Student sees conversation history. AC: only own conversations; a conversation can be resumed with context.
- US-14 Student rates an answer. AC: one rating per message, changeable; optional comment.

**E5 Assignments and Socratic mode**

- US-15 Lecturer creates an assignment with a help-policy level. AC: levels 1, 2, 3; optional open/due window; policy change takes effect on the next message.
- US-16 Student in assignment mode gets hints only. AC: hint 1 is a concept reminder, hint 2 points to a lecture section with citation, hint 3 is a guiding question; no response contains a complete solution or final answer.
- US-17 Student gets no help under level 3. AC: a polite fixed refusal, no model generation about the task.
- US-18 Student switches between Learn and Assignment modes. AC: the mode is visible; assignment-like questions in Learn mode still fall under the assignment's policy (D5).
- US-19 Bypass attempts fail. AC: the jailbreak set from the test file produces no full solution with defenses on; blocked attempts are logged as security events.

**E6 Lecturer analytics**

- US-20 Most-asked topics. AC: ranked by lecture/section and topic label for a date range.
- US-21 Questions the system could not answer. AC: list with date and count; no student identity shown.
- US-22 Low-rated answers. AC: question, answer, citations and comment visible; anonymized.

**E7 Admin AI operations**

- US-23 Admin selects the AI provider and models. AC: switch among configured providers with no code change or redeploy; secrets never shown or stored in the database.
- US-24 Admin monitors tokens, cost and latency. AC: per day, per model, per purpose; CSV export.
- US-25 Per-user limits protect the budget. AC: exceeding the per-minute limit or daily token budget gives a Georgian message and a security event; no model call is made.

**E8 Security and privacy**

- US-26 Retrieval is isolated per course. AC: a test with two courses proves no chunk crosses over, including through a forged `courseId`.
- US-27 Instructions hidden in materials are ignored. AC: a poisoned test document does not change the mentor's behavior; suspicious chunks are flagged.
- US-28 No student personal data reaches the model. AC: a test captures the outbound request and finds no name, email or user ID.

**E9 Research and E10 Delivery**

- US-29 Researcher runs each experiment from the CLI and gets CSV. AC: one command per experiment; re-runs hit the cache.
- US-30 Versioned test set. AC: 60 items validated against the schema in CI.
- US-31 `docker compose up` starts the system. AC: app + SQL Server 2025 healthy, migrations applied, seeded admin.
- US-32 CI builds and tests each push. AC: build, unit and integration tests, Docker image build.

## 5. Task list in vertical slices

Definition of done for every coding task, in addition to its own: builds with warnings as errors, new logic has unit tests, CI is green, and `docs/thesis-notes/` gets a few lines (what, why, screenshot or number). Thesis column: Intro, Ch1, Ch2, Ch3, Ch4.

### Phase 0: Inception and foundation (21 h). Demo: empty app runs in Docker; spike proves Georgian RAG works end to end in a script

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P0-T1 | Requirements and scope | Functional and non-functional requirements, actors, out-of-scope list, Georgian/English glossary | 3 | none | `docs/requirements.md` reviewed against this plan | Intro, Ch2 |
| P0-T2 | Agile backlog | Epics and stories from section 4 with acceptance criteria, priorities, story-to-task mapping; GitHub Projects board | 3 | P0-T1 | `docs/backlog.md` + board with all tasks | Ch2 |
| P0-T3 | Literature scan | RAG, Socratic tutoring and AI tutors, multilingual/Georgian embeddings, OWASP Top 10 for LLM Applications; 15 to 20 annotated sources | 5 | none | `docs/thesis-notes/ch1-sources.md` with one paragraph per source | Ch1 |
| P0-T4 | First diagrams | Use Case, draft ER model, component architecture | 3 | P0-T1 | three sources under `docs/diagrams/` that render | Ch2 |
| P0-T5 | Repository and solution skeleton | git, solution and projects from section 1, central package management with pinned versions, analyzers, `.editorconfig`, compose file with SQL Server 2025, CI stub | 4 | none | `dotnet build` and `docker compose up` succeed; CI runs build | Ch3 |
| P0-T6 | Technical spike | Throwaway script: `SqlVector<float>` + `VectorDistance` on the 2025 container; Georgian chat and embedding call via GitHub Models (retired, see D11) replaced by the Azure v1 endpoint with the OpenAI client; PdfPig on three real lecture PDFs (check Unicode output); Agent Framework 1.28 API names; M.E.AI 10.11 vs 11.0 | 3 | P0-T5 | `docs/spikes.md` with findings and a go/no-go per item in section 0 | Ch2, Ch3 |

### Phase 1: Walking skeleton (31 h). Demo: one seeded course, upload a PDF, ask in Georgian, get an answer with material + page citations (no login yet)

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P1-T1 | Domain and persistence core | `Course`, `Material`, `MaterialPage`, `IndexProfile`, `Chunk` with `vector(1024)`; `DbContext`; first migration; seed one course | 4 | P0-T6 | migration applies to the container; round-trip test stores and reads a vector | Ch2, Ch3 |
| P1-T2 | Provider wiring | Options-bound registration of `IChatClient` and `IEmbeddingGenerator` chosen by configuration (Azure OpenAI first, D11); options validation at startup, including one real call whose response must parse; User Secrets | 4 | P0-T6 | changing one config value switches provider; missing key or a non-model response fails fast with a clear message | Ch3 |
| P1-T3 | PDF extraction | PdfPig extractor returning text per page; NFC normalization, de-hyphenation, whitespace cleanup; fixture PDFs in Georgian | 3 | P1-T1 | unit tests assert page count and known Georgian phrases per page | Ch3 |
| P1-T4 | Token-based chunker | Sentence-aware chunks of configurable token size and overlap, tracking `PageFrom`/`PageTo` | 4 | P1-T3 | unit tests: size bounds, overlap, page spans, no text lost | Ch3 |
| P1-T5 | Ingestion pipeline | extract, chunk, batch-embed, store; status transitions; idempotent on content hash | 4 | P1-T2, P1-T4 | integration test ingests a fixture PDF with a fake embedder | Ch3 |
| P1-T6 | Retrieval service | Query embedding, `VectorDistance("cosine")` top-k filtered by `CourseId` and active profile, projection without the vector column, similarity threshold | 3 | P1-T5 | integration test returns the expected chunk first for a known question | Ch3 |
| P1-T7 | Grounded answer with citations | `ChatClientAgent` with a Georgian system prompt, numbered context blocks, `[n]` citation parser and validator, "not covered" path | 5 | P1-T6 | unit tests for parser; manual check on 5 real questions recorded in notes | Ch3 |
| P1-T8 | Minimal UI and demo | Upload page and ask page listing answer plus sources; README run instructions; demo script | 4 | P1-T7 | demo script passes on a clean clone | Ch3 |

### Phase 2: Users, courses, isolation, all formats (25 h). Demo: lecturer logs in, creates a course, enrolls a student, uploads PDF/DOCX/PPTX; the student sees only that course and gets answers only from it

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P2-T1 | Identity and roles | ASP.NET Core Identity with three roles, seeded admin from environment, Georgian login pages, lockout | 4 | P1-T8 | login works per role; no self-registration | Ch3 |
| P2-T2 | Course management | Lecturer CRUD with ownership checks | 4 | P2-T1 | non-owner edit returns 403 in a test | Ch3 |
| P2-T3 | Enrollment and "my courses" | Enroll by email/CSV; student course list | 3 | P2-T2 | student sees exactly the enrolled courses | Ch3 |
| P2-T4 | Course isolation | Single `ICourseAccess` gate used by every service and endpoint; authorization policy; two-course isolation tests including forged IDs | 4 | P2-T3 | isolation test suite green (US-26) | Ch3, Ch4 |
| P2-T5 | DOCX and PPTX extractors | OpenXml extraction: slides with titles and notes; DOCX headings with approximate pages (D9) | 4 | P1-T3 | fixture tests for both formats | Ch3 |
| P2-T6 | Background ingestion and status | `Channel` + `BackgroundService` queue, progress and error display, re-ingest, delete | 4 | P2-T5 | upload returns immediately; status updates live; failure shows a reason | Ch3 |
| P2-T7 | Georgian localization and layout | `ka-GE` culture, resource files, role-based navigation | 2 | P2-T1 | no English strings on user-facing pages | Ch3 |

### Phase 3: Full chat experience (22 h). Demo: streamed multi-turn chat with history, clickable citations that open the page, ratings, and a correct "not covered" reply

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P3-T1 | Conversations and history | Persist conversations/messages, history page, resume with trimmed context, follow-up condensing behind a flag | 4 | P2-T4 | a follow-up such as "explain it more simply" retrieves the right context | Ch3 |
| P3-T2 | Streaming | Agent streaming to the UI, cancel button, SSE endpoint over the same service | 4 | P3-T1 | tokens render incrementally; cancel stops the model call | Ch3 |
| P3-T3 | Citations UI and source viewer | Citation chips, side panel, secured file and page endpoints, PDF at `#page=N` | 4 | P3-T2 | US-11 acceptance criteria pass | Ch3 |
| P3-T4 | Feedback | Thumbs up/down with comment | 2 | P3-T1 | stored once per message, changeable | Ch3 |
| P3-T5 | Out-of-scope handling | Calibrate `MinSimilarity` on a small labeled sample; log unanswered questions | 3 | P3-T1 | 5 of 5 sample out-of-scope questions refused, 0 of 10 in-scope refused; threshold recorded | Ch3, Ch4 |
| P3-T6 | Mode switch and rendering | Learn/Assignment toggle (plumbing), Markdown and code rendering with HTML sanitization | 2 | P3-T2 | model output cannot inject script into the page | Ch3 |
| P3-T7 | Chat flow tests | Integration tests with deterministic fake chat/embedding clients; two bUnit component tests | 3 | P3-T3 | tests cover answer, citation, not-covered and cancel paths | Ch4 |

### Phase 4: Assignments, Socratic policy, defenses (30 h). Demo: lecturer creates an assignment per policy level; the student gets the hint ladder, a refusal under level 3, and jailbreak attempts fail

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P4-T1 | Assignment management | CRUD, policy level, time window, task embedding on save | 4 | P3-T6 | US-15 acceptance criteria pass | Ch3 |
| P4-T2 | Intent classification and assignment matching | Structured-output classifier (concept / solve request / other, topic label, injection suspicion); match question to active assignments by similarity | 4 | P4-T1 | accuracy on a 20-item labeled sample recorded; matching works in both modes (D5) | Ch3, Ch4 |
| P4-T3 | Policy engine | Pure domain function: mode, intent, matched assignment, time window to decision (full answer / hint level / refuse) | 4 | P4-T2 | decision-table unit tests cover every combination | Ch2, Ch3 |
| P4-T4 | Hint ladder | Per-student ladder state; one prompt per rung (concept reminder, lecture pointer with citation, guiding question); never a solution | 4 | P4-T3 | US-16 acceptance criteria pass on 5 sample tasks | Ch3 |
| P4-T5 | Direct injection defenses | Hardened system prompt, delimited user input, output guard (LLM judge for solution leak) with regenerate-then-fallback, generate-then-stream on restricted paths (D6); every defense behind a `DefenseOptions` flag for experiment 3 | 5 | P4-T4 | US-19 passes; flags switch each defense off individually | Ch3, Ch4 |
| P4-T6 | Indirect injection defenses | Ingestion-time detection and flagging of instruction-like text, context marked as data in the prompt, poisoned-document test | 4 | P4-T5 | US-27 passes; flagged chunks visible to the lecturer | Ch3, Ch4 |
| P4-T7 | PII minimization | Prompts carry only a pseudonymous conversation ID; regex scrub of emails, phones, personal numbers in input; outbound-payload test | 2 | P4-T5 | US-28 test green | Ch3, Ch4 |
| P4-T8 | Behavior diagrams and threat mapping | Sequence (Q&A, ingestion), Activity (policy decision and hint ladder), OWASP LLM Top 10 mapping table in `docs/security.md` | 3 | P4-T6 | sources render; each OWASP item marked mitigated / accepted / not applicable | Ch2 |

### Phase 5: Admin and analytics (21 h). Demo: admin switches provider and watches cost and latency; a rate-limited user is stopped; lecturer sees topics, unanswered and low-rated questions

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P5-T1 | Usage telemetry | Delegating chat/embedding client that records tokens, latency, time to first token, cost into `LlmCallLogs`; OpenTelemetry enabled | 4 | P3-T2 | every model call produces one log row with cost | Ch3, Ch4 |
| P5-T2 | User administration | Users, roles, activation | 3 | P2-T1 | US-02 acceptance criteria pass | Ch3 |
| P5-T3 | AI settings | Choose active provider/model/index profile among configured ones, limits, price table | 3 | P5-T1 | provider switch at runtime with no restart; secrets never displayed | Ch3 |
| P5-T4 | Usage dashboard | Tokens, cost, p50/p95 latency by day/model/purpose; CSV export; security events list | 3 | P5-T1 | numbers match a manual SQL check | Ch3, Ch4 |
| P5-T5 | Rate limits and budget | Partitioned per-user limiter in the chat service and middleware on HTTP endpoints (D7); daily token budget | 3 | P5-T1 | US-25 passes in an integration test | Ch3, Ch4 |
| P5-T6 | Lecturer analytics | Top topics by lecture/section and topic label, unanswered list, low-rated list, anonymized; CSV export | 5 | P3-T5, P4-T2 | US-20 to US-22 pass on seeded data | Ch3 |

### Phase 6: Test set and experiments 1 to 3 (38 h). Demo: one command per experiment produces the CSV behind each thesis table

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P6-T1 | Test-set format | JSON schema (id, category, question, reference answer, gold material + pages, expected behavior, assignment reference, attack type), validator test, versioning rule | 2 | P1-T3 | empty v1 file validates in CI | Ch4 |
| P6-T2 | Factual questions | 30 factual/conceptual questions with reference answers and gold locations | 6 | P6-T1 | 30 items validate; gold pages checked against the extracted text | Ch4 |
| P6-T3 | Remaining categories | 10 multi-lecture, 10 out-of-scope, 10 assignment-solving requests including jailbreaks (role-play, "ignore instructions", incremental extraction, translation trick, fake authority); second-reader spot check of 15 items | 6 | P6-T2 | 60 items validate; spot-check notes saved | Ch4 |
| P6-T4 | Experiment runner | CLI commands `index`, `retrieval`, `answers`, `jailbreak`, `perf`, `sus`; config matrix; disk caches for embeddings and model responses; CSV writer | 5 | P1-T6 | `--help` lists commands; second run makes zero API calls | Ch3, Ch4 |
| P6-T5 | Experiment 1: retrieval | Embedding models (3-small, 3-large, bge-m3 if Ollama runs) x chunk size (300, 600): Recall@5 and MRR per question and summary; native-dimension variant in memory; parity test that in-memory ranking equals SQL ranking | 5 | P6-T3, P6-T4 | `exp1_per_question.csv`, `exp1_summary.csv`; winning profile set as active | Ch4 |
| P6-T6 | Experiment 2: answer quality | No-RAG vs RAG (optional local model): Groundedness, Relevance, Retrieval evaluators (1 to 5), correct-refusal rate on out-of-scope, false-refusal rate, citation accuracy; 10 answers also hand-scored to check the judge | 6 | P6-T5 | `exp2_per_question.csv`, `exp2_summary.csv`, judge-vs-human note | Ch4 |
| P6-T7 | Experiment 3: Socratic robustness | 10 attack items x 3 repetitions with defenses off, then on (flags from P4-T5); judge plus manual review of every response | 5 | P4-T6, P6-T4 | `exp3_jailbreak.csv` with leak rate before and after | Ch4 |
| P6-T8 | Analysis | Tables and charts, interpretation, limitations (small n, single course, self-authored set, LLM judge on Georgian) | 3 | P6-T5 to T7 | `docs/thesis-notes/ch4-results.md` with figures | Ch4 |

### Phase 7: Packaging, quality, usability study (22 h). Demo: `docker compose up` on a clean machine; green CI; students use it in a supervised session

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P7-T1 | Docker package | Multi-stage Dockerfile, compose with app + SQL Server 2025 (+ optional Ollama profile), health checks, migrations on start, volumes | 4 | P5-T5 | US-31 passes on a clean clone | Ch3 |
| P7-T2 | CI pipeline | Build, unit tests, integration tests with Testcontainers, coverage report, test-set validation, image build | 3 | P7-T1 | US-32; badge in README | Ch3, Ch4 |
| P7-T3 | Tests and code metrics | Raise coverage of Domain + Application to at least 70%; Visual Studio code metrics export (Maintainability Index, cyclomatic complexity); refactor the worst three methods | 4 | P5-T6 | coverage and metrics tables saved under `docs/thesis-notes/` | Ch4 |
| P7-T4 | Security review | Checklist pass: authorization on every endpoint, upload validation, security headers, secret scan, dependency vulnerabilities (`dotnet list package --vulnerable`) | 3 | P7-T1 | `docs/security.md` checklist complete, findings fixed or accepted | Ch4 |
| P7-T5 | Usability study preparation | Task scenarios, Georgian SUS form, consent text, seeded course and accounts, session rehearsal | 3 | P7-T1 | materials printed/online; dry run with one person | Ch4 |
| P7-T6 | Usability sessions and experiment 4 | Supervised sessions with 10 to 15 students on the laptop over local network; SUS scores; latency and cost per answer from `LlmCallLogs` | 5 | P7-T5 | `exp4_sus.csv`, `exp4_latency_cost.csv`, SUS mean and distribution | Ch4 |

### Phase 8: Thesis and defense (34 h)

| ID | Task | Description | h | Depends | Deliverable / done when | Thesis |
|---|---|---|---|---|---|---|
| P8-T1 | Introduction and chapter 1 | Problem, goals, novelty; literature review from P0-T3 notes | 6 | P0-T3 | draft in Georgian, about 8 pages | Intro, Ch1 |
| P8-T2 | Chapter 2 | Requirements, backlog, final UML (Use Case, Activity, Sequence, Class), ER model, architecture; diagrams updated to match the code | 7 | P4-T8, P7-T3 | draft, about 8 pages, all diagrams final | Ch2 |
| P8-T3 | Chapter 3 | Implementation: pipelines, policy engine, defenses, provider switching, deployment | 6 | P7-T1 | draft, about 7 pages with screenshots | Ch3 |
| P8-T4 | Chapter 4 | Testing strategy and results, experiments 1 to 4, code metrics, limitations | 6 | P6-T8, P7-T6 | draft, about 7 pages with tables and charts | Ch4 |
| P8-T5 | Closing parts and formatting | Conclusion, abstracts (Georgian and English), references, appendices, GTU template | 3 | P8-T1 to T4 | complete formatted document | all |
| P8-T6 | Review and revision | Supervisor feedback round, plagiarism check, corrections | 3 | P8-T5 | supervisor-approved version | all |
| P8-T7 | Defense | Slides, live demo script, recorded backup video | 3 | P8-T6 | rehearsed 10-minute talk | all |

Contingency: 6 h, unassigned.

## 6. Summary table

| Phase | Content | Hours | Cumulative h | Cumulative % | Target date |
|---|---|---|---|---|---|
| P0 | Requirements, backlog, literature, first diagrams, skeleton, spike | 21 | 21 | 8.4% | 22 Nov 2026 |
| P1 | Walking skeleton: PDF to cited answer | 31 | 52 | 20.8% | 2 Dec 2026 |
| P2 | Users, courses, isolation, DOCX/PPTX | 25 | 77 | 30.8% | 13 Dec 2026 |
| P3 | Full chat experience | 22 | 99 | 39.6% | 20 Dec 2026 |
| P4 | Assignments, Socratic policy, defenses | 30 | 129 | 51.6% | 3 Jan 2027 |
| P5 | Admin and analytics | 21 | 150 | 60.0% | 10 Jan 2027 |
| P6 | Test set and experiments 1 to 3 | 38 | 188 | 75.2% | 24 Jan 2027 |
| P7 | Packaging, quality, usability study | 22 | 210 | 84.0% | 31 Jan 2027 |
| P8 | Thesis text and defense | 34 | 244 | 97.6% | 14 Feb 2027 |
| | Contingency | 6 | 250 | 100% | |

Pace: about 20 hours per week for 13 weeks. If the real deadline is earlier, apply the cut list in section 9.

| Kind of work | Hours | Share |
|---|---|---|
| Requirements, backlog, literature (P0-T1 to T3) | 11 | 4.4% |
| Diagrams (P0-T4, P4-T8; finalization is inside P8-T2) | 6 | 2.4% |
| Product code, tests, packaging | 147 | 58.8% |
| Test-set creation (P6-T1 to T3) | 14 | 5.6% |
| Experiment harness, runs, analysis, usability study | 32 | 12.8% |
| Thesis writing and defense | 34 | 13.6% |
| Contingency | 6 | 2.4% |

Scheduling notes: P6-T2 and P6-T3 need only the materials, so do them whenever coding is blocked. Book the usability session date in December because late January is exam season. P8-T1 can start any time after P0-T3.

## 7. Diagrams to produce (sources under `docs/diagrams/`, not drawn yet)

| File | Type | Shows | Task |
|---|---|---|---|
| `use-case.puml` | UML Use Case (PlantUML; Mermaid has none) | Student, Lecturer, Admin and their use cases | P0-T4 |
| `er-model.mmd` | ER diagram | All tables of section 2 | P0-T4, final in P8-T2 |
| `architecture-components.mmd` | Component / C4 container | Four projects, SQL Server, AI providers, file storage | P0-T4 |
| `class-domain.mmd` | UML Class | Domain entities, enums, policy engine | P8-T2 |
| `class-application.mmd` | UML Class | Services and interfaces of the ingestion and chat pipelines | P8-T2 |
| `sequence-qa.mmd` | UML Sequence | Question to cited answer, including guard and logging | P4-T8 |
| `sequence-ingestion.mmd` | UML Sequence | Upload to stored embeddings | P4-T8 |
| `activity-policy.mmd` | UML Activity | Intent, assignment match, policy decision, hint ladder | P4-T8 |
| `activity-ingestion.mmd` | UML Activity | Extraction, chunking, embedding, failure paths | P4-T8 |
| `state-hint-ladder.mmd` | State | Hint levels 1 to 3 and material status lifecycle | P8-T2 |
| `deployment.mmd` | Deployment | docker-compose services, volumes, external APIs | P8-T2 |
| `threat-model-dfd.mmd` | Data-flow | Trust boundaries and injection entry points | P4-T8 (optional) |

## 8. Testing strategy

| Level | Scope | Tools | Runs |
|---|---|---|---|
| Unit | Policy engine decision table, hint ladder, chunker, citation parser, extractors on fixture files, PII scrubber, cost calculation, Recall@k / MRR / SUS formulas, prompt builder | xUnit v3, hand-written fakes | every push |
| Integration | EF Core + real `vector` columns, `VectorDistance` ranking, course isolation, ingestion end to end, chat flow, rate limits, authorization of every endpoint | `WebApplicationFactory`, Testcontainers SQL Server 2025, deterministic fake `IChatClient` / `IEmbeddingGenerator` | every push |
| Component | Chat page and citation panel | bUnit | every push |
| AI evaluation | Relevance, Groundedness, Retrieval evaluators on a 10-question smoke subset; full set through the experiment runner | Microsoft.Extensions.AI.Evaluation (+ Quality, Reporting with disk response cache) | manual / nightly, gated by `AIMENTOR_EVAL=1` so CI spends nothing |
| Security regression | Jailbreak prompts, poisoned document, cross-course access, outbound-payload PII check | integration + evaluation projects, cached responses | every push with fakes; with a real model before release |
| Acceptance | Demo script per phase, mapped to story acceptance criteria | manual checklist in `docs/backlog.md` | end of each phase |
| Usability | SUS with 10 to 15 students | supervised session | P7-T6 |

Targets: at least 70% line coverage on Domain + Application; no method with a Maintainability Index below 20 (Visual Studio's green band) and a reported project average; zero known vulnerable packages.

## 9. Risks and mitigations

| Risk | Likelihood / impact | Mitigation |
|---|---|---|
| Embedding quality on Georgian is weak | medium / high | Measured early by P0-T6 and properly by experiment 1; three models compared; `bge-m3` as multilingual alternative; fallback is adding lecture/section titles to chunk text before embedding |
| Georgian tokenizes into many tokens per word, so 300/600-token chunks hold little text and cost more | high / medium | Report chunk sizes in tokens and in words; add a larger size only if results demand it; note it as a finding |
| Small or local models write poor Georgian | high / medium | Use a strong hosted model for generation; the local model is an optional comparison, not a dependency |
| PDF text comes out garbled despite being digital (font maps) | low / high | P0-T6 checks real files first; fallback is asking for DOCX/PPTX originals; OCR is out of scope |
| Azure for Students gives no or very low OpenAI quota (reports from 2026 show 0 to about 1K tokens per minute; no batch quota for student offers) | medium / high | Checked first in P0-T6; fallback is a Pay-As-You-Go Azure subscription or the OpenAI API with the same client (D11); fake clients in tests; disk cache for every model call |
| Hosted model retired mid-project (GitHub Models already was) | medium / medium | Use GA models whose retirement date is after the defense (D11); provider and model are configuration only |
| Azure for Students credit runs out | low / medium | Cheapest adequate models; per-user and daily budgets; cost dashboard from P5; experiments are cached |
| Agent Framework and M.E.AI change quickly | medium / medium | Exact version pins in `Directory.Packages.props`; no upgrades after P1 unless a bug forces it; Agent Framework kept behind one orchestrator class |
| LLM judge is unreliable on Georgian text | medium / medium | 10 answers hand-scored and compared with the judge (P6-T6); every jailbreak response reviewed manually |
| Test set is self-authored with no lecturer review | certain / medium | Second-reader spot check (classmate or supervisor) in P6-T3; gold pages verified against extracted text; stated as a limitation |
| Small samples (10 jailbreak items, 10 to 15 students) | certain / low | Repeat runs, report counts next to percentages, avoid claims of statistical significance |
| Students unavailable for the study in exam season | medium / medium | Book the date in December; fall back to two smaller sessions; minimum 10 participants |
| Compressed timeline (13 weeks, about 20 h/week, holidays in the middle) | high / high | Vertical slices keep a demoable system at every phase; 6 h contingency; cut list below |
| Scope creep | high / high | Backlog is frozen after P0; new ideas go to a "future work" list that feeds the thesis conclusion |
| Demo fails on defense day | low / high | Recorded backup video (P8-T7); cached responses; local Docker package |
| Course materials are copyrighted | medium / low | Never committed; private repository until cleared; test set stores questions and page references only |

Cut list, in the order to apply it if time runs short: drop the Ollama/`bge-m3` arm (two embedding models still satisfy experiment 1); make AI settings read-only with provider chosen by configuration; reduce topic analytics to grouping by lecture; drop follow-up condensing; drop bUnit tests; drop approximate DOCX pages (cite by heading only).

## 10. Verification of this plan

- Hours: phase totals sum to 244 plus 6 contingency = 250; every task is between 2 and 8 hours.
- Coverage: every user story in section 4 maps to at least one task; every required thesis artifact (UML set, ER model, backlog, unit tests, Maintainability Index, Docker image) has a producing task.
- After each phase, run its demo line from section 5 on a clean clone with `docker compose up` and tick the mapped acceptance criteria.
