# Requirements and scope (P0-T1)

Project: AI-mentor web platform supporting the learning process, based on RAG architecture and the Socratic dialogue method. This document fixes what the system must do before coding starts. Section numbers in brackets point to `docs/PLAN.md`. The backlog in `docs/backlog.md` refines these requirements into user stories with acceptance criteria.

Status: draft v1, 2026-11-14. Frozen at the end of phase 0; later changes go to the "future work" list (section 7).

## 1. Purpose and context

Students in a university course ask questions outside lecture hours. General chatbots answer from general knowledge rather than the course's own lectures, give no sources, and hand over complete solutions to graded assignments. The platform answers only from the lecturer's uploaded materials, cites the material and page, and switches to Socratic guidance (hints and questions, never solutions) when a question concerns an active assignment.

Scope of the thesis prototype: one university (GTU), Georgian-language courses and UI, a handful of courses, tens of students per course, run on one machine with Docker.

## 2. Actors

| Actor | Description | Main goals |
|---|---|---|
| Student | Enrolled in one or more courses | Ask questions about course content in Georgian, get cited answers, get hints on assignments, review past conversations |
| Lecturer | Owns courses | Upload materials, enroll students, define assignments and their help policy, see what students ask and where answers fail |
| Administrator | Operates the platform | Manage accounts and roles, choose AI provider and models, watch tokens, cost, latency and security events, set limits |
| AI provider (external system) | Azure OpenAI (hosted), or an optional local Ollama server | Generate chat completions and embeddings |
| Researcher (the author, in the experiment role) | Runs experiments from the CLI | Reproducible measurements for chapter 4 |

## 3. Functional requirements

Priority uses MoSCoW: **M** must, **S** should, **C** could. Story column maps to `docs/backlog.md`.

### 3.1 Accounts and access

| ID | Requirement | Priority | Story |
|---|---|---|---|
| FR-01 | Users log in with email and password. There is no self-registration; accounts are created by an administrator or by enrollment | M | US-01 |
| FR-02 | Three roles: Student, Lecturer, Admin. Navigation and pages are limited to the user's role | M | US-01 |
| FR-03 | An account is locked for a period after 5 failed login attempts | M | US-01 |
| FR-04 | An administrator creates, deactivates and changes the role of users. A deactivated user cannot log in. The last administrator cannot be removed or demoted | M | US-02 |
| FR-05 | Users can change their own password | S | US-01 |

### 3.2 Courses and enrollment

| ID | Requirement | Priority | Story |
|---|---|---|---|
| FR-06 | A lecturer creates, edits and archives courses. A course code is unique. Only the owner or an administrator can edit | M | US-03 |
| FR-07 | A lecturer enrolls students by email, one at a time or from a CSV file; duplicates are ignored, unknown emails are reported or create inactive accounts | M | US-04 |
| FR-08 | Removing an enrollment revokes access immediately | M | US-04 |
| FR-09 | A student sees and can open only courses they are enrolled in; any other course returns 403 or 404 | M | US-05 |

### 3.3 Materials and ingestion

| ID | Requirement | Priority | Story |
|---|---|---|---|
| FR-10 | A lecturer uploads PDF, DOCX and PPTX files. Other types, files whose content does not match the extension, and files over the size limit are rejected | M | US-06 |
| FR-11 | A duplicate upload in the same course is detected by SHA-256 hash | M | US-06 |
| FR-12 | Ingestion extracts text with page numbers (PDF), slide numbers (PPTX) or headings with approximate pages (DOCX), splits it into token-sized chunks with overlap, embeds and stores them | M | US-06 |
| FR-13 | Ingestion runs in the background; the lecturer sees Pending, Processing, Ready or Failed with a reason | M | US-07 |
| FR-14 | Only Ready materials are used in answers | M | US-07 |
| FR-15 | A lecturer deletes or re-ingests a material; its chunks stop being used at once; older citations still render with a "material removed" note | M | US-08 |
| FR-16 | Chunks that contain instruction-like text (possible indirect prompt injection) are flagged and shown to the lecturer | S | US-27 |

