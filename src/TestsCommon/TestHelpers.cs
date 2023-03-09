using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using KS.RustAnalyzer.TestAdapter;
using KS.RustAnalyzer.TestAdapter.Cargo;
using KS.RustAnalyzer.TestAdapter.Common;
using Moq;

namespace KS.RustAnalyzer.Tests.Common;

public static class TestHelpers
{
    public const string TestContainersSearchPattern = $"*{Constants.TestsContainerExtension}";

    public static readonly PathEx ThisTestRoot =
        (PathEx)Path.Combine(
            Path.GetDirectoryName(Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath)),
            @"Cargo\TestData").ToLowerInvariant();

    public static readonly TL TL =
        new ()
        {
            L = Mock.Of<ILogger>(),
            T = Mock.Of<ITelemetryService>(),
        };

    private static readonly ConcurrentDictionary<PathEx, IMetadataService> MetadataServices = new ConcurrentDictionary<PathEx, IMetadataService>();

    public static PathEx RemoveMachineSpecificPaths(this PathEx @this)
        => (PathEx)((string)@this).ToLowerInvariant().Replace(ThisTestRoot, "<TestRoot>");

    public static IMetadataService MS(this PathEx @this)
    {
        // NOTE: This simulates the case when a folder with multiple workspaces is opened.
        var root = @this.GetDirectoryName();
        return MetadataServices.GetOrAdd(root, (wr) => new MetadataService(new ToolChainService(TL.T, TL.L), wr, TL));
    }

    public static string Replace(this string str, string old, string @new, StringComparison comparison)
    {
        @new = @new ?? string.Empty;
        if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(old) || old.Equals(@new, comparison))
        {
            return str;
        }

        int foundAt = 0;
        while ((foundAt = str.IndexOf(old, foundAt, comparison)) != -1)
        {
            str = str.Remove(foundAt, old.Length).Insert(foundAt, @new);
            foundAt += @new.Length;
        }

        return str;
    }

    public static Task<bool> DoBuildAsync(this IToolChainService @this, PathEx workspacePath, PathEx manifestPath, string profile)
    {
        return @this.BuildAsync(
                    new BuildTargetInfo { WorkspaceRoot = workspacePath, ManifestPath = manifestPath, Profile = profile, AdditionalBuildArgs = string.Empty },
                    new BuildOutputSinks { OutputSink = Mock.Of<IBuildOutputSink>(), BuildActionProgressReporter = bm => Task.CompletedTask },
                    default);
    }

    public static void CleanTestContainers(this PathEx @this)
    {
        if (!@this.DirectoryExists())
        {
            return;
        }

        Directory.EnumerateFiles(@this, TestContainersSearchPattern).ForEach(File.Delete);
    }
}
