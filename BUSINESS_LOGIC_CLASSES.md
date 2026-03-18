# Business Logic Classes in AStar.Dev.OneDrive.Sync.Client

This document catalogs all service classes, business logic classes, and repositories with real business operations that benefit from unit testing.

---

## Core Sync Services

### 1. **SyncService**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/SyncService.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/SyncService.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Sync`
- **Description**: Orchestrates the complete sync process for OneDrive accounts—fetches delta changes, detects conflicts, builds sync jobs, and processes them via parallel download pipeline.

#### Key Methods (Worth Testing):
- `SyncAccountAsync(OneDriveAccount account, CancellationToken ct)` - Main entry point; handles auth, validates sync path, iterates selected folders
- `ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct)` - Applies conflict resolution policy and updates DB
- `SyncFolderAsync(...)` (private) - Fetches delta changes, classifies jobs, processes job queue, updates delta links
- `BuildJobs(..): List<SyncJob>` (private, static) - Maps delta items to sync jobs (download/delete operations)
- `ClassifyJobsAsync(...): (List<SyncJob> Clean, List<SyncConflict> Conflicts)` (private) - Detects conflicts by comparing local/remote modification times
- `ProcessJobQueueAsync(...)` (private) - Creates ParallelDownloadPipeline and orchestrates download workers
- `ApplyConflictOutcomeAsync(...)` (private) - Applies conflict outcome (skip, use local, use remote, keep both)

#### Events:
- `SyncProgressChanged` - Raised during sync operations
- `JobCompleted` - Raised when individual job completes
- `ConflictDetected` - Raised when conflict is detected

#### Dependencies (Mock These):
- `IAuthService` - Gets access tokens
- `IGraphService` - Fetches delta changes and file metadata
- `IAccountRepository` - Reads/updates account and folder state
- `ISyncRepository` - Manages sync jobs and conflicts

---

### 2. **GraphService**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/Graph/GraphService.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/Graph/GraphService.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Graph`
- **Description**: Wraps Microsoft Graph API for OneDrive operations including delta queries, folder enumeration, quota, and drive discovery. Implements smart caching for drive context.

#### Key Methods (Worth Testing):
- `GetDriveIdAsync(string accessToken, CancellationToken ct): Task<string>` - Returns user's default drive ID
- `GetRootFoldersAsync(string accessToken, CancellationToken ct): Task<List<DriveFolder>>` - Lists root-level folders, orders by name
- `GetChildFoldersAsync(string accessToken, string driveId, string parentFolderId, CancellationToken ct): Task<List<DriveFolder>>` - Lists child folders
- `GetQuotaAsync(string accessToken, CancellationToken ct): Task<(long Total, long Used)>` - Retrieves storage quota
- `GetDeltaAsync(string accessToken, string folderId, string? deltaLink, CancellationToken ct): Task<DeltaResult>` - **Core method**:
  - On first sync: performs **full enumeration** of folder tree recursively, returns delta link
  - On subsequent syncs: uses delta link for **incremental changes only**
  - Returns `DeltaResult` with items and next delta link
- `FullEnumerationAsync(...)` (private) - Recursively enumerates all files/folders in a directory tree
- `EnumerateFolderAsync(...)` (private) - Recursively walks folder hierarchy, builds relative paths
- `MapToDeltaItem(DriveItem item): DeltaItem` (private, static) - Maps Graph API DriveItem to domain model

#### State Management:
- `_cache: Dictionary<string, DriveContext>` - Caches drive context (DriveId, RootId, etc.) per access token

#### Dependencies (Mock These):
- Microsoft Graph SDK (GraphServiceClient)
- HTTP client (implicit via Graph SDK)

---

### 3. **ConflictResolver**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/ConflictResolver.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/ConflictResolver.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Sync`
- **Description**: Static utility class that implements conflict resolution business logic—applies policies to determine which version wins.

