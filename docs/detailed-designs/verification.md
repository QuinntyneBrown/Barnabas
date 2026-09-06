# Design verification

Verification completed on 2026-09-06 for the [design catalogue](README.md).

| Check | Result |
|---|---|
| Feature coverage | 39 designs in 10 subsystems: 17 existing paths revised and 22 added |
| Requirements | All 120 L2 requirements mapped to their exact parent among 18 L1 requirements |
| Requirement wording | Every feature table matches the source text after whitespace normalisation; identifiers remain verbatim |
| Acceptance references | All 343 source criteria represented by API/E2E references; 346 feature references include the shared photo requirement twice |
| Feature structure | Every feature has Overview, Description, Requirements, and Diagrams in order |
| PlantUML rendering | 235 sources rendered to 235 PNG siblings; renderer reported 235 rendered, 0 failed |
| PlantUML syntax | Independent whole-tree `-checkonly -failfast2` completed with exit code 0 |
| Image references | All 235 feature image links resolve to current, nonempty PNG files |
| Markdown navigation | Local file links and heading anchors resolve, including requirement and source anchors |
| C4 conventions | All 117 C4 sources use offline standard-library includes and macro elements/relationships; no raw-shape substitutes |
| Behaviour conventions | Every sequence includes coloured Frontend and Backend layer boxes, return messages, and L2 traces |
| House style | Feature prose checked for first/second-person address, prohibited obligation language, and filler expressions; literal product labels and exact requirements remain intact |
| Visual inspection | Representative component, class, and sequence images inspected; runtime concurrency and operational boundaries corrected before the final render |
| Change boundaries | Changes confined to `docs/detailed-designs`; requirements and application source unchanged |
| Whitespace | `git diff --check` completed cleanly |

The render used the software-design-document skill's `scripts/render_puml.py`, with the locally available PlantUML 1.2026.8 and Java 21. PNG output occupies approximately 11.4 MB. The syntax pass independently checked source validity because the renderer's PNG-existence check alone cannot distinguish an error image from a valid diagram.

The structural verification parsed the L2 source, compared each requirement table cell and L1 parent, checked acceptance references, resolved Markdown anchors, and inspected every diagram's source conventions and PNG signature. File timestamps confirmed that the rendered siblings were current.

These checks verify documentation, traceability, and diagram artifacts. Application acceptance and performance suites were not run for this documentation-only change. “Existing” means the named code was inspected; it does not certify that every associated acceptance criterion passes. Planned behaviour remains labelled, and the [open decisions register](open-decisions.md) retains unresolved specification inputs and conflicts.
