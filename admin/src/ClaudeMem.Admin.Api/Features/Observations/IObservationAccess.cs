using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Observations;

/// <summary>Database access contract for observation operations.</summary>
internal interface IObservationAccess
{
    /// <summary>Returns a cursor-paged list of observations matching the filter.</summary>
    Task<CursorPageResult<Observation>> GetObservations(GetObservationsFilter filter, CancellationToken cancellationToken);
}
