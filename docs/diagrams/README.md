# Diagrams

Sources only; rendered images are produced for the thesis in P8-T2.

| File | Type | Status |
|---|---|---|
| `use-case.puml` | UML Use Case (PlantUML) | Draft (P0-T4). Not render-checked yet: needs Java + PlantUML or the VS Code PlantUML extension |
| `er-model.mmd` | ER (Mermaid) | Draft (P0-T4), renders with mermaid-cli 11 |
| `architecture-components.mmd` | C4 container / component (Mermaid) | Draft (P0-T4), renders with mermaid-cli 11 |

Render Mermaid locally:

```bash
npx -y @mermaid-js/mermaid-cli@11 -i docs/diagrams/er-model.mmd -o er-model.svg
```

Render PlantUML (needs Java):

```bash
java -jar plantuml.jar docs/diagrams/use-case.puml
```
