namespace WebAPI.IntegrationTests;

// Starting a Postgres container is expensive (seconds, not milliseconds) — sharing one
// CustomWebApplicationFactory (and its one container) across every test class in this project
// via a collection fixture, rather than IClassFixture per class, keeps the whole suite from
// paying that cost once per class.
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>;