### 3.4 Grounded chat

| ID | Requirement | Priority | Story |
|---|---|---|---|
| FR-17 | A student asks a question in Georgian inside a course and gets a Georgian answer based only on that course's Ready materials | M | US-09 |
| FR-18 | Every factual answer carries at least one citation (material and page/slide/section) using `[n]` markers that map to stored sources | M | US-09 |
| FR-19 | Answers stream token by token; the student can cancel generation | M | US-10 |
| FR-20 | Clicking a citation opens the source: PDF in the browser at the cited page; PPTX/DOCX as the stored extracted text of the slide or section | M | US-11 |
| FR-21 | When no chunk passes the similarity threshold, the system replies with the fixed message „ეს საკითხი კურსის მასალებში არ არის განხილული" without calling the generation model, and logs the question as unanswered | M | US-12 |
| FR-22 | Conversations are stored; a student sees only their own history and can resume a conversation with its context | M | US-13 |
| FR-23 | A follow-up question that depends on earlier turns is rewritten into a standalone query before retrieval | C | US-13 |
| FR-24 | A student rates an answer thumbs up or down with an optional comment; one rating per message, changeable | S | US-14 |
| FR-25 | Model output is rendered as sanitized Markdown with code blocks; it cannot inject script into the page | M | US-09 |

### 3.5 Assignments and Socratic mode

| ID | Requirement | Priority | Story |
|---|---|---|---|
| FR-26 | A lecturer creates assignments with task text, optional open/due window and a help policy: 1 full help, 2 hints only, 3 no help | M | US-15 |
| FR-27 | A policy change takes effect on the next message | M | US-15 |
| FR-28 | The chat has two visible modes, Learn and Assignment; in Assignment mode the student picks an assignment | M | US-18 |
| FR-29 | Every question, in either mode, is classified (concept question, solution request, other) and matched against the course's active assignments by embedding similarity; a match applies that assignment's policy | M | US-18 |
| FR-30 | Under policy 2 the student gets a hint ladder per assignment: hint 1 concept reminder, hint 2 pointer to a lecture section with citation, hint 3 guiding question. No response contains a complete solution or final answer | M | US-16 |
| FR-31 | Under policy 3 the student gets a polite fixed refusal and no model generation about the task | M | US-17 |
| FR-32 | Restricted answers are generated in full, checked by an output guard for solution leaks, and only then shown; a leak triggers one regeneration, then a fixed fallback | M | US-19 |
| FR-33 | Blocked attempts and suspected injections are recorded as security events | M | US-19 |

### 3.6 Lecturer analytics

| ID | Requirement | Priority | Story |
|---|---|---|---|
| FR-34 | Most-asked topics for a date range, grouped by lecture/section and by topic label | S | US-20 |
| FR-35 | List of questions the system could not answer, with date and count, without student identity | M | US-21 |
| FR-36 | List of low-rated answers with question, answer, citations and comment, anonymized | S | US-22 |
| FR-37 | Analytics export as CSV | C | US-20 |

### 3.7 Administration of AI operations

| ID | Requirement | Priority | Story |
|---|---|---|---|
| FR-38 | The administrator selects the active chat provider/model and the active index profile among configured ones, at runtime, without code change or restart | S | US-23 |
| FR-39 | API keys come only from configuration (User Secrets or environment); they are never shown in the UI or stored in the database | M | US-23 |
| FR-40 | Every model call is logged with purpose, provider, model, input/output tokens, latency, time to first token, cost and success | M | US-24 |
| FR-41 | A usage dashboard shows tokens, cost and p50/p95 latency by day, model and purpose, plus security events; CSV export | S | US-24 |
| FR-42 | Per-user per-minute message limit and daily token budget; exceeding either gives a Georgian message, records a security event, and makes no model call | M | US-25 |

### 3.8 Research support

