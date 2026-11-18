using Analytics.Api.Persistence;
using BuildingBlocks.CoachAccess;

namespace Analytics.Api.CoachLinks;

/// <summary>
/// Az Identity kapcsolat-eseményeiből karbantartja az edzői hozzáférés helyi tábláját.
/// </summary>
public class CoachRunnerLinkConsumer(AnalyticsDbContext db) : CoachRunnerLinkConsumerBase<AnalyticsDbContext>(db);
