## 🛡️ Comprehensive code review fixes: security hardening, async safety, and error handling

### Summary

Full codebase audit of Workflow Core identifying and fixing **58 issues** across the core engine, DSL module, persistence/queue/lock providers, and extensions. Fixes span **35 files** with changes grouped into 6 categories.

### 🔴 Security (Critical)

- **Disabled `AllowNewToEvaluateAnyType`** in Dynamic LINQ parsing config — previously allowed arbitrary type instantiation (e.g., `System.Diagnostics.Process`) in user-supplied workflow expressions (DSL `DefinitionLoader`)
- **Added `TypeNameHandling.None`** to JSON workflow deserialization to prevent deserialization gadget chain attacks (`Deserializers.cs`)
- **Added type resolution error handling** in `TypeResolver.FindType` to fail gracefully on invalid types
- **Added SQL identifier validation** in `SqlServerQueueProvider` to prevent injection via queue name string replacement
- **Added OData filter sanitization** in `SearchService` for Azure Search queries

### 🔴 Async & Concurrency (Critical/High)

- **Eliminated all `async void` methods** (4 instances) — converted to `async Task` with proper tracking in:
  - `WorkflowConsumer.FutureQueue`
  - `AzureLockManager.RenewLeases`
  - `DynamoLockProvider.SendHeartbeat`
  - `KinesisStreamConsumer.Process`
- **Removed blocking `.Wait()` calls** (6 instances) — replaced with `GetAwaiter().GetResult()` where sync API is required, or `await` where async is possible:
  - `WorkflowHost.Start/Stop`
  - `QueueConsumer.Stop` (added 30s timeout)
  - `DynamoPersistenceProvider.EnsureStoreExists`
  - `DynamoLockProvider.Stop`
  - `SqlServerQueueProvider.Dispose`
- **Fixed fire-and-forget `Task.Run`** in `SingleNodeEventHub.PublishNotification` — now properly awaited
- **Fixed TOCTOU race conditions** in `WorkflowRegistry` (ContainsKey → TryGetValue) and `GreyList`
- **Replaced thread-unsafe `HashSet`** with `ConcurrentBag` in `SingleNodeEventHub`
- **Replaced manual `lock` + `Dictionary`** with `ConcurrentDictionary` in `IndexConsumer`
- **Synchronized static `indexesCreated`** flag in MongoDB and RavenDB providers
- **Fixed `GetOrCreate` race condition** in `InMemoryConversationStore` using `GetOrAdd`

### 🟠 Resource Management (High)

- **Replaced `cn.Close()` with `using` blocks** in `SqlServerQueueProvider` and `SqlServerQueueProviderMigrator` (5 methods) to prevent connection leaks
- **Implemented proper `Dispose`** in `SingleNodeQueueProvider` — `BlockingCollection` was never disposed
- **Fixed `ServiceProvider` disposal** in `WorkflowTest` and `JsonWorkflowTest`
- **Added transaction abort on failure** in MongoDB `PersistWorkflow`
- **Used `DisposeAsync`** for RabbitMQ channel cleanup

### 🟠 Error Handling (High)

- **Replaced unsafe LINQ methods** — `First()` → `FirstOrDefault()`, `Single()` → `SingleOrDefault()` in `MemoryPersistenceProvider`; `FirstAsync()` → `FirstOrDefaultAsync()` in `EntityFrameworkPersistenceProvider` (9 call sites)
- **Added constructor validation** before `Invoke(null)` in DSL `DefinitionLoader` — prevents `NullReferenceException` on types without parameterless constructors
- **Wrapped `DynamicInvoke` calls** with `TargetInvocationException` unwrapping for meaningful error messages
- **Added safe `Enum.Parse`** with try-catch and descriptive error for invalid values
- **Replaced bare `catch`** with specific `ArgumentException` handling in expression property resolution
- **Added `ParseLambda` error wrapping** with step/expression context in error messages
- **Used `TryGetValue`** instead of direct dictionary access in `SqlLockProvider`
- **Fixed `throw ex` → `throw`** in `SqlLockProvider` to preserve stack traces
- **Added exception logging** in MongoDB `ProcessCommands` (was silently swallowed with a TODO)
- **Fixed lock release** in `ActivityController` — was releasing locks that were never acquired
- **Added null step logging** in `WorkflowExecutor.ProcessAfterExecutionIteration`

### 🟡 API & Validation (Medium)

- **Added input validation** in `WorkflowsController` — pagination bounds, null checks, `404 Not Found` instead of `400 Bad Request` for missing workflow definitions
- **Added input validation** in `EventsController` — null/empty checks for event name and key
- **Added source/deserializer validation** in AI `DefinitionLoader`
- **Added null guard** in `ToolRegistry` constructor

### 🟢 Cleanup (Low)

- Fixed compensation chain pointer logic in `CompensateHandler`
- Removed unused `JsonSerializerSettings` field from `RabbitMQProvider`
- Set `activity = null` after dispose in `QueueConsumer` to prevent double-dispose
- Added logging in `QueueConsumer` `Task.WhenAll` catch block

### Testing

- ✅ All **58 unit tests pass** (unchanged from baseline)
- No test files were modified (except `WorkflowTest`/`JsonWorkflowTest` for the `ServiceProvider` disposal fix)

### Stats

- **35 files changed**, 363 insertions, 224 deletions
- **58 issues** fixed (10 critical, 23 high, 18 medium, 7 low)
