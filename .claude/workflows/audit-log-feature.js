
export const meta = {
  name: 'audit-log-feature',
  description: 'Implement Audit Log: read+write API, split-screen UI, tests with CodeWriter/Reviewer/Tester loop',
  phases: [
    { title: 'Implement API' },
    { title: 'Generate + Build' },
    { title: 'Review + Fix' },
    { title: 'Implement UI + API Tests' },
    { title: 'Final Verify' }
  ]
}

const ROOT = 'D:\\repos\\github\\claude-mem\\admin'

// ── Phase 1: CodeWriter implements all API code ─────────────────────────────
phase('Implement API')

await agent(`
You are a senior C# developer implementing the Audit Log feature for the ClaudeMem Admin project.
Working directory: D:\\repos\\github\\claude-mem\\admin

CRITICAL STYLE RULES (from project memory — NEVER deviate):
- Full {} braces ALWAYS — never single-line if/return without braces
- Access layer: Task<T> return type, NOT ValueTask<T>
- Mediator handlers implement ICommandHandler/IQueryHandler which REQUIRE ValueTask<TResult> — follow that contract
- internal sealed for all feature classes/records; public sealed for controllers
- No inline field initializers in constructors
- No comments unless the WHY is completely non-obvious
- Dapper: DefaultTypeMap.MatchNamesWithUnderscores = true is already globally set

STEP 1 — Read these reference files FIRST (read them all before writing anything):
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Jobs\\JobAccess.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Jobs\\JobsController.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Jobs\\Get\\GetJobsHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Jobs\\Get\\GetJobsQuery.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Jobs\\GetJobsFilter.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Teams\\TeamAccess.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Teams\\Create\\CreateTeamHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Projects\\Create\\CreateProjectHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\ApiKeys\\Create\\CreateApiKeyHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\ApiKeys\\Create\\CreateApiKeyCommand.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\ApiKeys\\Delete\\DeleteApiKeyHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\ApiKeys\\Delete\\DeleteApiKeyCommand.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\ApiControllerBase.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\Results\\ResultErrorCodes.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\Pagination\\CursorPageOptions.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\Pagination\\CursorRequest.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\Pagination\\CursorPageResult.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\Pagination\\ICursorFilter.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\Pagination\\CursorPayload.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Program.cs
- D:\\repos\\github\\claude-mem\\admin\\docs\\openapi.yaml

STEP 2 — Create the analysis document:
Write D:\\repos\\github\\claude-mem\\admin\\docs\\audit-log-analysis.md with:
- Section: Overview (what audit_log is and who writes it)
- Section: Schema (the PostgreSQL table definition)
- Section: Event Catalogue (table of all known action values, resource_type, details JSONB shape, source file in claude-mem core, and whether implemented/planned)
  Include ALL of these actions from the claude-mem Node.js server:
  api_key.create (source: api-key-service.ts, details: { source? }), api_key.revoke (source: api-key-service.ts, details: {}),
  project.create/project.read/projects.list (source: ServerV1PostgresRoutes.ts, details: {}),
  session.start/session.end/session.read (source: ServerV1PostgresRoutes.ts, details: {}),
  event.write/event.batch_write/event.received/event.batch_received/event.read (source: ServerV1PostgresRoutes.ts, details: various),
  memory.write/memory.read/memory.update/memory.search/memory.context (source: ServerV1PostgresRoutes.ts, details: {}),
  observation.created (source: processGeneratedResponse.ts, details: { generationJobId, sourceType, sourceId, provider, model, sourceAdapter, parsedObservationIndex }),
  generation_job.processing (source: ProviderObservationGenerator.ts, details: { sourceType, sourceId, sourceAdapter, attempt, correlationId, requestId }),
  generation_job.completed (source: processGeneratedResponse.ts, details: { generationJobId, provider, model, observationCount, observationIds, sourceAdapter }),
  generation_job.scope_violation (source: ProviderObservationGenerator.ts, details: { reason, message, payloadTeamId, payloadProjectId, canonicalTeamId, canonicalProjectId, sourceAdapter, correlationId }),
  generation_job.revoked_key (source: ProviderObservationGenerator.ts, details: { reason, message, sourceAdapter, correlationId }),
  generation_job.stalled (source: ActiveServerBetaGenerationWorkerManager.ts, details: { lane, bullmqJobId }),
  generation_job.retried_by_operator (source: ServerV1PostgresRoutes.ts, details: { outcome?, currentAttempts?, previousStatus?, currentStatus?, retriedCount?, requestId? }),
  generation_job.cancelled_by_operator (source: ServerV1PostgresRoutes.ts, details: { outcome?, previousStatus?, currentStatus?, requestId? })
  Also add admin API actions: team.create, project.create (via admin), api_key.create (via admin), api_key.revoke (via admin)
- Section: Actor Semantics (actor_id and api_key_id explained)
- Section: Indexes
- Section: Admin API additions (what this admin feature adds)

STEP 3 — Update schema.sql:
Add these indexes at the bottom of D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\Database\\schema.sql (after the last existing line):
CREATE INDEX IF NOT EXISTS idx_audit_log_created ON audit_log(created_at DESC, id DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_actor ON audit_log(actor_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_api_key ON audit_log(api_key_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_action ON audit_log(action, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_resource_type ON audit_log(resource_type, created_at DESC);

STEP 4 — Update openapi.yaml:
Add to D:\\repos\\github\\claude-mem\\admin\\docs\\openapi.yaml immediately before the "components:" section:

  # ── Audit Log ──────────────────────────────────────────────────────────────
  /audit-log:
    get:
      operationId: GetAuditLog
      summary: List audit log entries (cursor-paginated)
      tags: [AuditLog]
      parameters:
        - name: cursor
          in: query
          schema:
            type: string
        - name: page_size
          in: query
          schema:
            type: integer
            default: 20
            minimum: 1
            maximum: 100
        - name: team_id
          in: query
          schema:
            type: string
        - name: project_id
          in: query
          schema:
            type: string
        - name: api_key_id
          in: query
          schema:
            type: string
        - name: actor_id
          in: query
          schema:
            type: string
        - name: action
          in: query
          schema:
            type: string
        - name: resource_type
          in: query
          schema:
            type: string
        - name: from
          in: query
          schema:
            type: string
            format: date-time
        - name: to
          in: query
          schema:
            type: string
            format: date-time
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/AuditLogPage'
        '401':
          description: Missing or invalid API key
        default:
          $ref: '#/components/responses/ProblemDetailsError'

And add these schemas to components/schemas (after the JobPage schema, before ProblemDetails):

    # ── Audit Log ─────────────────────────────────────────────────────────────
    AuditLogEntry:
      type: object
      properties:
        id:
          type: string
          readOnly: true
        team_id:
          type: string
          nullable: true
          readOnly: true
        team_name:
          type: string
          nullable: true
          readOnly: true
        project_id:
          type: string
          nullable: true
          readOnly: true
        project_name:
          type: string
          nullable: true
          readOnly: true
        actor_id:
          type: string
          nullable: true
          readOnly: true
        api_key_id:
          type: string
          nullable: true
          readOnly: true
        action:
          type: string
          readOnly: true
        resource_type:
          type: string
          readOnly: true
        resource_id:
          type: string
          nullable: true
          readOnly: true
        details:
          type: object
          nullable: true
          readOnly: true
          additionalProperties: {}
          description: Arbitrary JSON payload associated with this audit event.
        created_at:
          type: string
          format: date-time
          readOnly: true

    AuditLogPage:
      allOf:
        - $ref: '#/components/schemas/ResponsePage'
        - type: object
          properties:
            items:
              type: array
              items:
                $ref: '#/components/schemas/AuditLogEntry'
          required: [items]

STEP 5 — Create all C# AuditLog feature files:

Create directory structure: src/ClaudeMem.Admin.Api/Features/AuditLog/Get/

File: src/ClaudeMem.Admin.Api/Features/AuditLog/GetAuditLogFilter.cs
namespace ClaudeMem.Admin.Api.Features.AuditLog;
Following ICursorFilter pattern from GetJobsFilter. Properties: TeamId, ProjectId, ApiKeyId, ActorId, Action, ResourceType, From (DateTimeOffset?), To (DateTimeOffset?), Cursor, PageSize.

File: src/ClaudeMem.Admin.Api/Features/AuditLog/AuditLogWriteData.cs
A record for write-side data: Action, ResourceType, ResourceId (required), TeamId, ProjectId, ActorId (default "admin-api"), ApiKeyId, Details (string, default "{}").

File: src/ClaudeMem.Admin.Api/Features/AuditLog/IAuditLogAccess.cs
Two methods:
  Task<CursorPageResult<AuditLogEntry>> GetAuditLog(GetAuditLogFilter filter, CancellationToken cancellationToken);
  Task WriteEntry(AuditLogWriteData data, CancellationToken cancellationToken);
Note: AuditLogEntry here is from ClaudeMem.Admin.Api.Contracts (the generated contract type, will be created by Step 6 generator run).

File: src/ClaudeMem.Admin.Api/Features/AuditLog/AuditLogAccess.cs
- Constructor: NpgsqlDataSource db, ICursorCodec cursorCodec
- GetAuditLog: Follow JobAccess pattern exactly.
  SQL: SELECT al.id, al.team_id, t.name AS team_name, al.project_id, p.name AS project_name, al.actor_id, al.api_key_id, al.action, al.resource_type, al.resource_id, al.details::text AS details_json, al.created_at FROM audit_log al LEFT JOIN teams t ON t.id = al.team_id LEFT JOIN projects p ON p.id = al.project_id /**where**/ ORDER BY al.created_at DESC, al.id DESC LIMIT @FetchCount
  Private row record: AuditLogRow(string Id, string? TeamId, string? TeamName, string? ProjectId, string? ProjectName, string? ActorId, string? ApiKeyId, string Action, string ResourceType, string? ResourceId, string? DetailsJson, DateTimeOffset CreatedAt)
  Map row to AuditLogEntry contract. For Details: deserialize DetailsJson with System.Text.Json.JsonSerializer.Deserialize<IDictionary<string, object>>(r.DetailsJson) if not null/empty, else null.
  If AuditLogEntry.Details type is not IDictionary<string, object> after running the generator, adjust to match what NSwag actually generated.
  Filters: team_id, project_id, api_key_id, actor_id, action, resource_type, created_at >= From, created_at <= To, cursor clause.
  
- WriteEntry: INSERT INTO audit_log (id, team_id, project_id, actor_id, api_key_id, action, resource_type, resource_id, details, created_at) VALUES (@Id, @TeamId, @ProjectId, @ActorId, @ApiKeyId, @Action, @ResourceType, @ResourceId, @Details::jsonb, NOW())
  Use Guid.NewGuid().ToString() for id. Wrap in try-catch that swallows exceptions (best-effort — audit write must not fail the primary operation).

File: src/ClaudeMem.Admin.Api/Features/AuditLog/Get/GetAuditLogQuery.cs
internal sealed record with: CursorRequest Cursor, string? TeamId, string? ProjectId, string? ApiKeyId, string? ActorId, string? Action, string? ResourceType, DateTimeOffset? From, DateTimeOffset? To
Implements IQuery<Result<CursorPageResult<AuditLogEntry>>>

File: src/ClaudeMem.Admin.Api/Features/AuditLog/Get/GetAuditLogHandler.cs
Follow GetJobsHandler pattern. Map query properties to GetAuditLogFilter, call _auditLogAccess.GetAuditLog, return Result.Ok(page).

File: src/ClaudeMem.Admin.Api/Features/AuditLog/AuditLogController.cs
[ApiController, Route("audit-log")]
public sealed class AuditLogController : ApiControllerBase
Constructor: IMediator mediator, PaginationLinker linker — call base(linker)
[HttpGet] Get method with all 10 query params (cursor, page_size, team_id, project_id, api_key_id, actor_id, action, resource_type, from, to) plus CancellationToken.
Build GetAuditLogQuery with all params, send via mediator, use FromResult to build AuditLogPage response.

STEP 6 — Modify existing CRUD handlers to write audit entries:
Each handler gets IAuditLogAccess injected. After successful primary operation, call WriteEntry in a try-catch (swallow exceptions).

Modify CreateTeamHandler:
- Add IAuditLogAccess _auditLog constructor param
- After createdTeam = await _teamAccess.CreateTeam(...): write audit entry with Action="team.create", ResourceType="team", ResourceId=createdTeam.Id, TeamId=createdTeam.Id

Modify CreateProjectHandler:
- Add IAuditLogAccess _auditLog constructor param  
- After createdProject = await _projectAccess.CreateProject(...): write audit entry with Action="project.create", ResourceType="project", ResourceId=createdProject.Id, TeamId=command.TeamId, ProjectId=createdProject.Id

Modify CreateApiKeyHandler:
- Add IAuditLogAccess _auditLog constructor param
- After insertedApiKey = await _apiKeyAccess.InsertApiKey(...): write audit entry with Action="api_key.create", ResourceType="api_key", ResourceId=insertedApiKey.Id, TeamId=command.TeamId, ProjectId=command.ProjectId, ActorId=command.ActorId, ApiKeyId=insertedApiKey.Id
  Note: for ActorId we use command.ActorId (the creator's id) rather than "admin-api" for api_key entries since the key owner is meaningful

Modify DeleteApiKeyHandler:
- Add IAuditLogAccess _auditLog constructor param
- After RevokeApiKeyById succeeds (after the guard checks pass): write audit entry with Action="api_key.revoke", ResourceType="api_key", ResourceId=command.KeyId, TeamId=command.TeamId, ProjectId=command.ProjectId, ApiKeyId=command.KeyId
  Check what properties DeleteApiKeyCommand has (read it first).

STEP 7 — Register in Program.cs:
Add this line to AddAccessLayerServices method in src/ClaudeMem.Admin.Api/Program.cs:
services.AddScoped<Features.AuditLog.IAuditLogAccess, Features.AuditLog.AuditLogAccess>();

STEP 8 — Run the client generator:
Run: dotnet run --project src/ClaudeMem.Admin.Api.Contracts.Generator
from D:\\repos\\github\\claude-mem\\admin

STEP 9 — Verify the generated output:
Read src/ClaudeMem.Admin.Api.Contracts/Client/AdminApiClient.generated.cs
Check: Does IAuditLogClient exist? What is the exact signature of AuditLogAsync? What type did NSwag generate for AuditLogEntry.Details?
If Details is not IDictionary<string, object>, update AuditLogAccess.cs mapping accordingly to match the generated type.
Also check what the AuditLogEntry constructor looks like (positional params) so the mapping is correct.

IMPORTANT: Read all files before writing. Match the existing patterns exactly. Do not add features beyond what is specified.
`, { label: 'CodeWriter: API + schema + openapi', phase: 'Implement API' })