| ID | Requirement | Priority | Story |
|---|---|---|---|
| FR-43 | A CLI runs each experiment (retrieval, answer quality, jailbreak robustness, performance, SUS) and writes CSV | M | US-29 |
| FR-44 | Model and embedding responses are cached on disk so a re-run makes no API calls | M | US-29 |
| FR-45 | A versioned test set of 60 items is validated against a JSON schema in CI | M | US-30 |

## 4. Non-functional requirements

| ID | Category | Requirement | How it is verified |
|---|---|---|---|
| NFR-01 | Security: isolation | Retrieval and every endpoint are scoped to courses the user may access through one access gate; a forged `courseId` never returns another course's data | Two-course isolation integration tests (US-26) |
| NFR-02 | Security: prompt injection | Defenses against direct and indirect injection (OWASP LLM01): hardened prompt, delimited input, context marked as data, output guard, ingestion flagging; each defense switchable for experiment 3 | Jailbreak set, poisoned-document test, experiment 3 |
| NFR-03 | Privacy | No student name, email or account ID is sent to an AI provider; conversations use a pseudonymous ID; emails, phone numbers and personal numbers in input are scrubbed | Outbound-payload test (US-28) |
| NFR-04 | Security: web | Authorization on every endpoint, upload validation by magic bytes, antiforgery, security headers, no secrets in the repository, no known vulnerable packages | P7-T4 checklist |
| NFR-05 | Performance | First streamed token visible in at most 5 s at p95 for unrestricted answers on a hosted model; restricted answers complete in at most 20 s at p95 | `LlmCallLogs`, experiment 4 |
| NFR-06 | Capacity | 15 concurrent students in one session on one laptop; a course of a few thousand chunks searched exactly (no ANN index) | Usability session |
| NFR-07 | Cost | Cost per answer recorded and reported; daily token budget per user enforced | Experiment 4, US-25 |
| NFR-08 | Usability | Georgian UI throughout (`ka-GE`); SUS mean of at least 68 (industry average) is the target | Experiment 4 |
| NFR-09 | Language quality | Answers in Georgian; retrieval works on Georgian queries over Georgian materials | Experiments 1 and 2 |
| NFR-10 | Maintainability | Layered solution (Domain, Application, Infrastructure, Web); warnings as errors; at least 70% line coverage on Domain + Application; no method with Maintainability Index below 20 | CI, P7-T3 metrics |
| NFR-11 | Portability | `docker compose up` starts the app and SQL Server 2025 on a clean machine with migrations applied and an admin seeded | US-31 |
| NFR-12 | Provider independence | Chat and embedding providers are swappable through configuration (M.E.AI abstractions); switching the embedding model requires only re-indexing | P1-T2, P5-T3 |
| NFR-13 | Reproducibility | Retrieval is deterministic code, experiments are cached and versioned, results CSV are committed | Experiment runner |
| NFR-14 | Observability | Health endpoints, structured logs, OpenTelemetry traces and metrics for model calls | P5-T1 |
| NFR-15 | Data protection | Course materials are stored only in a git-ignored folder or Docker volume; the repository stays private until copyright is cleared | Repository review |

## 5. Constraints

- Platform: .NET 10 (LTS), Blazor Web App with Interactive Server rendering, EF Core 10, SQL Server 2025 `vector` type (decisions D1 to D3).
- AI: Microsoft.Extensions.AI abstractions and Microsoft Agent Framework 1.28; provider Azure OpenAI for development, experiments and the demo (`gpt-5.4-mini`, `text-embedding-3-small`/`-large`), Ollama optional local (PLAN D11). GitHub Models, planned at first, was retired by GitHub on 2026-07-30.
- Budget: free tiers and student credit only.
- Time: about 250 hours over 13 weeks, single developer.
- Materials are digital (text-based) PDF/DOCX/PPTX. Scanned documents are not supported.

## 6. Out of scope

