using System.Text.Json;

namespace MainCore.Commands.Misc
{
    /// <summary>
    /// Moves a repeatedly-failing build job to the bottom of the queue so it stops blocking the rest.
    /// Matches the first NormalBuild job at the given location+type and re-numbers positions.
    /// </summary>
    [Handler]
    public static partial class DeprioritizeBuildJobCommand
    {
        public sealed record Command(VillageId VillageId, int Location, BuildingEnums Type) : IVillageCommand;

        private static async ValueTask HandleAsync(
            Command command,
            AppDbContext context
            )
        {
            await Task.CompletedTask;
            var (villageId, location, type) = command;

            var jobs = context.Jobs
                .Where(x => x.VillageId == villageId.Value)
                .OrderBy(x => x.Position)
                .ToList();
            if (jobs.Count <= 1) return;

            var target = jobs.FirstOrDefault(x => x.Type == JobTypeEnums.NormalBuild && Matches(x.Content, location, type));
            if (target is null) return;
            if (jobs[^1].Id == target.Id) return; // already last

            jobs.Remove(target);
            jobs.Add(target);
            for (var i = 0; i < jobs.Count; i++) jobs[i].Position = i;
            context.SaveChanges();
        }

        private static bool Matches(string content, int location, BuildingEnums type)
        {
            try
            {
                var plan = JsonSerializer.Deserialize<NormalBuildPlan>(content);
                return plan is not null && plan.Location == location && plan.Type == type;
            }
            catch
            {
                return false;
            }
        }
    }
}
