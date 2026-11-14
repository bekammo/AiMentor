namespace AiMentor.Evaluation;

/// <summary>Real-model evaluations run only when AIMENTOR_EVAL=1, so CI spends nothing.</summary>
internal static class EvaluationGate
{
    public static bool Enabled => Environment.GetEnvironmentVariable("AIMENTOR_EVAL") == "1";
}
