namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Graph;

using AStar.Dev.OneDrive.Sync.Client.Services.Graph;

public class GraphServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Act
        var service = new GraphService();

        // Assert
        service.ShouldNotBeNull();
    }

    [Fact]
    public void GraphService_ShouldImplementIGraphService()
    {
        // Arrange
        var service = new GraphService();

        // Assert
        service.ShouldBeAssignableTo<IGraphService>();
    }
}