#### Key Methods (Worth Testing):
- `Resolve(ConflictPolicy policy, DateTimeOffset localModified, DateTimeOffset remoteModified): ConflictOutcome` - **Policy evaluation**:
  - `Ignore` → Skip (prompt user later)
  - `LocalWins` → UseLocal
  - `RemoteWins` → UseRemote
  - `KeepBoth` → KeepBoth (rename local)
  - `LastWriteWins` → Compares timestamps, returns UseLocal or UseRemote
- `MakeKeepBothName(string localPath, DateTimeOffset localModified): string` - Generates "keep both" filename: `report (local 2024-01-15 14-32).docx`

#### Business Logic:
- Policy-based decision making
- Timestamp comparison with 5-second tolerance
- Filename generation for "keep both" strategy

---

### 4. **SyncScheduler**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/SyncScheduler.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/SyncScheduler.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Sync`
- **Description**: Orchestrates scheduled sync passes for all connected accounts using a timer. Supports manual immediate sync triggering and configurable intervals.

#### Key Methods (Worth Testing):
- `Start(TimeSpan? interval = null)` - Initializes timer with interval (default 60 minutes)
- `Stop()` - Stops the timer
- `SetInterval(TimeSpan interval)` - Updates sync interval dynamically
- `TriggerNowAsync(CancellationToken ct): Task` - Triggers immediate sync for all accounts outside normal schedule
- `TriggerAccountAsync(OneDriveAccount account, CancellationToken ct): Task` - Triggers sync for single account
- `OnTimerTick(object? state)` (private) - Timer callback that prevents concurrent sync passes
- `RunSyncPassAsync(CancellationToken ct)` (private) - Loads all accounts from DB and syncs each one

#### State Management:
- `_timer: Timer?` - System timer
- `_interval: TimeSpan` - Current sync interval
- `_running: bool` - Prevents concurrent sync passes

#### Events:
- `SyncStarted` - Raised when sync starts
- `SyncCompleted` - Raised when sync completes

#### Dependencies (Mock These):
- `ISyncService` - Performs actual sync
- `IAccountRepository` - Loads accounts to sync

---

### 5. **StartupService**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/Startup/StartupService.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/Startup/StartupService.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Startup`
- **Description**: Loads persisted accounts at application startup, filtering out accounts with invalid tokens and ensuring only one active account.

#### Key Methods (Worth Testing):
- `RestoreAccountsAsync(): Task<List<OneDriveAccount>>` - **Startup logic**:
  1. Loads all accounts from database
  2. Gets cached MSAL token IDs
  3. Filters out accounts with expired/evicted tokens
  4. Maps AccountEntity → OneDriveAccount
  5. Ensures exactly one account is marked active (last-write wins if corrupted)
  6. Returns accounts in display order

#### Business Logic:
- Token cache validation (cross-device token eviction)
- Account filtering based on valid tokens
- Active account reconciliation (fixes data corruption)
- Entity → ViewModel mapping

#### Dependencies (Mock These):
- `IAccountRepository` - Loads accounts from database
- `IAuthService` - Gets list of cached account IDs from MSAL token cache

---

### 6. **SettingsService**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/SettingsService.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/SettingsService.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Settings`
- **Description**: Manages application-level settings (theme, locale, sync interval, default conflict policy) persisted to JSON file.

#### Key Methods (Worth Testing):
- `LoadAsync(): Task<SettingsService>` (static) - Loads settings from disk, returns new SettingsService instance
- `SaveAsync(): Task` - Serializes current settings to JSON, raises SettingsChanged event
- Constructor - Initializes data directory path

#### Properties:
- `Current: AppSettings` (get/set)
- `SettingsChanged: EventHandler<AppSettings>?` - Event fired after SaveAsync

#### Persistence:
- JSON file location: `{PlatformDataDir}/settings.json`
- Uses `JsonSerializerOptions` with camelCase naming

#### Dependencies (Mock These):
- File I/O (System.IO)
- Platform data directory (DbContextFactory)

---

### 7. **AuthService**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/AuthService.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/AuthService.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Auth`
- **Description**: Wraps MSAL for Azure AD authentication with OneDrive scopes. Supports both interactive and silent token acquisition with automatic token cache management.