- OCR of scanned PDFs or images inside documents.
- Rendering PPTX/DOCX pages visually (needs LibreOffice in the image; stretch goal only).
- Self-registration, password reset by email, external identity providers (Google, Microsoft), two-factor authentication.
- Integration with an LMS (Moodle, GTU systems) or SSO.
- Grading or automatic assessment of submitted assignments; plagiarism detection.
- Mobile apps; offline mode.
- Approximate nearest-neighbour vector indexes (`CREATE VECTOR INDEX`, still experimental in EF Core).
- Multi-tenant deployment, horizontal scaling, high availability.
- Voice input/output, image understanding.
- Languages other than Georgian for the UI (materials may contain English terms and code).
- Fine-tuning any model.

## 7. Future work list (filled during the project)

Ideas that come up after the backlog is frozen go here; they feed the thesis conclusion.

- (empty)

## 8. Glossary (Georgian / English)

| ქართული | English | Meaning in this project |
|---|---|---|
| AI მენტორი | AI mentor | The assistant that answers students from course materials |
| სოკრატული დიალოგი | Socratic dialogue | Guiding with questions and hints instead of giving answers |
| მინიშნება | hint | One step of help that does not reveal the solution |
| მინიშნებების კიბე | hint ladder | Ordered hints 1 to 3 per student per assignment |
| დახმარების პოლიტიკა | help policy | Assignment setting: full help, hints only, no help |
| დავალება | assignment | A graded task defined by the lecturer |
| სასწავლო კურსი | course | Container for materials, students and assignments |
| ჩარიცხვა | enrollment | Link that gives a student access to a course |
| სასწავლო მასალა | course material | An uploaded PDF, DOCX or PPTX file |
| ინდექსაცია | ingestion / indexing | Extracting, chunking and embedding a material |
| ფრაგმენტი | chunk | A token-bounded piece of material text stored with its embedding |
| ვექტორული წარმოდგენა (ემბედინგი) | embedding | Numeric vector representing the meaning of a text |
| ვექტორული ძიება | vector search | Finding chunks whose embeddings are closest to the question's |
| კოსინუსური მანძილი | cosine distance | Similarity measure used by `VECTOR_DISTANCE` |
| მსგავსების ზღვარი | similarity threshold | Minimum similarity for a chunk to count as relevant |
| ძიებით გაძლიერებული გენერაცია | retrieval-augmented generation (RAG) | Answering with a language model given retrieved course text |
| დიდი ენობრივი მოდელი | large language model (LLM) | The generation model |
| წყაროს მითითება (ციტირება) | citation | `[n]` marker linking an answer to material and page |
| ინსტრუქციის ჩანერგვა (prompt injection) | prompt injection | Input that tries to override the mentor's instructions |
| ირიბი ჩანერგვა | indirect injection | Instructions hidden inside uploaded materials |
| შემოწმების ფილტრი (გამომავალი დაცვა) | output guard | Check that blocks solution leaks before display |
| ჯეილბრეიკი (შეზღუდვის გვერდის ავლა) | jailbreak | Attempt to make the mentor reveal a solution |
| განზრახვის კლასიფიკაცია | intent classification | Labeling a question as concept, solution request or other |
| ფსევდონიმი | pseudonym | Random ID used instead of student identity in prompts |
| ტოკენი | token | Unit of model input/output used for limits and cost |
| ლიმიტი / ბიუჯეტი | rate limit / budget | Per-user caps on messages and tokens |
| გამოყენებადობის შკალა (SUS) | System Usability Scale | 10-item questionnaire used in experiment 4 |
| გახსენება@k, MRR | Recall@k, MRR | Retrieval quality metrics in experiment 1 |
| დასაბუთებულობა | groundedness | How well an answer is supported by the retrieved text |
| შესაბამისობა | relevance | How well an answer addresses the question |

## 9. Review against PLAN.md

- Every user story US-01 to US-32 maps to at least one FR or NFR above (US-26 to US-28 via NFR-01 to NFR-03; US-31 and US-32 via NFR-11 and NFR-10).
- Decisions D1 to D10 are reflected: D5 in FR-29, D6 in FR-32, D7 in FR-42, D9 in FR-12, D10 in FR-20.
- Out-of-scope items match the plan's stretch notes (LibreOffice rendering, experimental vector index) and the cut list.
