# SPEC Guide (SDD for g003_biz)

## 1. Purpose
- Keep requirements executable, testable, and traceable.
- Make Codex changes map directly to spec items.

## 2. Directory Layout
- `SPEC/README.md`: global conventions and workflow.
- `SPEC/changelog.md`: spec version history.
- `SPEC/_template/requirement.md`: requirement understanding and review template.
- `SPEC/_template/spec.md`: feature requirement template.
- `SPEC/_template/tasks.md`: implementation task template.
- `SPEC/_template/tests.md`: test matrix template.
- `SPEC/_template/traceability.md`: requirement-code-test mapping template.

## 3. Feature Spec Placement
- One feature per directory:
- `SPEC/<feature_name>/requirement.md`
- `SPEC/<feature_name>/spec.md`
- `SPEC/<feature_name>/tasks.md`
- `SPEC/<feature_name>/tests.md`
- `SPEC/<feature_name>/traceability.md`

Example:
- `SPEC/user_card_collection_reward/requirement.md`
- `SPEC/user_card_collection_reward/spec.md`
- `SPEC/user_card_collection_reward/tasks.md`
- `SPEC/user_card_collection_reward/tests.md`
- `SPEC/user_card_collection_reward/traceability.md`

## 4. Writing Rules
- `requirement.md` must be created and reviewed first.
- Do not create `spec.md`, `tasks.md`, `tests.md`, or `traceability.md` before `requirement.md` is confirmed.
- Use short requirement IDs: `REQ-001`, `REQ-002`.
- Every `REQ-*` must have acceptance criteria.
- Every task must map to at least one `REQ-*`.
- Every implemented `REQ-*` must map to tests.
- Explicitly state compatibility strategy:
- A. backward-compatible plan
- B. direct-change plan
- Keep scope explicit: In Scope / Out of Scope.

## 5. g003_biz Layer Mapping
- API behavior changes: check `biz/api/*`, `biz/service/*`, `biz/constant/error.go`.
- Data model changes: check `biz/dal/model/*`, `biz/dal/*`, `biz/domain/*`.
- GM changes: check `biz/api/gm/gm.go`, `biz/service/gm.go`.
- DynamoDB: document PK/SK/GSI impact and migration/rollback.

## 6. SDD Workflow with Codex
1. Create `SPEC/<feature_name>/requirement.md`.
2. Use `requirement.md` to align requirement points, business flow, AI understanding, and review questions.
3. Stop and review `requirement.md` with the requester.
4. Only after `requirement.md` is confirmed, create `SPEC/<feature_name>/spec.md`.
5. Review constraints and acceptance criteria.
6. Only after `spec.md` is ready, split work in `tasks.md` (small, commit-friendly).
7. Define test matrix in `tests.md`.
8. Implement task by task.
9. Update `traceability.md` with file/test links.
10. Record spec updates in `SPEC/changelog.md`.

## 7. Completion Definition
- All in-scope `REQ-*` implemented.
- Related package tests executed and recorded.
- Known risks and uncovered cases documented.
- Traceability entries complete.