#### Key Methods (Worth Testing):
- `SignInInteractiveAsync(CancellationToken ct): Task<AuthResult>` - Browser-based login using system browser + loopback redirect
  - Handles user cancellation gracefully
  - Returns success/failure/cancelled result
- `AcquireTokenSilentAsync(string accountId, CancellationToken ct): Task<AuthResult>` - Gets access token from cache or refreshes
  - Returns failure if account not found
  - Detects MsalUiRequiredException (token expired, needs re-auth)
- `SignOutAsync(string accountId, CancellationToken ct): Task` - Removes account from MSAL token cache
- `GetCachedAccountIdsAsync(): Task<IReadOnlyList<string>>` - Lists all accounts in local token cache
- `EnsureCacheRegisteredAsync()` (private) - Lazy-initializes file-backed token cache

#### MSAL Configuration:
- Authority: `https://login.microsoftonline.com/consumers` (personal accounts only)
- Scopes: `Files.ReadWrite`, `offline_access`, `User.Read`
- Redirect: `http://localhost` (loopback)
- Uses system browser (no WebView2 dependency)

#### Error Handling:
- Distinguishes cancellation from auth failures
- Maps MSAL exceptions to user-friendly messages

#### Dependencies (Mock These):
- `TokenCacheService` - File-backed token cache
- MSAL `IPublicClientApplication`

---

## Download & File Operations

### 8. **HttpDownloader**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/HttpDownloader.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/HttpDownloader.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Sync`
- **Description**: Handles file downloads with automatic retry logic and exponential backoff for rate limiting. Preserves remote file's last-modified timestamp locally.

#### Key Methods (Worth Testing):
- `DownloadAsync(string url, string localPath, DateTimeOffset remoteModified, IProgress<long>? progress, CancellationToken ct): Task` - **Core download logic**:
  1. Sends GET request with `ResponseHeadersRead` mode
  2. On HTTP 429: waits using exponential backoff (respects Retry-After header)
  3. Retries up to 5 times, throws if max retries exceeded
  4. Creates parent directories if needed
  5. Streams content to disk with optional progress reporting
  6. Sets local file's last-modified time to remote value
  7. Disposes response and stream

#### Rate Limiting Handling:
- Max 5 retries on 429 Too Many Requests
- Exponential backoff: base 2s, max 120s
- Respects `Retry-After` header when present
- Falls back to calculated delay if header missing

#### Backpressure:
- Single shared HttpClient across all downloads (connection pooling)

#### Dependencies (Mock These):
- `HttpClient` - HTTP communication

---

### 9. **DownloadWorker**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/DownloadWorker.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/DownloadWorker.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Sync`
- **Description**: Single worker that drains sync jobs from a bounded channel and executes them (download/delete). Multiple workers run concurrently.

#### Key Methods (Worth Testing):
- `RunAsync(ChannelReader<SyncJob> reader, string accessToken, Action<SyncJob, bool, string?> onJobComplete, CancellationToken ct): Task` - **Worker loop**:
  1. Reads jobs from channel
  2. Updates job state to InProgress
  3. Executes job (download or delete)
  4. On success: updates state to Completed
  5. On cancel: re-queues job (state back to Queued)
  6. On error: updates state to Failed with error message
  7. Invokes callback for each completion
- `ExecuteJobAsync(SyncJob job, CancellationToken ct)` (private) - Executes job based on direction:
  - **Download**: calls HttpDownloader
  - **Delete**: removes local file
  - **Upload**: placeholder for future implementation

#### State Management:
- Job state transitions: Queued → InProgress → {Completed, Failed, Queued}
- Worker ID for logging

#### Error Handling:
- Catches OperationCanceledException separately (re-queues)
- Catches general exceptions (marks failed)
- Always invokes callback in finally block

#### Dependencies (Mock These):
- `HttpDownloader` - File downloads
- `ISyncRepository` - Updates job state
- Channel<SyncJob> - Job queue

---

