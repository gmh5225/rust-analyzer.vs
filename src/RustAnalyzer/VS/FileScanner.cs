using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KS.RustAnalyzer.Cargo;
using Microsoft.VisualStudio.Workspace;
using Microsoft.VisualStudio.Workspace.Build;
using Microsoft.VisualStudio.Workspace.Debug;
using Microsoft.VisualStudio.Workspace.Indexing;

namespace KS.RustAnalyzer.VS;

public class FileScanner : IFileScanner
{
    private readonly string _workspaceRoot;

    public FileScanner(string workspaceRoot)
    {
        _workspaceRoot = workspaceRoot;
    }

    public async Task<T> ScanContentAsync<T>(string filePath, CancellationToken cancellationToken)
        where T : class
    {
        var owningManifest = Manifest.IsManifest(filePath) ? Manifest.Create(filePath) : Manifest.GetParentManifest(_workspaceRoot, filePath);
        if (owningManifest == null)
        {
            return (T)(IReadOnlyCollection<T>)Array.Empty<T>();
        }

        if (typeof(T) == FileScannerTypeConstants.FileDataValuesType)
        {
            var ret = GetFileDataValues(owningManifest, filePath);
            return await Task.FromResult((T)(IReadOnlyCollection<FileDataValue>)ret);
        }
        else if (typeof(T) == FileScannerTypeConstants.FileReferenceInfoType)
        {
            var ret = GetFileReferenceInfos(owningManifest, filePath);
            return await Task.FromResult((T)(IReadOnlyCollection<FileReferenceInfo>)ret);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private List<FileDataValue> GetFileDataValues(Manifest owningManifest, string filePath)
    {
        var allFileDataValues = new List<FileDataValue>();

        if (owningManifest.Is(filePath) && owningManifest.IsPackage && owningManifest.IsBinary)
        {
            var launchSettings = new PropertySettings
            {
                [LaunchConfigurationConstants.NameKey] = owningManifest.StartupProjectEntryName,
                [LaunchConfigurationConstants.DebugTypeKey] = LaunchConfigurationConstants.NativeOptionKey,
            };

            allFileDataValues.Add(
                new FileDataValue(
                    type: DebugLaunchActionContext.ContextTypeGuid,
                    name: DebugLaunchActionContext.IsDefaultStartupProjectEntry,
                    value: launchSettings,
                    target: null,
                    context: null));

            var fileDataValuesForAllProfiles1 = owningManifest.Profiles.Select(
                profile =>
                    new FileDataValue(
                        type: Microsoft.VisualStudio.Workspace.Build.BuildConfigurationContext.ContextTypeGuid,
                        name: Microsoft.VisualStudio.Workspace.Build.BuildConfigurationContext.DataValueName,
                        value: null,
                        target: owningManifest.GetTargetPathForProfile(profile),
                        context: profile));

            allFileDataValues.AddRange(fileDataValuesForAllProfiles1);
        }

        if (owningManifest.Is(filePath))
        {
            var fileDataValuesForAllProfiles = owningManifest.Profiles.Select(
            profile =>
                new FileDataValue(
                    type: Microsoft.VisualStudio.Workspace.Build.BuildConfigurationContext.ContextTypeGuid,
                    name: Microsoft.VisualStudio.Workspace.Build.BuildConfigurationContext.DataValueName,
                    value: null,
                    target: null,
                    context: profile));

            allFileDataValues.AddRange(fileDataValuesForAllProfiles);
        }

        return allFileDataValues;
    }

    private static List<FileReferenceInfo> GetFileReferenceInfos(Manifest owningManifest, string filePath)
    {
        var allFileRefInfos = new List<FileReferenceInfo>();

        if (owningManifest.Is(filePath) && owningManifest.IsPackage)
        {
            var fileRefInfosForProfiles = owningManifest.Profiles
                .Select(
                    profile => new FileReferenceInfo(
                        relativePath: owningManifest.GetTargetPathForProfileRelativeToPath(profile, filePath),
                        target: owningManifest.GetTargetPathForProfile(profile),
                        context: profile,
                        referenceType: (int)FileReferenceInfoType.Output));

            allFileRefInfos.AddRange(fileRefInfosForProfiles);
        }

        return allFileRefInfos;
    }
}
