using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using KS.RustAnalyzer.TestAdapter.Common;
using KS.RustAnalyzer.Tests.Common;
using KS.RustAnalyzer.VS;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace KS.RustAnalyzer.UnitTests.VS;

public class FileContextProviderTests
{
    public static IEnumerable<object[]> GetTestData() => TestData.Get();

    [Theory]
    [UseReporter(typeof(DiffReporter))]
    [MemberData(nameof(GetTestData))]
    public async Task GetContextsForFileTestsAsync(string workspaceRootRel, string filePathRel)
    {
        NamerFactory.AdditionalInformation = $"{Path.Combine(workspaceRootRel, filePathRel).ReplaceInvalidChars()}";
        string workspaceRoot = Path.Combine(TestHelpers.ThisTestRoot, workspaceRootRel);
        var fcp = new FileContextProvider(workspaceRoot, Mock.Of<IBuildOutputSink>(), TestHelpers.TL);
        var filePath = Path.Combine(workspaceRoot, filePathRel);

        var refInfos = await fcp.GetContextsForFileAsync(filePath, default);
        var processedRefInfos = refInfos.Select(
            ri => new
            {
                ri.ProviderType,
                ri.ContextType,
                Context = new
                {
                    (ri.Context as BuildFileContextBase).BuildConfiguration,
                    WorkspaceRoot = (ri.Context as BuildFileContextBase).BuildTargetInfo.WorkspaceRoot.RemoveMachineSpecificPaths(),
                    (ri.Context as BuildFileContextBase).BuildTargetInfo.Profile,
                    FilePath = (ri.Context as BuildFileContextBase).BuildTargetInfo.FilePath.RemoveMachineSpecificPaths(),
                    (ri.Context as BuildFileContextBase).BuildTargetInfo.AdditionalBuildArgs,
                },
                InputFiles = ri.InputFiles.Select(i => i.RemoveMachineSpecificPaths()).ToArray(),
                ri.DisplayName,
            });
        Approvals.VerifyAll(processedRefInfos.Select(o => o.SerializeObject(Formatting.Indented)), label: string.Empty);
    }
}
