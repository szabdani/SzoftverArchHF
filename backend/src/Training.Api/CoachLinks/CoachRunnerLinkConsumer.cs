using BuildingBlocks.CoachAccess;
using Training.Api.Persistence;

namespace Training.Api.CoachLinks;

public class CoachRunnerLinkConsumer(TrainingDbContext db) : CoachRunnerLinkConsumerBase<TrainingDbContext>(db);
