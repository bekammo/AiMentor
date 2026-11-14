# Product backlog (P0-T2)

Source: `docs/PLAN.md` section 4 (stories) and section 5 (tasks); requirements in `docs/requirements.md`. The backlog is **frozen** at the end of phase 0. New ideas go to the future-work list in `docs/requirements.md` section 7.

Format: "As a <role> I want <goal> so that <benefit>". Priority is MoSCoW (**M** must, **S** should, **C** could). Phase is when the story's acceptance criteria are first checked. Each acceptance criterion is a checkbox that is ticked at the end of the phase demo.

GitHub Projects board: `scripts/create_github_board.py` creates one issue per story and per task and adds them to a project board with Phase and Priority fields. It prints what it would do by default; with `--apply` it needs `gh auth login` (with the `project` scope) and an existing private repository. It has not been run yet.

## Epics

| Epic | Name | Stories | Goal |
|---|---|---|---|
| E1 | Accounts and roles | US-01, US-02 | Only known users get in, each sees only what the role allows |
| E2 | Courses and enrollment | US-03 to US-05 | Lecturers own courses; students see only enrolled ones |
| E3 | Materials and ingestion | US-06 to US-08 | Lecture files become searchable, cited chunks |
| E4 | Grounded chat | US-09 to US-14 | Cited Georgian answers from course materials only |
| E5 | Assignments and Socratic mode | US-15 to US-19 | Help on assignments without giving away solutions |
| E6 | Lecturer analytics | US-20 to US-22 | Lecturers see what students struggle with |
| E7 | Admin AI operations | US-23 to US-25 | Control of providers, cost and abuse |
| E8 | Security and privacy | US-26 to US-28 | Isolation, injection resistance, data minimization |
| E9 | Research | US-29, US-30 | Reproducible experiments for chapter 4 |
| E10 | Delivery | US-31, US-32 | One-command start, green CI |

## User stories

### E1 Accounts and roles

**US-01 Log in** (M, phase 2). As a student, lecturer or admin I want to log in with my account so that I reach the features of my role.
- [ ] A wrong password shows a Georgian error message.
- [ ] The account is locked after 5 failed attempts.
- [ ] Navigation shows only items for the user's role.
- [ ] There is no self-registration page.

**US-02 Manage users and roles** (M, phase 5). As an admin I want to create users, change roles and deactivate accounts so that only current members use the platform.
- [ ] Create, deactivate and change role work from `/admin/users`.
- [ ] A deactivated user cannot log in.
- [ ] The last admin cannot be removed or demoted.

### E2 Courses and enrollment

**US-03 Create and edit a course** (M, phase 2). As a lecturer I want to create and edit my courses so that I can organize materials per subject.
- [ ] Course code is unique.
- [ ] Only the owner or an admin can edit; others get 403.

**US-04 Enroll students** (M, phase 2). As a lecturer I want to enroll students by email or CSV so that they get access to my course.
- [ ] Unknown emails create inactive-password accounts or are reported.
- [ ] Duplicates are ignored.
- [ ] Removal revokes access immediately.

**US-05 See only enrolled courses** (M, phase 2). As a student I want to see only my courses so that I am not confused by others and cannot reach them.
- [ ] Opening another course's URL returns 403/404.
- [ ] Verified by an integration test.

### E3 Materials and ingestion

**US-06 Upload materials** (M, phase 1 for PDF, phase 2 for DOCX/PPTX). As a lecturer I want to upload PDF, DOCX and PPTX files so that the mentor can answer from my lectures.
- [ ] Other types and oversized files are rejected.
- [ ] A duplicate file is detected by hash.
- [ ] Page and slide numbers are preserved.

**US-07 See ingestion status** (M, phase 2). As a lecturer I want to see the processing status of each material so that I know when students can use it.
- [ ] Pending, Processing, Ready and Failed (with error message) are shown.
- [ ] Chat uses only Ready materials.

**US-08 Delete or re-ingest** (M, phase 2). As a lecturer I want to delete or re-process a material so that outdated content stops being used.
- [ ] Its chunks stop appearing in answers at once.
- [ ] Old citations still render with a "material removed" note.

### E4 Grounded chat

**US-09 Grounded Georgian answer** (M, phase 1). As a student I want to ask in Georgian and get an answer based only on course materials so that I can trust it matches the lectures.
- [ ] Every factual answer carries at least one citation (material + page).
- [ ] The answer language is Georgian.
- [ ] Output is sanitized Markdown; script cannot be injected.

