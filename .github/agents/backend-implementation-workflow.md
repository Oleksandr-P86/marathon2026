# Restaurant Backend Implementation Workflow

Use the **Restaurant Backend Orchestrator** for user requirements that need backend code changes in `backend/restaurant-backend-app`. It coordinates the existing specialist agents; it does not replace their focused responsibilities.

## Delegation plan

1. **Turn the requirement into acceptance criteria.** Capture expected behavior, API or message contracts, validation and authorization, error handling, compatibility, and relevant constraints. Resolve material ambiguities with the user before implementation.
2. **Implement — AWS Lambda Implementer.** Pass the requirement and acceptance criteria to the implementer. It changes production code and directly related backend docs/configuration, then reports files changed and build/test evidence. It does not write unit tests or deploy.
3. **Test — Restaurant Backend Test Writer.** Pass the criteria, implementation report, and production-file list to the test writer. It adds or updates focused backend unit tests and runs the relevant test command. It does not change production code to make tests pass.
4. **Review — Restaurant Backend Code Reviewer.** Ask the reviewer to inspect the resulting backend diff against the criteria and report only actionable defects, with severity, changed lines, confidence, and a suggested fix. The reviewer does not edit files.
5. **Resolve findings.** Route concrete production defects to the implementer. After a fix, have the test writer rerun affected tests and the reviewer inspect the updated diff. If a finding cannot be resolved, preserve it in the final report.
6. **Report completion.** Summarize the delivered behavior, acceptance criteria, changed files, test/build commands and actual results, review findings, assumptions, and anything not completed. Do not claim unrun checks passed.

## Scope and safety

- Keep implementation and tests within the backend project and directly related backend documentation/configuration.
- Ask the user before materially expanding a request into frontend or unrelated infrastructure changes.
- Do not deploy, use AWS credentials, or change live resources as part of ordinary implementation. Deployment requires a separate explicit request and target-environment confirmation.
- If a handoff cannot be completed, report that stage as blocked; do not treat an omitted stage as a successful result.
