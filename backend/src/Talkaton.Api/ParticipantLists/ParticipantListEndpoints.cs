using Microsoft.EntityFrameworkCore;
using Talkaton.Api.Common;
using Talkaton.Domain.Entities;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.ParticipantLists;

public record ParticipantListDto(Guid Id, string Name, int SortOrder, IReadOnlyList<UserDto> Members);

public record CreateParticipantListRequest(string Name, Guid[]? MemberIds);

public static class ParticipantListEndpoints
{
    private const int MaxNameLength = 200;
    private const int MaxMembers = 100;

    public static IEndpointRouteBuilder MapParticipantListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/participant-lists")
            .AddEndpointFilter<RequireUserFilter>()
            .WithTags("ParticipantLists");

        group.MapGet("/", async (CurrentUser currentUser, TalkatonDbContext db, CancellationToken ct) =>
            {
                var lists = await db.ParticipantLists
                    .Where(x => x.OwnerId == currentUser.Required.Id)
                    .Include(x => x.Members)
                    .ThenInclude(x => x.User)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync(ct);

                return Results.Ok(lists.Select(Map).ToList());
            })
            .WithName("GetParticipantLists")
            .WithSummary("Блок «Списки участников» левой панели")
            .Produces<IReadOnlyList<ParticipantListDto>>();

        group.MapPost("/", async (
                CreateParticipantListRequest request,
                CurrentUser currentUser,
                TalkatonDbContext db,
                CancellationToken ct) =>
            {
                var name = request.Name?.Trim() ?? string.Empty;
                if (name.Length == 0 || name.Length > MaxNameLength)
                {
                    return Results.Problem(
                        title: $"Название списка должно содержать от 1 до {MaxNameLength} символов",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var owner = currentUser.Required;
                var memberIds = (request.MemberIds ?? []).Distinct().ToArray();
                if (memberIds.Length > MaxMembers)
                {
                    return Results.Problem(
                        title: $"В списке может быть не больше {MaxMembers} участников",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var lastOrder = await db.ParticipantLists
                    .Where(x => x.OwnerId == owner.Id)
                    .MaxAsync(x => (int?)x.SortOrder, ct) ?? -1;

                var list = new ParticipantList
                {
                    Id = Guid.NewGuid(),
                    OwnerId = owner.Id,
                    Name = name,
                    SortOrder = lastOrder + 1,
                };

                // Один параметризованный запрос не даёт подложить несуществующий ID и не создаёт N+1.
                var validMemberIds = await db.Users
                    .Where(x => x.Id != owner.Id && memberIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync(ct);

                foreach (var memberId in validMemberIds)
                {
                    list.Members.Add(new ParticipantListMember { ListId = list.Id, UserId = memberId });
                }

                db.ParticipantLists.Add(list);
                await db.SaveChangesAsync(ct);

                await db.Entry(list).Collection(x => x.Members).Query().Include(x => x.User).LoadAsync(ct);
                return Results.Created($"/api/participant-lists/{list.Id}", Map(list));
            })
            .WithName("CreateParticipantList")
            .WithSummary("Кнопка «+» рядом со списками участников")
            .Produces<ParticipantListDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static ParticipantListDto Map(ParticipantList list) => new(
        list.Id,
        list.Name,
        list.SortOrder,
        list.Members
            .Where(x => x.User is not null)
            .Select(x => UserDto.From(x.User!))
            .OrderBy(x => x.DisplayName, StringComparer.Ordinal)
            .ToList());
}
