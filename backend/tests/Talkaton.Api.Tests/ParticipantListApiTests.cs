using System.Net;
using System.Net.Http.Json;
using Talkaton.Api.Common;
using Talkaton.Api.ParticipantLists;

namespace Talkaton.Api.Tests;

public class ParticipantListApiTests(TalkatonApiFactory factory) : IClassFixture<TalkatonApiFactory>
{
    [Fact]
    public async Task Список_создаётся_с_выбранными_участниками_и_сохраняется()
    {
        var client = await factory.SignInAsync($"Владелец списка {Guid.NewGuid():N}");
        var people = await client.GetFromJsonAsync<List<UserDto>>("/api/users");
        var selected = people!.Take(2).Select(x => x.Id).ToArray();

        var response = await client.PostAsJsonAsync(
            "/api/participant-lists",
            new CreateParticipantListRequest("  Дизайн-ревью  ", selected));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ParticipantListDto>();
        Assert.Equal("Дизайн-ревью", created!.Name);
        Assert.Equal(selected.Order(), created.Members.Select(x => x.Id).Order());

        var saved = await client.GetFromJsonAsync<List<ParticipantListDto>>("/api/participant-lists");
        Assert.Contains(saved!, x => x.Id == created.Id && x.Members.Count == selected.Length);
    }

    [Fact]
    public async Task Слишком_длинное_название_списка_отклоняется()
    {
        var client = await factory.SignInAsync($"Длинное имя списка {Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/api/participant-lists",
            new CreateParticipantListRequest(new string('я', 201), []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