### 10. **ParallelDownloadPipeline**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/ParallelDownloadPipeline.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/Sync/ParallelDownloadPipeline.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Services.Sync`
- **Description**: Orchestrates parallel file downloads using bounded channels for backpressure. Prevents memory explosion even with 300k+ files by limiting in-memory job queue.

#### Key Methods (Worth Testing):
- `RunAsync(IEnumerable<SyncJob> jobs, string accessToken, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, string accountId, string folderId, CancellationToken ct): Task` - **Pipeline orchestration**:
  1. Validates non-empty job list
  2. Creates bounded channel with capacity = `workerCount * 4`
  3. Launches N worker tasks (concurrently)
  4. Producer loop feeds jobs into channel with backpressure
  5. Aggregates completion callbacks
  6. Tracks total/completed/failed counts
  7. Invokes progress callback after each job

#### Channel Configuration:
- Bounded capacity: `workers * 4` jobs
- SingleWriter: true (one producer)
- SingleReader: false (N workers)
- FullMode: Wait (blocks when full)

#### Progress Tracking:
- Tracks done = completed + failed
- Reports current file being processed
- Indicates when pipeline is complete

#### Memory Efficiency:
- Bounded channel prevents loading all jobs into memory
- Backpressure keeps memory constant even with 300k files

#### Dependencies (Mock These):
- `DownloadWorker` (created internally)
- `HttpDownloader` (passed to workers)
- `ISyncRepository` (passed to workers)

---

## Data Repositories

### 11. **AccountRepository**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Data/Repositories/AccountRepository.cs](src/AStar.Dev.OneDrive.Sync.Client/Data/Repositories/AccountRepository.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Data.Repositories`
- **Description**: Data access layer for account persistence—manages account storage, folder selections, delta links, and sync state.

#### Key Methods (Worth Testing):
- `GetAllAsync(): Task<List<AccountEntity>>` - Returns all accounts ordered by email, includes sync folders
- `GetByIdAsync(string id): Task<AccountEntity?>` - Loads single account by ID with sync folders
- `UpsertAsync(AccountEntity account): Task` - **Smart merge logic**:
  1. If account exists: updates scalar properties, syncs folder collection
  2. Removes folders not in new collection
  3. Adds new folders
  4. If new: inserts new row
  5. Saves changes
- `DeleteAsync(string id): Task` - Deletes account by ID
- `SetActiveAccountAsync(string id): Task` - Marks one account as active, clears others
- `UpdateDeltaLinkAsync(string accountId, string folderId, string deltaLink): Task` - Updates delta link for folder

