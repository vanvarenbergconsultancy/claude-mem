using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Observations;

internal interface IObservationAccess
{
    Task<CursorPageResult<Observation>> GetObservations(GetObservationsFilter filter, CancellationToken cancellationToken);
}
