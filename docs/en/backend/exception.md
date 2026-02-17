# Exception Handling

Art Admin uses typed exceptions with global middleware that automatically converts them to standard JSON responses.

## Exception Types

| Exception | HTTP Status | Usage |
| --- | --- | --- |
| `BadRequestException` | 400 | Parameter error, business rule violation |
| `UnauthorizedException` | 401 | Not logged in, Token expired |
| `ForbiddenException` | 403 | No permission |
| `NotFoundException` | 404 | Resource not found |
| `InternalServerException` | 500 | Internal error |

## Usage

```csharp
public async Task<UserDto> GetUserAsync(long id)
{
    var user = await _db.SysUsers.FindAsync(id);
    if (user == null)
        throw new NotFoundException("User not found");

    if (user.Status != 1)
        throw new BadRequestException("User is disabled");

    return new UserDto { ... };
}
```

### Coding Style

Single-line `if` without braces:

```csharp
// ✅ Correct
if (user == null)
    throw new NotFoundException("User not found");

// ❌ Wrong
if (user == null) {
    throw new NotFoundException("User not found");
}
```

## Response Format

The global exception middleware catches all typed exceptions and returns:

```json
{
  "code": 404,
  "msg": "User not found"
}
```

## Successful Response

Normal returns are automatically wrapped:

```json
{
  "code": 200,
  "data": { ... }
}
```

## Frontend Integration

The frontend HTTP client (`src/utils/http`) has global `ElMessage.error` interception. **Do not add duplicate error messages in catch blocks** — just restore state:

```typescript
try {
  await fetchSaveUser(formData);
} catch (e) {
  // ❌ Don't do this — duplicate toast
  // ElMessage.error('Save failed');

  // ✅ Just restore state
  loading.value = false;
}
```
