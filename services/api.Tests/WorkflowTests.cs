using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
using SistemasPrecios.Api.Dtos;
using SistemasPrecios.Api.Services;
namespace SistemasPrecios.Api.Tests;
public sealed class WorkflowTests : IAsyncLifetime
{
    private ApplicationDbContext db = null!;
    private string databaseName = "";
    private bool sqlServer;
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connection = Environment.GetEnvironmentVariable("PRECIOS_TEST_SQLSERVER"); sqlServer = !string.IsNullOrWhiteSpace(connection);
        if (sqlServer) { databaseName = "PreciosTests_" + Guid.NewGuid().ToString("N"); var cs = new SqlConnectionStringBuilder(connection) { InitialCatalog = databaseName }; options.UseSqlServer(cs.ConnectionString); }
        else options.UseSqlite("Data Source=:memory:");
        db = new ApplicationDbContext(options.Options); if (!sqlServer) await db.Database.OpenConnectionAsync(); await db.Database.EnsureCreatedAsync();
    }
    public async Task DisposeAsync()
    {
        if (sqlServer)
        {
            var catalog = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog;
            Assert.Equal(databaseName, catalog); Assert.StartsWith("PreciosTests_", catalog); Assert.Equal(45, catalog.Length);
            await db.Database.CloseConnectionAsync(); await db.Database.EnsureDeletedAsync();
        }
        await db.DisposeAsync();
    }
    private DocumentProcessingService Processor(IPriceExtractor? extractor = null) => new(db, extractor ?? new TestExtractor(), new TestMatching(), NullLogger<DocumentProcessingService>.Instance);
    private async Task<Document> Pending(string name = "Proveedor")
    {
        var supplier = new Supplier { Name = name }; var doc = new Document { Supplier = supplier, Status = DocumentStatus.NeedsReview, ObservedAt = DateTime.UtcNow.Date };
        doc.ExtractedLines.Add(new ExtractedLine { LineNumber = 1, RawText = "Arroz bolsa", SuggestedName = "Arroz", NeedsReview = true }); db.Documents.Add(doc); await db.SaveChangesAsync(); return doc;
    }
    private static DocumentReviewRequest Review(Document d, decimal price = 20, decimal qty = 5, string unit = "KG", string currency = "PEN") => new([new(d.ExtractedLines[0].Id, "Arroz marca A extra", unit, qty, price, currency)]);
    [Fact]
    public async Task Approval_NormalizesPresentationAndPersistsHistory()
    {
        var d = await Pending(); await Processor().ReviewDocumentAsync(d.Id, Review(d, 22.5m), default);
        var snapshot = await db.PriceSnapshots.SingleAsync(); Assert.Equal(4.5m, snapshot.Price); Assert.Equal("KG", snapshot.Unit); Assert.Equal("PEN", snapshot.Currency);
        var comparisons = await new ComparisonQueryService(db).GetCurrentComparisonsAsync(default); Assert.Equal(1, comparisons.Single().SupplierCount);
    }
    [Fact]
    public async Task RepeatedApprovalCannotDuplicateOrErasePrices()
    {
        var d = await Pending(); await Processor().ReviewDocumentAsync(d.Id, Review(d), default);
        await Assert.ThrowsAsync<ArgumentException>(() => Processor().ReviewDocumentAsync(d.Id, Review(d, 3), default)); Assert.Single(await db.PriceSnapshots.ToListAsync());
    }
    [Theory]
    [InlineData(0, 5)]
    [InlineData(20, 0)]
    [InlineData(-1, 5)]
    public async Task InvalidAmountsCreateNoPrices(decimal price, decimal quantity)
    {
        var d = await Pending(); await Assert.ThrowsAsync<ArgumentException>(() => Processor().ReviewDocumentAsync(d.Id, Review(d, price, quantity), default)); Assert.Empty(await db.PriceSnapshots.ToListAsync());
    }
    [Fact]
    public async Task MissingOrDuplicateRowsCannotApprove()
    {
        var d = await Pending(); var row = Review(d).Lines[0];
        await Assert.ThrowsAsync<ArgumentException>(() => Processor().ReviewDocumentAsync(d.Id, new([row, row]), default));
        await Assert.ThrowsAsync<ArgumentException>(() => Processor().ReviewDocumentAsync(d.Id, new([]), default)); Assert.Empty(await db.PriceSnapshots.ToListAsync());
    }
    [Fact]
    public async Task CurrencyAndUnitNormalizationPreventFalseWinner()
    {
        var a = await Pending("A"); await Processor().ReviewDocumentAsync(a.Id, Review(a, 20, 5), default);
        var b = await Pending("B"); await Processor().ReviewDocumentAsync(b.Id, Review(b, 6, 1000, "G"), default);
        var c = await Pending("C"); await Processor().ReviewDocumentAsync(c.Id, Review(c, 1, 1, "KG", "USD"), default);
        var comparisons = await new ComparisonQueryService(db).GetCurrentComparisonsAsync(default); Assert.Equal(2, comparisons.Count);
        var pen = comparisons.Single(c => c.Currency == "PEN"); Assert.Equal("A", pen.BestSupplier); Assert.Equal(4, pen.BestPrice); Assert.Equal(2, pen.SupplierCount);
        Assert.Null((await new ComparisonQueryService(db).GetHistoryAsync(null, c.SupplierId, default)).Single().VariationPercentage);
    }
    [Fact]
    public async Task IncompatibleUnitRollsBackWholeApproval()
    {
        var a = await Pending("A"); await Processor().ReviewDocumentAsync(a.Id, Review(a), default);
        var b = await Pending("B"); await Assert.ThrowsAsync<ArgumentException>(() => Processor().ReviewDocumentAsync(b.Id, Review(b, 20, 5, "L"), default));
        db.ChangeTracker.Clear(); Assert.Equal(DocumentStatus.NeedsReview, (await db.Documents.SingleAsync(d => d.Id == b.Id)).Status); Assert.Single(await db.PriceSnapshots.ToListAsync());
    }
    [Fact]
    public async Task ExtractionFailurePreservesPreviousEvidenceAndNoInventedPrices()
    {
        var d = await Pending(); d.Status = DocumentStatus.Uploaded; await db.SaveChangesAsync(); await Processor(new FailingExtractor()).ProcessDocumentAsync(d.Id);
        Assert.Equal(DocumentStatus.Failed, (await db.Documents.SingleAsync()).Status); Assert.Single(await db.ExtractedLines.ToListAsync()); Assert.Empty(await db.PriceSnapshots.ToListAsync());
    }
    [Fact]
    public async Task ExtractionAlwaysRequiresHumanReview()
    {
        var d = await Pending(); d.Status = DocumentStatus.Uploaded; await db.SaveChangesAsync(); await Processor().ProcessDocumentAsync(d.Id);
        Assert.Equal(DocumentStatus.NeedsReview, (await db.Documents.SingleAsync()).Status); Assert.All(await db.ExtractedLines.ToListAsync(), l => Assert.True(l.NeedsReview)); Assert.Empty(await db.PriceSnapshots.ToListAsync());
    }
    private sealed class TestExtractor : IPriceExtractor { public Task<IReadOnlyList<StructuredExtractionLine>> ExtractAsync(string p, string m, string k, CancellationToken ct) => Task.FromResult<IReadOnlyList<StructuredExtractionLine>>([new(1, "Arroz 5kg S/20", "Arroz", "KG", 5, 20, 0, "PEN")]); }
    [Fact]
    public async Task ExcludedTotalsDoNotBecomePrices()
    {
        var d = await Pending();
        var total = new ExtractedLine { DocumentId = d.Id, LineNumber = 2, RawText = "Total 20", NeedsReview = true };
        db.ExtractedLines.Add(total); await db.SaveChangesAsync();
        await Processor().ReviewDocumentAsync(d.Id, new([Review(d).Lines[0], new(total.Id, "", "", 0, 0, "", true)]), default);
        Assert.Single(await db.PriceSnapshots.ToListAsync());
        Assert.True((await db.ExtractedLines.SingleAsync(l => l.Id == total.Id)).Excluded);
    }
    [Fact]
    public async Task BackdatedObservationDoesNotReplaceCurrentPrice()
    {
        var current = await Pending(); await Processor().ReviewDocumentAsync(current.Id, Review(current, 20), default);
        var old = new Document { SupplierId = current.SupplierId, Status = DocumentStatus.NeedsReview, ObservedAt = DateTime.UtcNow.Date.AddDays(-7) };
        old.ExtractedLines.Add(new ExtractedLine { LineNumber = 1, RawText = "Precio anterior" });
        db.Documents.Add(old); await db.SaveChangesAsync();
        await Processor().ReviewDocumentAsync(old.Id, Review(old, 10), default);
        Assert.Equal(4, (await new ComparisonQueryService(db).GetCurrentComparisonsAsync(default)).Single().BestPrice);
        Assert.Equal(100, (await new ComparisonQueryService(db).GetHistoryAsync(null, null, default)).Last().VariationPercentage);
    }
    [Fact]
    public async Task PriceThatRoundsToZeroCannotBeApproved()
    {
        var d = await Pending();
        await Assert.ThrowsAsync<ArgumentException>(() => Processor().ReviewDocumentAsync(d.Id, Review(d, 0.000001m, 1_000_000m), default));
        Assert.Empty(await db.PriceSnapshots.ToListAsync());
    }
    [Fact]
    public async Task DuplicateProductRowsRollBackAllChanges()
    {
        var d = await Pending();
        var second = new ExtractedLine { DocumentId = d.Id, LineNumber = 2, RawText = "Mismo producto" };
        db.ExtractedLines.Add(second); await db.SaveChangesAsync();
        var row = Review(d).Lines[0];
        await Assert.ThrowsAsync<ArgumentException>(() => Processor().ReviewDocumentAsync(d.Id, new([row, row with { ExtractedLineId = second.Id }]), default));
        db.ChangeTracker.Clear(); Assert.Empty(await db.PriceSnapshots.ToListAsync()); Assert.Empty(await db.CanonicalProducts.ToListAsync());
    }
    private sealed class FailingExtractor : IPriceExtractor { public Task<IReadOnlyList<StructuredExtractionLine>> ExtractAsync(string p, string m, string k, CancellationToken ct) => throw new ExtractionException("Fallo controlado"); }
    [Fact]
    public async Task DashboardKeepsActualPreviousPriceInsteadOfReconstructingRoundedVariation()
    {
        var first = await Pending();
        await Processor().ReviewDocumentAsync(first.Id, Review(first, 20.61728m, 5), default);
        var next = new Document { SupplierId = first.SupplierId, Status = DocumentStatus.NeedsReview, ObservedAt = first.ObservedAt };
        next.ExtractedLines.Add(new ExtractedLine { LineNumber = 1, RawText = "Nuevo precio" });
        db.Documents.Add(next); await db.SaveChangesAsync();
        await Processor().ReviewDocumentAsync(next.Id, Review(next, 5, 1), default);
        var mover = (await new ComparisonQueryService(db).GetDashboardSummaryAsync(default)).BiggestMovers.Single();
        Assert.Equal(4.123456m, mover.PreviousPrice); Assert.Equal(5, mover.CurrentPrice);
    }
    private sealed class TestMatching : IProductMatchingService { public Task<IReadOnlyList<ProductMatchCandidate>> FindCandidatesAsync(StructuredExtractionLine l, CancellationToken ct) => Task.FromResult<IReadOnlyList<ProductMatchCandidate>>([]); }
}
public sealed class OpenAiContractTests
{
    private static string Response(string status = "completed") => JsonSerializer.Serialize(new { status, output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = "{\"lines\":[{\"rawText\":\"Arroz S/ 20\",\"name\":\"Arroz\",\"unit\":null,\"quantity\":null,\"price\":20,\"currency\":\"PEN\"}]}" } } } } });
    [Fact] public void UnknownFieldsStayNull() { var line = OpenAiPriceExtractor.ParseResponse(Response()).Single(); Assert.Null(line.SuggestedQuantity); Assert.Null(line.SuggestedUnit); Assert.Equal(20, line.SuggestedPrice); }
    [Fact] public void IncompleteOutputCannotCreatePartialPrices() => Assert.Throws<ExtractionException>(() => OpenAiPriceExtractor.ParseResponse(Response("incomplete")));
    [Fact]
    public async Task ImageRequestUsesActualBytesAndStrictSchema()
    {
        var path = Path.GetTempFileName(); await File.WriteAllBytesAsync(path, [1, 2, 3, 4]);
        try
        {
            var handler = new CapturingHandler(); var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "OpenAI:ApiKey", "test-only-key" } }).Build();
            var extractor = new OpenAiPriceExtractor(new HttpClient(handler), config); await extractor.ExtractAsync(path, "image/png", "label", default);
            using var payload = JsonDocument.Parse(handler.Body!); Assert.False(payload.RootElement.GetProperty("store").GetBoolean()); Assert.True(payload.RootElement.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean());
            Assert.Equal("data:image/png;base64,AQIDBA==", payload.RootElement.GetProperty("input")[0].GetProperty("content")[1].GetProperty("image_url").GetString());
        }
        finally { File.Delete(path); }
    }
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { Body = await request.Content!.ReadAsStringAsync(ct); return new(HttpStatusCode.OK) { Content = new StringContent(Response(), Encoding.UTF8, "application/json") }; }
    }
    [Theory][InlineData("application/pdf", "%PDF-1.7", true)][InlineData("image/jpeg", "not an image", false)] public void SignatureRejectsSpoofedContent(string mime, string text, bool expected) => Assert.Equal(expected, FileSignature.IsValid(Encoding.UTF8.GetBytes(text), mime));
}