**US-10 Streaming answer** (M, phase 3). As a student I want to see the answer appear as it is written so that I am not left waiting.
- [ ] First token is visible within a few seconds.
- [ ] Generation can be cancelled.

**US-11 Open a cited source** (M, phase 3). As a student I want to open the cited page so that I can read the original.
- [ ] PDF opens at the cited page.
- [ ] PPTX/DOCX show the cited slide or section text.

**US-12 Topic not covered** (M, phase 3). As a student I want to be told when my question is outside the course so that I do not get an invented answer.
- [ ] Out-of-scope questions return the fixed "not covered" message.
- [ ] They are logged as unanswered.

**US-13 Conversation history** (M, phase 3). As a student I want to see and resume my conversations so that I can continue learning where I stopped.
- [ ] Only own conversations are listed.
- [ ] A conversation can be resumed with context.

**US-14 Rate an answer** (S, phase 3). As a student I want to rate answers so that the lecturer learns where the mentor fails.
- [ ] One rating per message, changeable.
- [ ] Optional comment.

### E5 Assignments and Socratic mode

**US-15 Assignment with help policy** (M, phase 4). As a lecturer I want to create an assignment with a help level so that the mentor supports learning without doing the work.
- [ ] Levels 1, 2, 3 are available.
- [ ] Optional open/due window.
- [ ] A policy change takes effect on the next message.

**US-16 Hints only** (M, phase 4). As a student working on an assignment I want hints so that I can solve it myself.
- [ ] Hint 1 is a concept reminder.
- [ ] Hint 2 points to a lecture section with citation.
- [ ] Hint 3 is a guiding question.
- [ ] No response contains a complete solution or final answer.

**US-17 No help** (M, phase 4). As a lecturer I want no help given on some assignments so that they test independent work.
- [ ] A polite fixed refusal is shown.
- [ ] No model generation about the task happens.

**US-18 Learn and Assignment modes** (M, phase 4). As a student I want to switch between Learn and Assignment modes so that I get the right kind of help.
- [ ] The current mode is visible.
- [ ] Assignment-like questions in Learn mode still fall under the assignment's policy (D5).

**US-19 Bypass attempts fail** (M, phase 4). As a lecturer I want jailbreak attempts to fail so that the help policy cannot be tricked.
- [ ] The jailbreak set from the test file produces no full solution with defenses on.
- [ ] Blocked attempts are logged as security events.

### E6 Lecturer analytics

**US-20 Most-asked topics** (S, phase 5). As a lecturer I want to see the most-asked topics so that I can adjust my lectures.
- [ ] Ranked by lecture/section and topic label for a date range.

**US-21 Unanswered questions** (M, phase 5). As a lecturer I want to see questions the system could not answer so that I can add missing material.
- [ ] List with date and count.
- [ ] No student identity shown.

**US-22 Low-rated answers** (S, phase 5). As a lecturer I want to review low-rated answers so that I can find confusing material or mentor errors.
- [ ] Question, answer, citations and comment are visible.
- [ ] Anonymized.

### E7 Admin AI operations

**US-23 Choose provider and models** (S, phase 5). As an admin I want to select the AI provider and models so that I can balance quality and cost.
- [ ] Switch among configured providers with no code change or redeploy.
- [ ] Secrets are never shown or stored in the database.

**US-24 Monitor usage** (S, phase 5). As an admin I want to see tokens, cost and latency so that I can keep the platform within budget.
- [ ] Per day, per model, per purpose.
- [ ] CSV export.

**US-25 Per-user limits** (M, phase 5). As an admin I want per-user limits so that one user cannot exhaust the budget.
- [ ] Exceeding the per-minute limit or daily token budget gives a Georgian message.
- [ ] A security event is recorded.
- [ ] No model call is made.

### E8 Security and privacy

**US-26 Course isolation** (M, phase 2). As a lecturer I want retrieval isolated per course so that my materials never appear in another course.
- [ ] A test with two courses proves no chunk crosses over.
- [ ] Including through a forged `courseId`.

**US-27 Hidden instructions ignored** (M, phase 4). As a lecturer I want instructions hidden in materials to be ignored so that a document cannot hijack the mentor.
- [ ] A poisoned test document does not change the mentor's behavior.
- [ ] Suspicious chunks are flagged.