// ── Phase 2: Build (fix compilation errors) ─────────────────────────────────
phase('Generate + Build')

await agent(`
Build the ClaudeMem Admin solution and fix ALL compilation errors and warnings.

Working directory: D:\\repos\\github\\claude-mem\\admin

Run: dotnet build ClaudeMem.Admin.slnx

Read each error message carefully. Find the source file mentioned in the error. Fix the specific issue. Re-run the build. Repeat until zero errors and zero warnings.

Common issues to watch for:
- Missing using directives
- Namespace mismatches
- Type mismatches (especially for AuditLogEntry.Details — check what NSwag actually generated)
- Positional record constructor param order mismatches
- CursorPageResult<AuditLogEntry> vs CursorPageResult<SomeOtherType>
- Missing interface members
- IAuditLogAccess not fully implemented

After fixing all issues, report: what was fixed, final build status.
`, { label: 'Build: fix all errors', phase: 'Generate + Build' })

// ── Phase 3: Review loop (max 3 rounds) ─────────────────────────────────────
phase('Review + Fix')

const FINDINGS_SCHEMA = {
  type: 'object',
  required: ['passed', 'findings'],
  properties: {
    passed: { type: 'boolean' },
    findings: {
      type: 'array',
      items: {
        type: 'object',
        required: ['file', 'issue', 'fix'],
        properties: {
          file: { type: 'string' },
          issue: { type: 'string' },
          fix: { type: 'string' }
        }
      }
    }
  }
}

