"""Create GitHub issues for every story and task, and a Projects board (P0-T2).

Reads docs/backlog.md (stories) and docs/PLAN.md (tasks). Dry run by default.

    python scripts/create_github_board.py                      # print plan
    python scripts/create_github_board.py --apply --repo OWNER/REPO --owner OWNER

Requires: gh CLI logged in with scopes repo + project (gh auth refresh -s project).
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STORY_RE = re.compile(r"^\*\*(US-\d+) (.+?)\*\* \((\w), phase ([^)]+)\)\. (.+)$")
TASK_RE = re.compile(r"^\| (P\d-T\d) \| ([^|]+) \| ([^|]+) \| (\d+) \| ([^|]+) \| ([^|]+) \| ([^|]+) \|$")
PRIORITY = {"M": "Must", "S": "Should", "C": "Could"}


def parse_stories() -> list[dict]:
    stories, current = [], None
    for line in (ROOT / "docs/backlog.md").read_text(encoding="utf-8").splitlines():
        if m := STORY_RE.match(line):
            current = {"id": m[1], "title": m[2], "priority": PRIORITY[m[3]], "phase": m[4], "text": m[5], "ac": []}
            stories.append(current)
        elif current and line.startswith("- [ ] "):
            current["ac"].append(line)
        elif current and not line.strip() == "" and not line.startswith("- "):
            current = None
    return stories


def parse_tasks() -> list[dict]:
    tasks = []
    for line in (ROOT / "docs/PLAN.md").read_text(encoding="utf-8").splitlines():
        if m := TASK_RE.match(line.strip()):
            tasks.append({"id": m[1], "title": m[2].strip(), "desc": m[3].strip(), "hours": int(m[4]),
                          "depends": m[5].strip(), "done": m[6].strip(), "thesis": m[7].strip()})
    return tasks


def gh(*args: str) -> str:
    return subprocess.run(["gh", *args], check=True, capture_output=True, text=True).stdout


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--repo")
    ap.add_argument("--owner")
    ap.add_argument("--title", default="AiMentor thesis")
    a = ap.parse_args()

    stories, tasks = parse_stories(), parse_tasks()
    print(f"{len(stories)} stories, {len(tasks)} tasks, {sum(t['hours'] for t in tasks)} h")
    if not a.apply:
        for s in stories:
            print(f"  story {s['id']} [{s['priority']}, phase {s['phase']}] {s['title']} ({len(s['ac'])} AC)")
        for t in tasks:
            print(f"  task  {t['id']} {t['title']} ({t['hours']} h)")
        print("Dry run. Pass --apply --repo OWNER/REPO --owner OWNER to create them.")
        return 0
    if not (a.repo and a.owner):
        ap.error("--apply needs --repo and --owner")

    for label, color in [("story", "1d76db"), ("task", "0e8a16")] + [(f"phase-{i}", "c5def5") for i in range(9)]:
        subprocess.run(["gh", "label", "create", label, "--repo", a.repo, "--color", color, "--force"], check=True)

    project = json.loads(gh("project", "create", "--owner", a.owner, "--title", a.title, "--format", "json"))
    number = str(project["number"])

    for s in stories:
        body = f"{s['text']}\n\nPriority: {s['priority']}. Phase: {s['phase']}.\n\nAcceptance criteria:\n" + "\n".join(s["ac"])
        url = gh("issue", "create", "--repo", a.repo, "--title", f"{s['id']} {s['title']}", "--body", body, "--label", "story").strip()
        gh("project", "item-add", number, "--owner", a.owner, "--url", url)
        print("created", url)
    for t in tasks:
        body = (f"{t['desc']}\n\nEstimate: {t['hours']} h. Depends on: {t['depends']}.\n\n"
                f"Done when: {t['done']}\n\nThesis: {t['thesis']}")
        phase = f"phase-{t['id'][1]}"
        url = gh("issue", "create", "--repo", a.repo, "--title", f"{t['id']} {t['title']}", "--body", body,
                 "--label", "task", "--label", phase).strip()
        gh("project", "item-add", number, "--owner", a.owner, "--url", url)
        print("created", url)
    return 0


if __name__ == "__main__":
    sys.exit(main())
