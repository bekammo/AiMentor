namespace AiMentor.Evaluation;

public sealed class EvaluationGateTests
{
    [Fact]
    public void Gate_matches_environment_variable()
    {
        var expected = Environment.GetEnvironmentVariable("AIMENTOR_EVAL") == "1";

        Assert.Equal(expected, EvaluationGate.Enabled);
    }
}