**US-28 No personal data to the model** (M, phase 4). As a student I want my identity kept from the AI provider so that my privacy is protected.
- [ ] A test captures the outbound request and finds no name, email or user ID.

### E9 Research

**US-29 Run experiments from the CLI** (M, phase 6). As the researcher I want one command per experiment that writes CSV so that thesis tables are reproducible.
- [ ] One command per experiment.
- [ ] Re-runs hit the cache.

**US-30 Versioned test set** (M, phase 6). As the researcher I want a versioned, validated test set so that results are comparable across runs.
- [ ] 60 items.
- [ ] Validated against the schema in CI.

### E10 Delivery

**US-31 One-command start** (M, phase 0 skeleton, phase 7 complete). As a reviewer I want `docker compose up` to start the system so that I can run it without setup.
- [ ] App and SQL Server 2025 are healthy.
- [ ] Migrations are applied.
- [ ] An admin is seeded.

**US-32 CI on every push** (M, phase 0 stub, phase 7 complete). As the developer I want CI to build and test each push so that regressions are caught.
- [ ] Build, unit and integration tests run.
- [ ] Docker image is built.

## Story-to-task mapping

| Story | Priority | Tasks |
|---|---|---|
| US-01 | M | P2-T1, P2-T7 |
| US-02 | M | P5-T2 |
| US-03 | M | P2-T2 |
| US-04 | M | P2-T3 |
| US-05 | M | P2-T3, P2-T4 |
| US-06 | M | P1-T3, P1-T5, P1-T8, P2-T5 |
| US-07 | M | P2-T6 |
| US-08 | M | P2-T6 |
| US-09 | M | P1-T1, P1-T6, P1-T7, P3-T6 |
| US-10 | M | P3-T2 |
| US-11 | M | P3-T3 |
| US-12 | M | P1-T7, P3-T5 |
| US-13 | M | P3-T1 |
| US-14 | S | P3-T4 |
| US-15 | M | P4-T1 |
| US-16 | M | P4-T3, P4-T4 |
| US-17 | M | P4-T3 |
| US-18 | M | P3-T6, P4-T2 |
| US-19 | M | P4-T5 |
| US-20 | S | P5-T6 |
| US-21 | M | P3-T5, P5-T6 |
| US-22 | S | P5-T6 |
| US-23 | S | P1-T2, P5-T3 |
| US-24 | S | P5-T1, P5-T4 |
| US-25 | M | P5-T5 |
| US-26 | M | P2-T4 |
| US-27 | M | P4-T6 |
| US-28 | M | P4-T7 |
| US-29 | M | P6-T4, P6-T5, P6-T6, P6-T7 |
| US-30 | M | P6-T1, P6-T2, P6-T3 |
| US-31 | M | P0-T5, P7-T1 |
| US-32 | M | P0-T5, P7-T2 |

Tasks with no story (enablers and thesis work): P0-T1 to P0-T4, P0-T6, P3-T7, P4-T8, P6-T8, P7-T3 to P7-T6, P8-T1 to P8-T7.

Every story maps to at least one task; every Must story is first checked no later than phase 5, so the cut list in PLAN.md section 9 never removes a Must story.

## Phase acceptance checklist

Run the phase demo from `docs/PLAN.md` section 5 on a clean clone and tick the criteria above.

| Phase | Demo | Stories checked | Done |
|---|---|---|---|
| P0 | Empty app runs in Docker; spike proves Georgian RAG end to end in a script | US-31 (skeleton), US-32 (stub) | [ ] |
| P1 | Upload a PDF, ask in Georgian, get an answer with material + page citations | US-06 (PDF), US-09 | [ ] |
| P2 | Lecturer course, enrollment, three formats, isolation | US-01, US-03 to US-08, US-26 | [ ] |
| P3 | Streamed multi-turn chat, citations, ratings, not-covered | US-10 to US-14 | [ ] |
| P4 | Assignments, hint ladder, refusal, jailbreaks fail | US-15 to US-19, US-27, US-28 | [ ] |
| P5 | Provider switch, cost and latency, rate limit, lecturer analytics | US-02, US-20 to US-25 | [ ] |
| P6 | One command per experiment produces the thesis CSVs | US-29, US-30 | [ ] |
| P7 | Clean-machine `docker compose up`, green CI, usability session | US-31, US-32 | [ ] |