#### Business Logic:
- Efficient EF Core LINQ usage
- Folder collection synchronization (merge, don't replace)
- Bulk updates with `ExecuteUpdateAsync`
- Bulk deletes with `ExecuteDeleteAsync`

#### Dependencies (Mock These):
- `AppDbContext` - EF Core context

---

### 12. **SyncRepository**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Data/Repositories/SyncRepository.cs](src/AStar.Dev.OneDrive.Sync.Client/Data/Repositories/SyncRepository.cs)
- **Namespace**: `AStar.Dev.OneDrive.Sync.Client.Data.Repositories`
- **Description**: Data access layer for sync jobs and conflicts—manages job queue and conflict tracking.

#### Key Methods (Worth Testing):
- `EnqueueJobsAsync(IEnumerable<SyncJob> jobs): Task` - Converts SyncJob models to SyncJobEntity, inserts all at once
- `GetPendingJobsAsync(string accountId): Task<List<SyncJobEntity>>` - Returns queued jobs for account, ordered by enqueue time
- `UpdateJobStateAsync(Guid jobId, SyncJobState state, string? error): Task` - Updates job state with optional error, sets CompletedAt timestamp
- `ClearCompletedJobsAsync(string accountId): Task` - Deletes all completed jobs for account
- `AddConflictAsync(SyncConflict conflict): Task` - Converts SyncConflict model to SyncConflictEntity, inserts
- `GetPendingConflictsAsync(string accountId): Task<List<SyncConflictEntity>>` - Returns unresolved conflicts for account, ordered by detection time
- `ResolveConflictAsync(Guid conflictId, ConflictPolicy resolution): Task` - Marks conflict as resolved with policy
- `GetPendingConflictCountAsync(string accountId): Task<int>` - Returns count of unresolved conflicts

#### Business Logic:
- Job state machine: Queued → InProgress → {Completed, Failed, Skipped}
- Conflict resolution tracking (policy applied)
- Automatic timestamp management (CompletedAt, ResolvedAt)
- Efficient bulk operations

#### Dependencies (Mock These):
- `AppDbContext` - EF Core context

---

## Value Converters (Data Transformation)

These are lightweight converter classes that transform data for UI display. While not complex, they implement specific business logic for presentation.

### 13. **DashboardConverters**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Converters/DashboardConverters.cs](src/AStar.Dev.OneDrive.Sync.Client/Converters/DashboardConverters.cs)
- **Classes**:
  - `ConflictCountToColorConverter` - Returns red (#E24B4A) if conflict count > 0, else dark gray
  - `DepthToIndentConverter` - Converts tree depth (int) to left-margin pixels (16px per depth level)

---

### 14. **SyncStateConverters**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Converters/SyncStateToBadgeBackgroundConverter.cs](src/AStar.Dev.OneDrive.Sync.Client/Converters/SyncStateToBadgeBackgroundConverter.cs)
- **Classes**:
  - `SyncStateToBadgeBackgroundConverter` - Maps FolderSyncState enum to badge background colors:
    - Synced → Light green (#EAF3DE)
    - Syncing/Included → Light blue (#E6F1FB)
    - Partial/Conflict → Light yellow (#FAEEDA)
    - Error → Light red (#FCEBEB)

---

### 15. **BugVarianConverters** (Miscellaneous)
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Converters/BoolToExcludeIncludeLabelConverter.cs](src/AStar.Dev.OneDrive.Sync.Client/Converters/BoolToExcludeIncludeLabelConverter.cs)
  - `BoolToExcludeIncludeLabelConverter` - Converts bool to "Exclude" (true) or "Include" (false) button label
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Converters/IntZeroToBoolConverter.cs](src/AStar.Dev.OneDrive.Sync.Client/Converters/IntZeroToBoolConverter.cs)
  - `IntZeroToBoolConverter` - Returns true if int == 0 (for empty-state visibility)

---

## Other Services

### 16. **ThemeService**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/ThemeService.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/ThemeService.cs)
- **Description**: Applies theme changes to the Avalonia application
- **Key Method**: `Apply(AppTheme theme)` - Sets theme on Application instance

---

### 17. **TokenCacheService**
- **File**: [src/AStar.Dev.OneDrive.Sync.Client/Services/TokenCacheService.cs](src/AStar.Dev.OneDrive.Sync.Client/Services/TokenCacheService.cs)
- **Description**: File-backed MSAL token cache for storing encrypted tokens on disk
- **Key Methods**:
  - `RegisterAsync(IPublicClientApplication app)` - Registers file cache with MSAL app instance
  - `GetPlatformCacheDirectory()` - Returns OS-specific cache directory (Windows/Linux/macOS)

---

## Testing Strategy Summary

### Classes That Benefit Most from Unit Testing:
1. **SyncService** - Complex state machine, conflict detection, job building
2. **ConflictResolver** - Policy implementation, edge cases (timestamp comparison)
3. **ParallelDownloadPipeline** - Channel semantics, backpressure, progress tracking
4. **StartupService** - Token cache validation, active account resolution
5. **GraphService** - Delta query logic, recursive enumeration, caching
6. **HttpDownloader** - Rate limiting, retry logic, exponential backoff
7. **AccountRepository** - Collection merging, bulk operations
8. **SyncRepository** - State transitions, bulk inserts

### High-Value Mocking:
- **IAuthService** - Return configurable AuthResult
- **IGraphService** - Return synthetic delta results
- **IAccountRepository** - In-memory storage
- **ISyncRepository** - In-memory storage
- **HttpClient** - Return mock responses (rate limiting, errors)
- **System.IO** - Use temporary directories or in-memory file systems

