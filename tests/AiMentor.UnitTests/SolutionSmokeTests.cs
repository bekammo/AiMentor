namespace AiMentor.UnitTests;

public sealed class SolutionSmokeTests
{
    [Fact]
    public void Domain_assembly_has_no_package_dependencies()
    {
        var references = typeof(AiMentor.Domain.AssemblyMarker).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, r => r.Name is not null && !r.Name.StartsWith("System", StringComparison.Ordinal) && r.Name != "netstandard");
    }
}