let reviewPassed = false
let reviewRound = 0

while (!reviewPassed && reviewRound < 3) {
  reviewRound++

  const review = await agent(`
You are a strict C# code reviewer. Review ALL new and modified files for the Audit Log feature.

Read EVERY file listed below in full before producing any findings:

New files:
- D:\\repos\\github\\claude-mem\\admin\\docs\\audit-log-analysis.md
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\AuditLog\\GetAuditLogFilter.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\AuditLog\\AuditLogWriteData.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\AuditLog\\IAuditLogAccess.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\AuditLog\\AuditLogAccess.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\AuditLog\\Get\\GetAuditLogQuery.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\AuditLog\\Get\\GetAuditLogHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\AuditLog\\AuditLogController.cs

Modified files:
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Infrastructure\\Database\\schema.sql
- D:\\repos\\github\\claude-mem\\admin\\docs\\openapi.yaml
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Program.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Teams\\Create\\CreateTeamHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Projects\\Create\\CreateProjectHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\ApiKeys\\Create\\CreateApiKeyHandler.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\ApiKeys\\Delete\\DeleteApiKeyHandler.cs

Also read these reference files to compare patterns:
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Jobs\\JobAccess.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Jobs\\JobsController.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api\\Features\\Jobs\\Get\\GetJobsHandler.cs

Review checklist (find issues in ALL of these, not just some):
1. C# style: full {} braces on ALL if/else/try/catch blocks — no single-line statements without braces
2. Access layer methods return Task<T> not ValueTask<T>
3. Mediator handlers return ValueTask<Result<T>> (correct, library contract)
4. Naming: internal sealed for all feature types, public sealed for controllers
5. No inline field initializers (NO "private readonly X _x = new();" in class body)
6. Cursor pagination: ICursorFilter implemented on filter record, FetchCount = PageSize+1 sentinel, TrimAndGetNextCursor used correctly
7. SQL: al. prefix on all audit_log columns to avoid ambiguity with JOIN columns, LEFT JOIN for teams and projects
8. All 8 filters correctly wired in AuditLogAccess: TeamId, ProjectId, ApiKeyId, ActorId, Action, ResourceType, From (>=), To (<=)
9. Cursor clause: (al.created_at, al.id) < (@CursorCreatedAt, @CursorId) using @CursorCreatedAt not @CreatedAt
10. AuditLogAccess.WriteEntry: wrapped in try-catch that swallows the exception (audit must not fail primary operation)
11. WriteEntry inserts: Guid.NewGuid().ToString() for id, @Details::jsonb cast in SQL
12. CRUD handlers: all 4 have WriteEntry calls after successful primary operation
13. DeleteApiKeyHandler: WriteEntry only called when operation actually succeeds (after revoke)
14. CreateApiKeyHandler: ActorId for audit entry uses command.ActorId (not "admin-api")
15. Program.cs: IAuditLogAccess registered
16. openapi.yaml: AuditLogEntry has all 12 properties, AuditLogPage extends ResponsePage
17. Controller: all 10 query params bound correctly, from/to as DateTimeOffset?

Return structured JSON with passed=true only if NO issues found. If there are issues, list each one with file path, description of issue, and specific fix.
`, { label: 'Reviewer round ' + reviewRound, phase: 'Review + Fix', schema: FINDINGS_SCHEMA })

  if (!review || review.passed || review.findings.length === 0) {
    reviewPassed = true
    log('Review passed — proceeding to UI and tests')
  } else {
    log('Review round ' + reviewRound + ': ' + review.findings.length + ' findings')
    await agent(`
Fix the following code review findings. Read each file before editing it.

Findings to fix:
${JSON.stringify(review.findings, null, 2)}

After applying all fixes, run:
dotnet build D:\\repos\\github\\claude-mem\\admin\\ClaudeMem.Admin.slnx
to verify no compilation errors were introduced.
`, { label: 'Fix round ' + reviewRound, phase: 'Review + Fix' })
  }
}

