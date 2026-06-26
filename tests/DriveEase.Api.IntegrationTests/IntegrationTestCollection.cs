using Xunit;

namespace DriveEase.Api.IntegrationTests;

/// <summary>
/// Shares a single DriveEaseWebApplicationFactory (and thus one SQL Server Docker container)
/// across all integration test classes. Without this, each IClassFixture creates its own
/// container — 6 containers on a 2-vCPU GitHub Actions runner exhausts memory/CPU, causing
/// random container crashes and "container is not running" test failures.
///
/// With [Collection], all tests run sequentially inside one shared host (one container).
/// </summary>
[CollectionDefinition("IntegrationTests")]
public sealed class IntegrationTestCollection : ICollectionFixture<DriveEaseWebApplicationFactory>
{
}
