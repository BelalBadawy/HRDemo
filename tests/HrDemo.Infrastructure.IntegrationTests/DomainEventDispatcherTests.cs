using FluentAssertions;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using HrDemo.Application.Abstractions.DateTime;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Abstractions.Events;
using HrDemo.Domain.Common;
using HrDemo.Domain.Interfaces;
using HrDemo.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace HrDemo.Infrastructure.IntegrationTests;

public sealed class DummyDomainEvent : IDomainEvent
{
    public string Data { get; }
    public DummyDomainEvent(string data)
    {
        Data = data;
    }
}

public sealed class DummyEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<DummyEntity> DummyEntities => Set<DummyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DummyEntity>(cfg =>
        {
            cfg.HasKey(e => e.Id);
            cfg.Property(e => e.Name).IsRequired();
        });
    }
}

public sealed class DomainEventDispatcherTests : IAsyncLifetime, IDisposable
{
    private readonly TestDbContext _context;
    private readonly IDomainEventDispatcher _dispatcherMock;
    private readonly string _connectionString;

    public DomainEventDispatcherTests()
    {
        _connectionString = $"Server=(localdb)\\mssqllocaldb;Database=HrDemoDb_Integration_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";

        var currentUserMock = Substitute.For<ICurrentUser>();
        currentUserMock.UserId.Returns("1");

        var clockMock = Substitute.For<IClock>();
        clockMock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _dispatcherMock = Substitute.For<IDomainEventDispatcher>();

        var interceptor = new AuditableAndDomainEventsInterceptor(currentUserMock, clockMock, _dispatcherMock);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(_connectionString)
            .AddInterceptors(interceptor)
            .Options;

        _context = new TestDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GivenEntityWithEvents_WhenSaveSucceeds_ShouldDispatchEventsPostCommit()
    {
        // Arrange
        var entity = new DummyEntity { Name = "Integration Test" };
        var domainEvent = new DummyDomainEvent("SucceedData");
        entity.AddDomainEvent(domainEvent);

        _context.DummyEntities.Add(entity);

        // Act
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        await _dispatcherMock.Received(1).DispatchEventsAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(evs => evs.Contains(domainEvent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenEntityWithEvents_WhenSaveFails_ShouldNotDispatchEvents()
    {
        // Arrange
        var entity = new DummyEntity { Name = null! }; // Will fail because Name is non-nullable string
        var domainEvent = new DummyDomainEvent("FailData");
        entity.AddDomainEvent(domainEvent);

        _context.DummyEntities.Add(entity);

        // Act
        Func<Task> saveAction = () => _context.SaveChangesAsync();

        // Assert
        await saveAction.Should().ThrowAsync<DbUpdateException>();
        await _dispatcherMock.DidNotReceive().DispatchEventsAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }
}