// ── Phase 4: UI + API tests (parallel where file sets don't conflict) ────────
phase('Implement UI + API Tests')

await parallel([
  () => agent(`
You are implementing the Audit Log UI page for the ClaudeMem Admin Blazor application.

Read these files FIRST before writing anything:
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Ui\\Components\\Pages\\Jobs.razor (primary pattern to follow)
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Ui\\Components\\Layout\\NavMenu.razor
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api.Contracts\\Client\\AdminApiClient.generated.cs (find IAuditLogClient and AuditLogEntry, AuditLogPage types)
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Ui\\Components\\Shared\\LoadStateView.razor
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Ui\\_Imports.razor
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Ui\\Components\\Pages\\Teams.razor

TASK 1 — Create D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Ui\\Components\\Pages\\AuditLog.razor

@page "/audit-log"
@inject IAuditLogClient AuditLogClient
@inject ISnackbar Snackbar

Route: /audit-log
PageTitle: Audit Log — ClaudeMem Admin

Layout:
1. Header row: MudText h4 "Audit Log" + MudSpacer + filter controls
2. Filter bar: MudStack Row=true Wrap=true with these dense text inputs in MudItems:
   - Team ID (MudTextField bound to _teamId, Label="Team ID")
   - Project ID (MudTextField bound to _projectId, Label="Project ID")
   - Actor ID (MudTextField bound to _actorId, Label="Actor ID")
   - API Key ID (MudTextField bound to _apiKeyId, Label="API Key ID")
   - Action (MudTextField bound to _action, Label="Action")
   - Resource Type (MudTextField bound to _resourceType, Label="Resource Type")
   - From (MudDatePicker bound to _from, Label="From", DateFormat="yyyy-MM-dd")
   - To (MudDatePicker bound to _to, Label="To", DateFormat="yyyy-MM-dd")
   - MudButton "Search" Variant=Text OnClick="Reload"
   - MudButton "Clear" Variant=Text OnClick="ClearFilters" (resets all filter fields and calls Reload)
3. LoadStateView wrapping MudTable:
   - MudTable Items="page.Items" Hover=true Dense=true Breakpoint=Sm OnRowClick="OnRowClick" T="AuditLogEntry" RowClass="cursor-pointer"
   - Columns: Created At | Team | Project | Actor | Action | Resource Type | Resource ID
   - Created: entry.CreatedAt?.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")
   - Team: entry.TeamName ?? entry.TeamId ?? "—"
   - Project: entry.ProjectName ?? entry.ProjectId ?? "—"
   - Actor: entry.ActorId ?? "—"
   - Action: MudChip T=string Size=Small Color=ActionColor(entry.Action) Text=entry.Action
   - Resource Type: entry.ResourceType
   - Resource ID: MudText Typo=caption entry.ResourceId ?? "—"
   - NoRecordsContent: "No audit log entries match the current filter."
4. Load more button: if page.Next is not null, show MudButton "Load more" OnClick="LoadMore"
5. Detail panel (below table): if _selectedEntry is not null, show MudPaper Elevation=0 Class="mt-4 pa-4" Style="border: 1px solid var(--mud-palette-lines-default)":
   - MudText Typo=h6 "Entry Details"
   - MudGrid with 2-col layout showing: ID, Team, Project, Actor, API Key ID, Action, Resource Type, Resource ID, Created At
   - Below the grid: MudText Typo=subtitle2 "Details (JSON):" + <pre style="overflow-x:auto;background:var(--mud-palette-background-grey);padding:12px;border-radius:4px;font-size:0.8rem">@GetFormattedDetails(_selectedEntry)</pre>

@code section:
- [CascadingParameter] private ProcessError? ProcessError { get; set; }
- private AuditLogPage? _page;
- private bool _loading = true;
- private AuditLogEntry? _selectedEntry;
- private string _teamId = string.Empty;
- private string _projectId = string.Empty;
- private string _actorId = string.Empty;
- private string _apiKeyId = string.Empty;
- private string _action = string.Empty;
- private string _resourceType = string.Empty;
- private DateTimeOffset? _from;
- private DateTimeOffset? _to;

- OnInitializedAsync: calls Reload()
- Reload(): sets _loading=true, _selectedEntry=null, calls AuditLogClient.AuditLogAsync with all filter params (null if empty string), assigns _page, sets _loading=false. Wrap in try-catch → ProcessError?.HandleError(ex), _page=null.
  IMPORTANT: Check the actual method name from IAuditLogClient after reading AdminApiClient.generated.cs — it may be AuditLogAsync or AuditLogGetAsync depending on NSwag output.
- LoadMore(): follows Jobs.razor pattern — extract cursor from _page.Next, call AuditLogClient with cursor + same filters, accumulate items, replace page object preserving _page.First.
- OnRowClick(TableRowClickEventArgs<AuditLogEntry> args): toggle selection — if _selectedEntry?.Id == args.Item?.Id set to null, else set to args.Item
- ClearFilters(): reset all filter fields to empty/null, call Reload()
- ActionColor(string? action): return Color based on action prefix — "create" → Success, "revoke" → Warning, "scope_violation"/"stalled"/"revoked_key" → Error, else Default
- GetFormattedDetails(AuditLogEntry entry): serialize entry.Details to indented JSON using System.Text.Json.JsonSerializer.Serialize(entry.Details, new JsonSerializerOptions { WriteIndented = true }) — return "—" if null/empty

Using directives needed: System, System.Collections.Generic, System.Linq, System.Text.Json, ClaudeMem.Admin.Api.Contracts, ClaudeMem.Admin.Ui.Helpers (for CursorHelper), MudBlazor

TASK 2 — Update NavMenu.razor:
Add before the MudDivider:
<MudNavLink Href="/audit-log" Icon="@Icons.Material.Filled.Policy">
    Audit Log
</MudNavLink>
`, { label: 'CodeWriter: UI page + nav', phase: 'Implement UI + API Tests' }),

  () => agent(`
You are writing integration tests for the Audit Log API feature in the ClaudeMem Admin project.

Read these files FIRST:
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Features\\Jobs\\JobsTests.cs (primary pattern)
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Infrastructure\\AdminApiFixture.cs
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Infrastructure\\DbHelper.cs
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Infrastructure\\ResponsePageAssertions.cs
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Infrastructure\\AdminApiCollection.cs
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Assembly.cs
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Features\\Teams\\TeamsTests.cs
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Features\\Projects\\ProjectsTests.cs
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Features\\ApiKeys\\ApiKeysTests.cs (if it exists)
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api.Contracts\\Client\\AdminApiClient.generated.cs (find IAuditLogClient, AuditLogEntry, AuditLogPage)

TASK 1 — Add to DbHelper.cs:

public async Task<string> InsertAuditLogEntry(
    string? teamId = null,
    string? projectId = null,
    string? actorId = "test-actor",
    string? apiKeyId = null,
    string action = "test.action",
    string resourceType = "test",
    string? resourceId = null,
    string details = "{}")
{
    var id = Guid.NewGuid().ToString();
    await using var connection = CreateConnection();
    await connection.ExecuteAsync(
        "INSERT INTO audit_log (id, team_id, project_id, actor_id, api_key_id, action, resource_type, resource_id, details, created_at) VALUES (@Id, @TeamId, @ProjectId, @ActorId, @ApiKeyId, @Action, @ResourceType, @ResourceId, @Details::jsonb, NOW())",
        new { Id = id, TeamId = teamId, ProjectId = projectId, ActorId = actorId, ApiKeyId = apiKeyId, Action = action, ResourceType = resourceType, ResourceId = resourceId, Details = details });
    return id;
}

public async Task<AuditLogDbRow?> ReadLastAuditLogEntry(string resourceType, string action)
{
    await using var connection = CreateConnection();
    return await connection.QuerySingleOrDefaultAsync<AuditLogDbRow>(
        "SELECT id, team_id, project_id, actor_id, api_key_id, action, resource_type, resource_id FROM audit_log WHERE resource_type = @ResourceType AND action = @Action ORDER BY created_at DESC LIMIT 1",
        new { ResourceType = resourceType, Action = action });
}

Add this record either in DbHelper.cs or a separate file DbHelper.AuditLogDbRow.cs within the Infrastructure namespace:
internal sealed record AuditLogDbRow(string Id, string? TeamId, string? ProjectId, string? ActorId, string? ApiKeyId, string Action, string ResourceType, string? ResourceId);

TASK 2 — Create D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Api.Tests\\Features\\AuditLog\\AuditLogTests.cs

Follow EXACTLY the same structure as JobsTests.cs:
- [Collection<AdminApiCollection>]
- public sealed class AuditLogTests : IAsyncLifetime
- Constructor takes AdminApiFixture, creates DbHelper, gets IAuditLogClient from fixture
  Check: does AdminApiFixture have an AuditLogClient property? If not, add it following the pattern of fixture.JobsClient. Check the fixture class.
- InitializeAsync: ResetDatabaseAsync
- DisposeAsync: ValueTask.CompletedTask
- private const string EndpointAuditLog = "/audit-log";

Test cases (follow JobsTests.cs style — use AwesomeAssertions, AssertionScope for multi-step):
1. GetAuditLog_EmptyDatabase_ReturnsEmptyPage — call AuditLogAsync, assert ShouldBeEmptyPage
2. GetAuditLog_WithEntries_ReturnsAll — seed 2 entries, assert ShouldHaveItems(2)
3. GetAuditLog_FilterByTeamId — seed 2 entries with different teamIds, filter by one, expect 1 result
   IMPORTANT: If seeding with teamId, use _db.InsertTeam() to create the team first, then InsertAuditLogEntry with that teamId (FK constraint)
4. GetAuditLog_FilterByProjectId — seed with different projectIds, filter, expect 1
   Seed with _db.InsertTeam() → InsertProject(teamId) → InsertAuditLogEntry(teamId, projectId)
5. GetAuditLog_FilterByApiKeyId — seed 2 entries one with apiKeyId, filter by it
6. GetAuditLog_FilterByActorId — seed 2 entries with different actorIds, filter
7. GetAuditLog_FilterByAction — seed "api_key.create" and "api_key.revoke", filter by "api_key.create", expect 1
8. GetAuditLog_FilterByResourceType — seed "api_key" and "team" resource types, filter by "team"
9. GetAuditLog_FilterByDateRange — this is tricky with DB-generated timestamps; seed 1 entry, filter with from=DateTimeOffset.UtcNow.AddDays(-1) and to=DateTimeOffset.UtcNow.AddDays(1), expect 1 result
10. GetAuditLog_MultiPagePagination_LinksCorrect — seed 10 entries, pageSize=3, validate 4 pages
11. GetAuditLog_WithoutApiKey_Returns401 — use _fixture.GetUnauthenticated
12. GetAuditLog_WithWrongApiKey_Returns401 — use _fixture.GetWithWrongKey
13. GetAuditLog_ReturnsJoinedTeamAndProjectNames — seed team, project, entry; verify team_name and project_name populated
14. GetAuditLog_WhenTeamDeleted_EntryStillReturnedWithNullTeamName — this test may be skipped if it requires complex DB manipulation; add a simple version or note it's a future test

TASK 3 — Add write-side tests to existing test files:

Add to TeamsTests.cs (read it first to find the right place):
[Fact]
public async Task CreateTeam_ProducesAuditLogEntry()
{
    var response = await _teams.TeamsPostAsync(new Team(null, "Audit Test Team", null, null), TestContext.Current.CancellationToken);
    var auditEntry = await _db.ReadLastAuditLogEntry("team", "team.create");
    using (new AssertionScope())
    {
        auditEntry.Should().NotBeNull();
        auditEntry!.Action.Should().Be("team.create");
        auditEntry.ResourceType.Should().Be("team");
        auditEntry.ResourceId.Should().Be(response.Id);
    }
}

Add to ProjectsTests.cs (read it first):
[Fact]
public async Task CreateProject_ProducesAuditLogEntry()
{
    var teamId = await _db.InsertTeam();
    var response = await _projects.ProjectsPostAsync(new Project(null, null, "Audit Test Project", null, null, null), teamId, TestContext.Current.CancellationToken);
    var auditEntry = await _db.ReadLastAuditLogEntry("project", "project.create");
    using (new AssertionScope())
    {
        auditEntry.Should().NotBeNull();
        auditEntry!.Action.Should().Be("project.create");
        auditEntry.ResourceType.Should().Be("project");
        auditEntry.ResourceId.Should().Be(response.Id);
        auditEntry.TeamId.Should().Be(teamId);
    }
}

Add to the ApiKeys test file (check its name and read it first):
[Fact]
public async Task CreateApiKey_ProducesAuditLogEntry()
{
    // seed team+project, create api key, verify audit entry
}
[Fact]
public async Task RevokeApiKey_ProducesAuditLogEntry()
{
    // seed team+project+api_key, revoke it, verify audit entry with action=api_key.revoke
}

IMPORTANT: Check if AdminApiFixture has an AuditLogClient property. If not, add it — look at how JobsClient is defined there and follow the same pattern.

Check what the actual method name is on IAuditLogClient for listing audit entries (may be AuditLogAsync or GetAuditLogAsync depending on NSwag output).

After writing all files, report what was written.
`, { label: 'Tester: API tests + DbHelper', phase: 'Implement UI + API Tests' })
])

