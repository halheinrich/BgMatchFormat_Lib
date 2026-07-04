namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// Reads committed golden <c>.MAT</c> fixtures and compares exporter output to
/// them byte-for-byte. Setting the environment variable
/// <c>BGMATCHFORMAT_UPDATE_GOLDENS=1</c> rewrites the source goldens from the
/// current output instead of asserting — a regeneration convenience. Regenerated
/// goldens are only trustworthy once their column geometry has been verified
/// against the real reference exports; regeneration alone proves nothing.
/// </summary>
internal static class GoldenFile
{
    private const string UpdateEnvVar = "BGMATCHFORMAT_UPDATE_GOLDENS";

    /// <summary>
    /// Asserts <paramref name="actual"/> equals the committed golden
    /// <paramref name="name"/> (e.g. <c>"dance.mat"</c>), or rewrites it in update
    /// mode.
    /// </summary>
    public static void Verify(string name, string actual)
    {
        if (Environment.GetEnvironmentVariable(UpdateEnvVar) == "1")
        {
            string sourcePath = Path.Combine(SourceGoldensDir(), name);
            File.WriteAllText(sourcePath, actual);
            return;
        }

        string outputPath = Path.Combine(AppContext.BaseDirectory, "Goldens", name);
        Assert.True(File.Exists(outputPath), $"Golden '{name}' is missing. Regenerate with {UpdateEnvVar}=1.");
        string expected = File.ReadAllText(outputPath);
        Assert.Equal(expected, actual);
    }

    private static string SourceGoldensDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null &&
               !File.Exists(Path.Combine(dir.FullName, "BgMatchFormat_Lib.Tests.csproj")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
            throw new InvalidOperationException("Could not locate the test project directory.");

        string goldens = Path.Combine(dir.FullName, "Goldens");
        Directory.CreateDirectory(goldens);
        return goldens;
    }
}
