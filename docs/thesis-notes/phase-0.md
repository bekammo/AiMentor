# Phase 0 notes (inception and foundation)

Short notes for the thesis, appended as each task finishes. Chapter tags in brackets.

## P0-T1 Requirements [Intro, Ch2]
- 45 functional requirements in 8 groups and 15 non-functional ones, MoSCoW priorities, each mapped to a story (`docs/requirements.md`).
- Explicit out-of-scope list (OCR, Office rendering, LMS integration, grading, ANN index) gives the boundary for chapter 2.
- Georgian/English glossary of 30 terms for consistent terminology in the Georgian text.

## P0-T2 Backlog [Ch2]
- 10 epics, 32 stories, 63 tasks, 244 h + 6 h contingency (`docs/backlog.md`). Every story maps to at least one task; every Must story is checked by phase 5, so the cut list never removes a Must story.
- Board creation script (`scripts/create_github_board.py`) ready; board not created yet.

## P0-T3 Literature [Ch1]
- 20 annotated sources in five groups (`ch1-sources.md`). Key argument chain: Bloom 1984 → VanLehn 2011 (interactive tutoring works) → Bastani et al. 2025 (unrestricted GenAI harms learning) → Kestin et al. 2025 (pedagogically designed AI tutor helps) → this thesis: grounded, policy-restricted mentor.
- OWASP Top 10 for LLM Applications has a 2026 edition (August 2026); use its numbering in chapter 2 and `docs/security.md`.
- No published evaluation of RAG on Georgian course material was found: supports the novelty claim.

## P0-T4 Diagrams [Ch2]
- Use Case (PlantUML), ER (Mermaid, all 17 tables + Identity), component architecture (Mermaid, C4 container level). Drafts; finalized in P8-T2.

## P0-T5 Skeleton [Ch3]
- .NET 10 solution, 4 layers + 3 test projects + experiment runner; central package management with exact pins; warnings as errors with analyzers and `.editorconfig`; Dockerfile + compose with SQL Server 2025; CI stub (build, unit, integration tests).
- Finding: xunit.v3 4.x needs Microsoft Testing Platform under the .NET 10 SDK; VSTest packages and `coverlet.collector` are replaced by `coverlet.MTP`.

## P0-T6 Spike [Ch2, Ch3]
- Numbers for chapter 3: exact cosine top-5 over 5,000 vectors of 1024 dimensions in SQL Server 2025 Express: 11.6 ms average (laptop, LocalDB). Supports the decision not to use an approximate vector index.
- Georgian costs about 2.0x the tokens of English per word with the o200k tokenizer (2.50 vs 1.23 tokens per word): a 300-token chunk is about 120 Georgian words.
- Agent Framework 1.28 API differs from older samples (AgentSession, AgentResponse, AsAIAgent): example of fast-moving AI tooling and why versions are pinned.
- `docker compose up` runs app + SQL Server 2025 healthy; the same vector query on the container: 21.2 ms per top-5 over 5,000 vectors.
- Lesson for chapter 3: pin container base images to exact versions; a floating `10.0` tag silently used a stale SDK and broke the build.
- GitHub Models, the planned development provider, turned out to be retired (2026-07-30); its endpoint answers every request, even with a fake token, with a plain `200 OK`. Decision D11: Azure OpenAI as the single hosted provider (`gpt-5.4-mini`, `text-embedding-3-small`/`-large`). Example for chapter 3 of why the provider is configuration, and why a startup check must parse a real model response.
- Still open: the Azure call (S3) and PdfPig on real lectures (see `docs/spikes.md`).
