# <Feature Name> Test Matrix

## 1. Unit Tests
| Case ID | Related REQ | Scenario | Input | Expected |
|---|---|---|---|---|
| UT-001 | REQ-001 | Happy path |  |  |
| UT-002 | REQ-001 | Invalid param |  |  |
| UT-003 | REQ-002 | Boundary condition |  |  |

## 2. Integration Tests
| Case ID | Related REQ | Scenario | Precondition | Expected |
|---|---|---|---|---|
| IT-001 | REQ-001 | API -> Service -> Domain |  |  |
| IT-002 | REQ-002 | Domain -> DAL persistence |  |  |

## 3. Regression Checks
| Case ID | Area | Risk | Verification |
|---|---|---|---|
| RG-001 | Existing API behavior | Response regression | Compare before/after |
| RG-002 | Data consistency | Wrong key/index usage | Verify PK/SK/GSI queries |

## 4. Run Commands
- `$env:GOCACHE=(Join-Path (Get-Location) '.gocache'); go test <pkg> -v`
- `if (Test-Path '.gocache') { Remove-Item -Recurse -Force '.gocache' }`

## 5. Known Gaps
- 