// UI tests run after UI is written (sequential — needs the Razor component to exist)
await agent(`
You are writing bUnit UI tests for the Audit Log page in the ClaudeMem Admin Blazor application.

Read these files FIRST:
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Ui.Tests\\Pages\\TeamsPageTests.cs (primary pattern)
- D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Ui.Tests\\Infrastructure\\UiTestContext.cs
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Ui\\Components\\Pages\\AuditLog.razor (just written — read it)
- D:\\repos\\github\\claude-mem\\admin\\src\\ClaudeMem.Admin.Api.Contracts\\Client\\AdminApiClient.generated.cs (find IAuditLogClient, AuditLogEntry, AuditLogPage)

TASK 1 — Add SetupAuditLogClient to UiTestContext.cs:
Following the exact same pattern as SetupJobsClient, add:
protected IAuditLogClient SetupAuditLogClient()
{
    var client = Substitute.For<IAuditLogClient>();
    Services.AddSingleton(client);
    return client;
}

TASK 2 — Create D:\\repos\\github\\claude-mem\\admin\\tests\\ClaudeMem.Admin.Ui.Tests\\Pages\\AuditLogPageTests.cs

Follow TeamsPageTests.cs EXACTLY for structure and style.
Use IAuditLogClient mock from SetupAuditLogClient().

Check the actual method name on IAuditLogClient for listing (may be AuditLogAsync or similar).
Check the AuditLogPage constructor signature from the generated code.
Check the AuditLogEntry constructor signature.

Helper methods:
private static AuditLogPage EmptyAuditLogPage() — returns empty page
private static AuditLogPage AuditLogPageWithItems(IEnumerable<AuditLogEntry> items, Uri? next = null)
private static AuditLogEntry MakeEntry(string id = "e1", string action = "test.action", string resourceType = "test") — creates minimal entry

Test cases:
1. Api_Returns_Error_Shows_Error_Alert — client throws ApiException, render AuditLog, check MudAlert severity Error
2. Api_Returns_Empty_List_Shows_No_Entries_Message — empty page, check markup contains "No audit log entries"
3. Api_Returns_Entries_Shows_Action_Column — page with 1 entry, check markup contains the action value
4. Api_Returns_Next_Shows_LoadMore_Button — page with Next uri, check markup contains "Load more"
5. Api_Returns_No_Next_Hides_LoadMore_Button — page without Next, check markup does NOT contain "Load more"
6. ClickRow_Shows_Detail_Panel — page with 1 entry, render, simulate click on first row, check detail panel appears (check markup contains "Entry Details")
7. ClickSameRow_Again_Hides_Detail_Panel — click same row twice, detail panel disappears
`, { label: 'Tester: UI tests', phase: 'Implement UI + API Tests' })

// ── Phase 5: Final build + test run ─────────────────────────────────────────
phase('Final Verify')

await agent(`
Run the complete build and test suite for the ClaudeMem Admin solution.

Working directory: D:\\repos\\github\\claude-mem\\admin

Step 1: Run dotnet build ClaudeMem.Admin.slnx
If there are errors, fix them (read the failing files, apply fixes) and re-run.

Step 2: Run dotnet test ClaudeMem.Admin.slnx --verbosity normal
If tests fail, read the failing test output carefully, find the root cause, fix the source code (NOT the test unless the test itself is wrong), re-run tests.

Keep fixing until:
- Zero build errors
- Zero build warnings
- All tests pass

Report final state: build status, total tests, passed, failed.
`, { label: 'Final: build + test run', phase: 'Final Verify' })

log('Audit Log feature implementation complete')
