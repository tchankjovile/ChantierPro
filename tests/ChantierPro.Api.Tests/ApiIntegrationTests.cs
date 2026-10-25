using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChantierPro.Api.Dtos;
using ChantierPro.Core.Models;
using Xunit;

namespace ChantierPro.Api.Tests;

public class ApiIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> AuthenticateAsync(string email = "admin@chantierpro.cm", string password = "Admin#2026")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth.AccessToken;
    }

    [Fact]
    public async Task Login_WithValidAdmin_ReturnsTokenAndUserInfo()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@chantierpro.cm", "Admin#2026"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal("admin@chantierpro.cm", auth.User.Email);
        Assert.Equal(UserRole.Administrateur, auth.User.Role);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@chantierpro.cm", "WrongPassword"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetChantiers_Unauthorized_Returns401()
    {
        var unauthenticatedClient = _client;
        var response = await unauthenticatedClient.GetAsync("/api/chantiers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetChantiers_Authenticated_ReturnsSeededChantiers()
    {
        var token = await AuthenticateAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/chantiers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(list);
        Assert.True(list.Count >= 3, "Les chantiers de démo doivent être présents.");
    }

    [Fact]
    public async Task Stock_PreventsNegativeStock_UnlessAuthorized_RG03()
    {
        var token = await AuthenticateAsync();

        // 1. Récupérer un chantier et un matériau
        using var reqChantier = new HttpRequestMessage(HttpMethod.Get, "/api/chantiers");
        reqChantier.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resChantier = await _client.SendAsync(reqChantier);
        var chantiers = await resChantier.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        var chantierId = chantiers![0].GetProperty("id").GetGuid();

        using var reqMat = new HttpRequestMessage(HttpMethod.Get, "/api/stock/materials");
        reqMat.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resMat = await _client.SendAsync(reqMat);
        var materials = await resMat.Content.ReadFromJsonAsync<List<Material>>();
        var material = materials![0];

        // 2. Tenter une sortie démesurée (ex: 999 999 unités) sans autorisation -> Doit refuser (409 Conflict)
        using var reqExcess = new HttpRequestMessage(HttpMethod.Post, "/api/stock/movements");
        reqExcess.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        reqExcess.Content = JsonContent.Create(new CreateMovementRequest(
            chantierId,
            material.Id,
            MovementType.Sortie,
            Quantity: 999999m,
            UnitPrice: material.UnitPrice,
            PhaseId: null,
            SupplierId: null,
            DeliveryNoteNumber: null,
            Date: null,
            IsNegativeStockAuthorized: false));

        var resExcess = await _client.SendAsync(reqExcess);
        Assert.Equal(HttpStatusCode.Conflict, resExcess.StatusCode);
    }

    [Fact]
    public async Task ClosedChantier_IsLockedForModifications_RG07()
    {
        var token = await AuthenticateAsync();

        // 1. Créer un chantier de test
        using var reqCreate = new HttpRequestMessage(HttpMethod.Post, "/api/chantiers");
        reqCreate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        reqCreate.Content = JsonContent.Create(new CreateChantierRequest(
            Reference: "TEST-RG07-" + Guid.NewGuid().ToString("N")[..6],
            Name: "Chantier Test RG07",
            Client: "Client Test",
            Address: "Test",
            Latitude: null,
            Longitude: null,
            StartDate: DateTime.Today,
            PlannedEndDate: DateTime.Today.AddMonths(1),
            TotalBudget: 10000000m,
            Status: ChantierStatus.Planifie,
            Description: "Test RG-07"));

        var resCreate = await _client.SendAsync(reqCreate);
        Assert.Equal(HttpStatusCode.Created, resCreate.StatusCode);
        var created = await resCreate.Content.ReadFromJsonAsync<ChantierAggregate>();
        var id = created!.Chantier.Id;

        // 2. Le clôturer
        using var reqStatus = new HttpRequestMessage(HttpMethod.Patch, $"/api/chantiers/{id}/status");
        reqStatus.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        reqStatus.Content = JsonContent.Create(new SetStatusRequest(ChantierStatus.Cloture));
        var resStatus = await _client.SendAsync(reqStatus);
        Assert.Equal(HttpStatusCode.NoContent, resStatus.StatusCode);

        // 3. Tenter d'ajouter une dépense -> Doit renvoyer Conflict (409)
        using var reqExpense = new HttpRequestMessage(HttpMethod.Post, "/api/budget/expenses");
        reqExpense.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        reqExpense.Content = JsonContent.Create(new CreateExpenseRequest(
            ChantierId: id,
            Category: BudgetCategory.Administratif,
            Amount: 50000m,
            Date: DateTime.Today,
            Supplier: "Fournisseur Test",
            Label: "Frais",
            PaymentMode: PaymentMode.Especes,
            ReceiptPhotoPath: null));

        var resExpense = await _client.SendAsync(reqExpense);
        Assert.Equal(HttpStatusCode.Conflict, resExpense.StatusCode);
    }
}
