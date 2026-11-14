# AiMentor

AI-mentor web platform supporting the learning process, based on RAG and the Socratic dialogue method (GTU bachelor thesis). Plan: [docs/PLAN.md](docs/PLAN.md).

## Prerequisites

- .NET SDK 10.0.403 (pinned in `global.json`)
- Docker Desktop (for SQL Server 2025 and the app container)

## Build and test

```bash
dotnet build AiMentor.slnx -c Release
dotnet test -c Release
```

Tests run on Microsoft Testing Platform (`global.json` opts `dotnet test` in). Real-model evaluation tests run only with `AIMENTOR_EVAL=1`.

## Run with Docker

```bash
cp .env.example .env   # set MSSQL_SA_PASSWORD
docker compose up --build
```

App: http://localhost:8080 (health: `/health`, `/alive`). SQL Server is exposed on port 14333.

## Layout

| Path | Content |
|---|---|
| `src/` | Domain, Application, Infrastructure, Web |
| `tests/` | Unit, integration (Testcontainers), evaluation (opt-in) |
| `experiments/` | Experiment runner, test set, committed CSV results |
| `spike/` | Throwaway P0-T6 spike, not part of the solution |
| `docs/` | Plan, requirements, backlog, spikes, diagrams, thesis notes |

Course materials are copyrighted and never committed: they live in the git-ignored `data/` folder or the `materials` Docker volume. Secrets come only from User Secrets or environment variables.
