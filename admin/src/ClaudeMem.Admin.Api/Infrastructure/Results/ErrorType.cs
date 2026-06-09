namespace ClaudeMem.Admin.Api.Infrastructure.Results;

/// <summary>
/// Classifies the nature of a failure, enabling generic infrastructure — HTTP response mappers,
/// retry handlers — to act on error categories without inspecting individual error codes.
/// </summary>
/// <remarks>
/// Values are assigned explicit integers with gaps of 100 between categories so that new types
/// can be inserted in a logical position in the future without renumbering existing members.
/// <see cref="Unexpected"/> is <c>0</c> because it is the C# default for uninitialized enum
/// fields, making it a safe sentinel: an error that was never explicitly classified still maps
/// to a generic server error rather than a misleading specific type.
/// </remarks>
public enum ErrorType
{
    /// <summary>
    /// An unexpected or unclassified error occurred.
    /// <para>
    /// Use as a catch-all for failures that do not fit a more specific category — for example, an unhandled exception or an inconsistent internal state.
    /// Maps to HTTP <c>500 Internal Server Error</c>.
    /// </para>
    /// <para>
    /// Not retryable by default — the root cause is unknown and retrying may trigger unintended side effects.
    /// Prefer assigning a more specific <see cref="ErrorType"/> whenever possible.
    /// </para>
    /// </summary>
    Unexpected = 0,

    /// <summary>
    /// The requested resource does not exist.
    /// <para>
    /// Use when an entity lookup by identifier returns no result (e.g. a team, project, or job that cannot be found in the data store).
    /// Maps to HTTP <c>404 Not Found</c>.
    /// </para>
    /// <para>
    /// Never retryable — the resource will not materialise between retries.
    /// The caller must verify the identifier or accept that the resource is gone.
    /// </para>
    /// </summary>
    NotFound = 100,

    /// <summary>
    /// The operation conflicts with the current state of the system.
    /// <para>
    /// Use for ownership mismatches (e.g. a project that does not belong to the requested team), state-machine violations (e.g. retrying a job that is not in a failed state),
    /// or concurrent modification conflicts. Maps to HTTP <c>409 Conflict</c>.
    /// </para>
    /// <para>
    /// Never retryable — the underlying state must be resolved by the caller before the operation can succeed.
    /// Retrying without changing the state will produce the same failure.
    /// </para>
    /// </summary>
    Conflict = 200,

    /// <summary>
    /// The input or a business rule is invalid.
    /// <para>
    /// Use for failed field validation, violated constraints, or requests that are structurally well-formed but semantically incorrect.
    /// Maps to HTTP <c>422 Unprocessable Entity</c> (or <c>400 Bad Request</c> for low-level format errors).
    /// </para>
    /// <para>
    /// Never retryable — the caller must correct the input before issuing a new request.
    /// Retrying the same payload will always fail.
    /// </para>
    /// </summary>
    Validation = 300,

    /// <summary>
    /// The caller is authenticated but lacks permission to perform the operation.
    /// <para>
    /// Use when an identity is known but authorisation checks fail (e.g. a user attempting to access a resource owned by another tenant).
    /// Maps to HTTP <c>403 Forbidden</c>.
    /// </para>
    /// <para>
    /// Never retryable — permissions must be granted through an out-of-band process.
    /// Retrying the same request will produce the same denial.
    /// </para>
    /// </summary>
    Forbidden = 400,

    /// <summary>
    /// The failure is transient and the operation may succeed if retried after a short delay.
    /// <para>
    /// Use for downstream HTTP timeouts, temporary service unavailability, or recoverable infrastructure faults where the root cause is expected to resolve on its own.
    /// Maps to HTTP <c>503 Service Unavailable</c> or <c>504 Gateway Timeout</c>.
    /// </para>
    /// <para>
    /// Always retryable — retry handlers should apply an exponential back-off strategy.
    /// Only use this type when the failure is genuinely transient; do not use it to mask persistent errors that happen to be intermittent.
    /// </para>
    /// </summary>
    Transient = 500,
}
