# <Feature Name> Tasks

## Task List
1. TASK-001: <Task title>
- Related REQ: `REQ-001`
- Files:
- `biz/api/...`
- `biz/service/...`
- DoD:
- Code implemented
- Unit tests updated
- Logs/errors aligned with existing style

2. TASK-002: <Task title>
- Related REQ: `REQ-002`
- Files:
- `biz/domain/...`
- `biz/dal/...`
- DoD:
- DynamoDB key/index usage verified
- Compatibility plan documented

3. TASK-003: Validation
- Related REQ: `REQ-001`, `REQ-002`
- Commands:
- `gofmt -w <changed_files>`
- `$env:GOCACHE=(Join-Path (Get-Location) '.gocache'); go test <related_packages> -v`
- `if (Test-Path '.gocache') { Remove-Item -Recurse -Force '.gocache' }`
- DoD:
- Tests pass
- No unrelated file changes
