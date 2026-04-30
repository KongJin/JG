using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

internal static class Program
{
    private const string ConverterVersion = "gx-pipeline-v14";
    private const string ManifestPath = "artifacts/nova1492/gx_conversion_manifest.csv";
    private const string SummaryPath = "artifacts/nova1492/gx_conversion_summary.md";
    private const string PipelineStatePath = "artifacts/nova1492/gx_pipeline_state.csv";
    private const string HierarchyDiagnosticsCsvPath = "artifacts/nova1492/gx_hierarchy_diagnostics.csv";
    private const string HierarchyDiagnosticsMdPath = "artifacts/nova1492/gx_hierarchy_diagnostics.md";
    private const int MaxVertexCount = 10000;
    private const int MaxIndexCount = 200000;
    private const float LegHelperSpanThreshold = 0.25f;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static int Main(string[] args)
    {
        var sourceRoot = GetArg(args, "--source-root") ?? @"C:\Program Files (x86)\Nova1492";
        var outputRoot = GetArg(args, "--output-root") ?? "Assets/Art/Nova1492/GXConverted";
        var includeRelative = GetArg(args, "--include-relative");
        var catalogPath = GetArg(args, "--catalog");
        var categoryFilter = GetArg(args, "--category");
        var partIdFilter = GetArg(args, "--part-id");
        var stage = ParseStage(GetArg(args, "--stage") ?? GetArg(args, "--mode"), args);
        var catalogOnly = HasArg(args, "--catalog-only");
        var changedOnly = HasArg(args, "--changed-only");
        var diagnostics = HasArg(args, "--diagnostics") || stage == PipelineStage.Analyze;
        var limitRaw = GetArg(args, "--limit");
        var limit = int.TryParse(limitRaw, out var parsedLimit) ? parsedLimit : int.MaxValue;
        var clean = HasArg(args, "--clean");
        var writeManifest = !HasArg(args, "--no-manifest");
        var analyzeOnly = stage == PipelineStage.Analyze;

        if (!Directory.Exists(sourceRoot))
        {
            Console.Error.WriteLine($"Source root not found: {sourceRoot}");
            return 2;
        }

        var modelDir = Path.Combine(outputRoot, "Models");
        var textureDir = Path.Combine(outputRoot, "Textures");
        if (clean)
        {
            if (Directory.Exists(modelDir))
            {
                Directory.Delete(modelDir, recursive: true);
            }

            if (Directory.Exists(textureDir))
            {
                Directory.Delete(textureDir, recursive: true);
            }
        }

        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(textureDir);
        Directory.CreateDirectory("artifacts/nova1492");

        var catalogRows = LoadCatalogRows(catalogPath)
            .Where(row => MatchesCatalogFilters(row, categoryFilter, partIdFilter))
            .ToArray();
        var catalogModelPaths = catalogRows.ToDictionary(
            row => NormalizeRelativePath(row.SourceRelativePath),
            row => row.ModelPath,
            StringComparer.OrdinalIgnoreCase);
        var catalogRowByRelativePath = catalogRows.ToDictionary(
            row => NormalizeRelativePath(row.SourceRelativePath),
            row => row,
            StringComparer.OrdinalIgnoreCase);
        var previousState = LoadPipelineState(PipelineStatePath);
        var includeKey = NormalizeRelativePath(includeRelative);
        IEnumerable<string> gxFilesQuery = Directory.EnumerateFiles(sourceRoot, "*.gx", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(sourceRoot, "*.GX", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(includeKey))
        {
            gxFilesQuery = gxFilesQuery.Where(path =>
                string.Equals(
                    NormalizeRelativePath(Path.GetRelativePath(sourceRoot, path)),
                    includeKey,
                    StringComparison.OrdinalIgnoreCase));
        }
        else if (catalogRows.Length > 0 || catalogOnly)
        {
            if (catalogModelPaths.Count == 0)
            {
                Console.Error.WriteLine("Catalog filtering requires source_relative_path/model_path rows.");
                return 2;
            }

            gxFilesQuery = gxFilesQuery.Where(path =>
                catalogModelPaths.ContainsKey(NormalizeRelativePath(Path.GetRelativePath(sourceRoot, path))));
        }

        var gxFiles = gxFilesQuery
            .Take(limit)
            .ToArray();

        var rows = new List<ManifestRow>();
        var diagnosticsRows = new List<HierarchyDiagnosticRow>();
        var stateRows = new List<PipelineStateRow>();
        var success = 0;
        var skipped = 0;

        foreach (var gxPath in gxFiles)
        {
            var relative = Path.GetRelativePath(sourceRoot, gxPath);
            var relativeKey = NormalizeRelativePath(relative);
            catalogRowByRelativePath.TryGetValue(relativeKey, out var catalogRow);
            var sourceHash = ComputeFileHash(gxPath);
            var catalogRowHash = ComputeStringHash(catalogRow?.RawLine ?? "");
            try
            {
                var bytes = File.ReadAllBytes(gxPath);
                var meshes = FindMeshes(bytes);
                if (meshes.Count == 0)
                {
                    var fallbackMesh = FindBestMesh(bytes);
                    if (fallbackMesh is not null)
                    {
                        meshes.Add(fallbackMesh);
                    }
                }

                if (meshes.Count == 0)
                {
                    rows.Add(ManifestRow.Failed(relative, gxPath, bytes.Length, sourceHash, catalogRowHash, "no_valid_mesh_stream"));
                    stateRows.Add(PipelineStateRow.FromManifest(relative, catalogRow?.PartId ?? "", sourceHash, catalogRowHash, "", "failed", "failed"));
                    continue;
                }

                var nodeTransforms = FindNodeTransforms(relative, bytes);
                var beforeRepairBounds = "";
                var afterRepairBounds = "";
                if (nodeTransforms.OriginalNodes.Count > 0)
                {
                    if (nodeTransforms.RepairApplied)
                    {
                        beforeRepairBounds = FormatBounds(ApplyNodeTransforms(relative, meshes, nodeTransforms.OriginalNodes));
                    }

                    meshes = ApplyNodeTransforms(relative, meshes, nodeTransforms.Nodes);
                    if (nodeTransforms.RepairApplied)
                    {
                        afterRepairBounds = FormatBounds(meshes);
                    }
                }

                var xfiInfo = ReadXfiInfo(gxPath);
                var selection = SelectMeshesForExport(relative, meshes, xfiInfo, nodeTransforms.Nodes);
                var safeName = BuildSafeName(relative);
                var objPath = catalogModelPaths.TryGetValue(NormalizeRelativePath(relative), out var catalogModelPath) &&
                              !string.IsNullOrWhiteSpace(catalogModelPath)
                    ? catalogModelPath
                    : Path.Combine(modelDir, safeName + ".obj");
                var mtlPath = Path.Combine(modelDir, safeName + ".mtl");
                if (!string.Equals(Path.GetDirectoryName(objPath), modelDir, StringComparison.OrdinalIgnoreCase))
                {
                    mtlPath = Path.ChangeExtension(objPath, ".mtl");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(objPath) ?? modelDir);
                var texture = FindTextureForGx(gxPath, bytes);
                var textureOutputName = texture is null ? "" : safeName + Path.GetExtension(texture);
                var textureOutputPath = texture is null ? "" : Path.Combine(textureDir, textureOutputName);
                var textureReference = string.IsNullOrWhiteSpace(textureOutputPath)
                    ? ""
                    : Path.GetRelativePath(Path.GetDirectoryName(objPath) ?? ".", textureOutputPath).Replace('\\', '/');
                var assessment = AssessConversion(selection, texture, nodeTransforms);
                var status = nodeTransforms.RepairApplied ? "repaired" : "converted";

                if (diagnostics)
                {
                    diagnosticsRows.AddRange(BuildHierarchyDiagnostics(
                        relative,
                        catalogRow?.PartId ?? "",
                        selection.KeptMeshes,
                        nodeTransforms,
                        texture,
                        assessment));
                }

                if (!analyzeOnly &&
                    changedOnly &&
                    previousState.TryGetValue(relativeKey, out var previous) &&
                    previous.Status is "converted" or "repaired" or "skipped" &&
                    previous.SourceHash == sourceHash &&
                    previous.CatalogRowHash == catalogRowHash &&
                    previous.ConverterVersion == ConverterVersion &&
                    File.Exists(objPath))
                {
                    skipped++;
                    rows.Add(ManifestRow.Skipped(
                        relative,
                        gxPath,
                        bytes.Length,
                        objPath,
                        sourceHash,
                        catalogRowHash,
                        assessment,
                        "changed_only_cache_hit"));
                    continue;
                }

                if (!analyzeOnly)
                {
                    WriteObj(objPath, mtlPath, safeName, selection.KeptMeshes, textureReference);

                    if (texture is not null)
                    {
                        File.Copy(texture, textureOutputPath, overwrite: true);
                    }
                }

                success++;
                rows.Add(ManifestRow.Success(
                    analyzeOnly ? "analyzed" : status,
                    relative,
                    gxPath,
                    bytes.Length,
                    objPath,
                    selection.KeptMeshes.Sum(mesh => mesh.VertexCount),
                    selection.KeptMeshes.Sum(mesh => mesh.IndexCount / 3),
                    string.Join(";", selection.KeptMeshes.Select(mesh => "0x" + mesh.PositionStart.ToString("X", Invariant))),
                    string.Join(";", selection.KeptMeshes.Select(mesh => "0x" + mesh.NormalStart.ToString("X", Invariant))),
                    string.Join(";", selection.KeptMeshes.Select(mesh => "0x" + mesh.UvStart.ToString("X", Invariant))),
                    string.Join(";", selection.KeptMeshes.Select(mesh => "0x" + mesh.IndexStart.ToString("X", Invariant))),
                    texture,
                    textureOutputName,
                    selection.ParserMode,
                    meshes.Count,
                    selection.KeptMeshes.Count,
                    selection.AssemblyBlocks,
                    selection.DirectionBlocks,
                    selection.DroppedBlocks,
                    selection.AssemblyBlockDiagnostics,
                    selection.DirectionBlockDiagnostics,
                    selection.DroppedBlockDiagnostics,
                    CountLargeDroppedBlocks(selection.DroppedMeshes),
                    FormatBounds(selection.KeptMeshes),
                    xfiInfo.TransformCount,
                    xfiInfo.DirectionRangeCount,
                    sourceHash,
                    catalogRowHash,
                    assessment,
                    nodeTransforms.RepairRule,
                    nodeTransforms.RepairReason,
                    beforeRepairBounds,
                    afterRepairBounds));
                stateRows.Add(PipelineStateRow.FromManifest(relative, catalogRow?.PartId ?? "", sourceHash, catalogRowHash, objPath, analyzeOnly ? "analyzed" : status, assessment));
            }
            catch (Exception ex)
            {
                rows.Add(ManifestRow.Failed(relative, gxPath, 0, sourceHash, catalogRowHash, ex.GetType().Name + ": " + ex.Message));
                stateRows.Add(PipelineStateRow.FromManifest(relative, catalogRow?.PartId ?? "", sourceHash, catalogRowHash, "", "failed", "failed"));
            }
        }

        if (writeManifest)
        {
            WriteManifest(rows, sourceRoot, outputRoot, stage);
            if (!analyzeOnly)
            {
                WritePipelineState(stateRows);
            }
        }

        if (diagnostics)
        {
            WriteHierarchyDiagnostics(diagnosticsRows);
        }

        Console.WriteLine($"GX files: {gxFiles.Length}");
        Console.WriteLine(analyzeOnly ? $"Analyzed: {success}" : $"Converted/Repaired: {success}");
        Console.WriteLine($"Skipped: {skipped}");
        Console.WriteLine($"Failed: {rows.Count(row => row.Status == "failed")}");
        Console.WriteLine(writeManifest
            ? "Manifest: " + ManifestPath
            : "Manifest: skipped (--no-manifest)");
        if (diagnostics)
        {
            Console.WriteLine("Diagnostics: " + HierarchyDiagnosticsCsvPath);
        }

        return success > 0 || skipped > 0 ? 0 : 1;
    }

    private static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return i + 1 < args.Length ? args[i + 1] : null;
        }

        return null;
    }

    private static bool HasArg(string[] args, string name)
    {
        return args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }

    private static PipelineStage ParseStage(string? value, IReadOnlyCollection<string> args)
    {
        if (args.Any(arg => string.Equals(arg, "--analyze", StringComparison.OrdinalIgnoreCase)))
        {
            return PipelineStage.Analyze;
        }

        if (args.Any(arg => string.Equals(arg, "--convert", StringComparison.OrdinalIgnoreCase)))
        {
            return PipelineStage.Convert;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return PipelineStage.Convert;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "analyze" => PipelineStage.Analyze,
            "convert" => PipelineStage.Convert,
            _ => throw new ArgumentException("Unknown --stage value: " + value + ". Use analyze or convert.")
        };
    }

    private static bool MatchesCatalogFilters(CatalogRow row, string? categoryFilter, string? partIdFilter)
    {
        if (!string.IsNullOrWhiteSpace(partIdFilter) &&
            !string.Equals(row.PartId, partIdFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(categoryFilter))
        {
            return true;
        }

        return string.Equals(row.Category, categoryFilter, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(row.Slot, categoryFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static List<CatalogRow> LoadCatalogRows(string? catalogPath)
    {
        var output = new List<CatalogRow>();
        if (string.IsNullOrWhiteSpace(catalogPath) || !File.Exists(catalogPath))
        {
            return output;
        }

        var lines = File.ReadAllLines(catalogPath);
        if (lines.Length == 0)
        {
            return output;
        }

        var headers = ParseCsvLine(lines[0]);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            headerIndex[headers[i]] = i;
        }

        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var values = ParseCsvLine(lines[i]);
            var relative = GetCsv(values, headerIndex, "source_relative_path");
            var modelPath = GetCsv(values, headerIndex, "model_path");
            if (string.IsNullOrWhiteSpace(relative) || string.IsNullOrWhiteSpace(modelPath))
            {
                continue;
            }

            output.Add(new CatalogRow(
                GetCsv(values, headerIndex, "partId"),
                GetCsv(values, headerIndex, "slot"),
                GetCsv(values, headerIndex, "category"),
                relative,
                modelPath,
                lines[i]));
        }

        return output;
    }

    private static Dictionary<string, string> LoadCatalogModelPaths(string? catalogPath)
    {
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(catalogPath) || !File.Exists(catalogPath))
        {
            return output;
        }

        var lines = File.ReadAllLines(catalogPath);
        if (lines.Length == 0)
        {
            return output;
        }

        var headers = ParseCsvLine(lines[0]);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            headerIndex[headers[i]] = i;
        }

        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var values = ParseCsvLine(lines[i]);
            var relative = GetCsv(values, headerIndex, "source_relative_path");
            var modelPath = GetCsv(values, headerIndex, "model_path");
            if (string.IsNullOrWhiteSpace(relative) || string.IsNullOrWhiteSpace(modelPath))
            {
                continue;
            }

            output[NormalizeRelativePath(relative)] = modelPath;
        }

        return output;
    }

    private static string NormalizeRelativePath(string? path)
    {
        return (path ?? "")
            .Trim()
            .Replace('\\', '/');
    }

    private static string GetCsv(
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, int> headerIndex,
        string key)
    {
        return headerIndex.TryGetValue(key, out var index) && index >= 0 && index < values.Count
            ? values[index]
            : "";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(builder.ToString());
                builder.Clear();
            }
            else
            {
                builder.Append(c);
            }
        }

        values.Add(builder.ToString());
        return values;
    }

    private static NodeTransformResult FindNodeTransforms(string relativePath, byte[] bytes)
    {
        var frameMarkers = FindAsciiMarkers(bytes, "4294901778d{");
        if (frameMarkers.Count == 0)
        {
            return NodeTransformResult.Empty;
        }

        var events = new List<StructureEvent>();
        events.AddRange(frameMarkers.Select(position => new StructureEvent(position, StructureEventKind.Frame)));
        events.AddRange(FindAsciiMarkers(bytes, "4294901781d{")
            .Select(position => new StructureEvent(position, StructureEventKind.Mesh)));
        events.AddRange(FindAsciiMarkers(bytes, "4294901766d")
            .Select(position => new StructureEvent(position, StructureEventKind.Close)));
        events.Sort(static (left, right) => left.Position.CompareTo(right.Position));

        var output = new List<NodeTransform>();
        var stack = new List<NodeTransform>();
        var afterMesh = false;
        var meshCloseSeen = false;

        foreach (var structureEvent in events)
        {
            switch (structureEvent.Kind)
            {
                case StructureEventKind.Frame:
                {
                    var local = TryReadNodeTransform(bytes, structureEvent.Position);
                    if (local == null)
                    {
                        afterMesh = false;
                        meshCloseSeen = false;
                        continue;
                    }

                    var parentIndex = stack.Count == 0
                        ? -1
                        : output.FindLastIndex(node => ReferenceEquals(node, stack[^1]));
                    var world = stack.Count == 0
                        ? local.Matrix
                        : MultiplyMatrices(stack[^1].Matrix, local.Matrix);
                    var node = new NodeTransform(local.Name, local.Start, bytes.Length, world, local.Matrix, parentIndex);
                    output.Add(node);
                    stack.Add(node);
                    afterMesh = false;
                    meshCloseSeen = false;
                    break;
                }

                case StructureEventKind.Mesh:
                    afterMesh = true;
                    meshCloseSeen = false;
                    break;

                case StructureEventKind.Close:
                    if (!afterMesh)
                    {
                        break;
                    }

                    if (!meshCloseSeen)
                    {
                        meshCloseSeen = true;
                        break;
                    }

                    if (stack.Count == 0)
                    {
                        break;
                    }

                    var closed = stack[^1];
                    stack.RemoveAt(stack.Count - 1);
                    var outputIndex = output.FindLastIndex(node => ReferenceEquals(node, closed));
                    if (outputIndex >= 0)
                    {
                        output[outputIndex] = closed with { End = structureEvent.Position };
                    }

                    break;
            }
        }

        return RepairSeparatedLegHierarchy(relativePath, output);
    }

    private static NodeTransformResult RepairSeparatedLegHierarchy(
        string relativePath,
        List<NodeTransform> nodeTransforms)
    {
        if (!IsLegPartRelativePath(relativePath) ||
            nodeTransforms.Count == 0)
        {
            return new NodeTransformResult(nodeTransforms, nodeTransforms, "", "");
        }

        var legsIndex = nodeTransforms.FindIndex(static node => node.Name == "legs");
        if (legsIndex < 0)
        {
            return new NodeTransformResult(nodeTransforms, nodeTransforms, "", "");
        }

        var parents = nodeTransforms.Select(static node => node.ParentIndex).ToArray();
        var changed = false;
        var reasons = new List<string>();
        for (var i = 0; i < nodeTransforms.Count; i++)
        {
            if (i == legsIndex ||
                parents[i] >= 0 ||
                IsModelRootNodeName(nodeTransforms[i].Name))
            {
                continue;
            }

            parents[i] = legsIndex;
            changed = true;
            reasons.Add(nodeTransforms[i].Name + " orphan->legs");
        }

        for (var i = 0; i < nodeTransforms.Count; i++)
        {
            if (!UsesRootChildBelowAssemblyRepair(relativePath))
            {
                break;
            }

            var parentIndex = parents[i];
            if (i == legsIndex ||
                parentIndex < 0 ||
                (!IsModelRootNodeName(nodeTransforms[parentIndex].Name) &&
                 !UsesAnyBelowAssemblyRepair(relativePath)) ||
                IsModelRootNodeName(nodeTransforms[i].Name))
            {
                continue;
            }

            var world = ComputeNodeWorldMatrix(nodeTransforms, parents, i);
            if (world[7] >= -0.5f)
            {
                continue;
            }

            parents[i] = legsIndex;
            changed = true;
            reasons.Add(nodeTransforms[i].Name + " root-child-below-assembly->legs");
        }

        for (var i = 0; i < nodeTransforms.Count; i++)
        {
            var local = nodeTransforms[i].LocalMatrix;
            var tx = local[3];
            if (Math.Abs(tx) < 0.05f)
            {
                continue;
            }

            for (var j = i - 1; j >= 0; j--)
            {
                var candidateLocal = nodeTransforms[j].LocalMatrix;
                if (Math.Abs(Math.Abs(tx) - Math.Abs(candidateLocal[3])) > 0.0005f ||
                    tx * candidateLocal[3] >= 0f ||
                    Math.Abs(local[7] - candidateLocal[7]) > 0.0005f ||
                    Math.Abs(local[11] - candidateLocal[11]) > 0.0005f)
                {
                    continue;
                }

                var candidateParent = parents[j];
                if (candidateParent < 0 ||
                    parents[i] == candidateParent ||
                    WouldCreateParentCycle(parents, i, candidateParent))
                {
                    break;
                }

                var currentWorld = ComputeNodeWorldMatrix(nodeTransforms, parents, i);
                var candidateWorld = ComputeNodeWorldMatrix(nodeTransforms, parents, j);
                if (Math.Abs(currentWorld[7] - candidateWorld[7]) <= 0.05f &&
                    Math.Abs(currentWorld[11] - candidateWorld[11]) <= 0.05f)
                {
                    break;
                }

                parents[i] = candidateParent;
                changed = true;
                reasons.Add(nodeTransforms[i].Name + " mirror-parent->" + nodeTransforms[candidateParent].Name);
                break;
            }
        }

        if (!changed)
        {
            return new NodeTransformResult(nodeTransforms, nodeTransforms, "", "");
        }

        var repaired = new List<NodeTransform>(nodeTransforms.Count);
        for (var i = 0; i < nodeTransforms.Count; i++)
        {
            var matrix = ComputeNodeWorldMatrix(nodeTransforms, parents, i);
            repaired.Add(nodeTransforms[i] with
            {
                Matrix = matrix,
                ParentIndex = parents[i]
            });
        }

        return new NodeTransformResult(
            repaired,
            nodeTransforms,
            "legs_orphan_and_mirror_bounds_repair",
            string.Join("; ", reasons));
    }

    private static bool IsModelRootNodeName(string name)
    {
        return name.Contains(":\\", StringComparison.Ordinal) ||
               name.EndsWith(".gx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLegPartRelativePath(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        var stem = Path.GetFileNameWithoutExtension(normalized);
        return normalized.Contains("/legs", StringComparison.OrdinalIgnoreCase) ||
               stem.Contains("legs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesRootChildBelowAssemblyRepair(string relativePath)
    {
        var stem = Path.GetFileNameWithoutExtension(relativePath);
        return stem.Equals("legs34_dpns", StringComparison.OrdinalIgnoreCase) ||
               stem.Equals("n_legs42_krr", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesAnyBelowAssemblyRepair(string relativePath)
    {
        var stem = Path.GetFileNameWithoutExtension(relativePath);
        return stem.Equals("n_legs42_krr", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesUnassignedMeshLegsTransformRepair(string relativePath)
    {
        var stem = Path.GetFileNameWithoutExtension(relativePath);
        return stem.Equals("legs50_pps", StringComparison.OrdinalIgnoreCase) ||
               stem.Equals("g_legs58_pps", StringComparison.OrdinalIgnoreCase);
    }

    private static float[]? GetBelowAssemblyFallbackTransform(
        string relativePath,
        NodeTransform transform,
        IReadOnlyList<NodeTransform> nodeTransforms)
    {
        var stem = Path.GetFileNameWithoutExtension(relativePath);
        if (!stem.Equals("n_legs42_krr", StringComparison.OrdinalIgnoreCase) ||
            transform.Matrix[7] >= -0.5f ||
            transform.Name == "legs" ||
            IsModelRootNodeName(transform.Name))
        {
            return null;
        }

        var legs = nodeTransforms.FirstOrDefault(static node => node.Name == "legs");
        return legs == null
            ? null
            : MultiplyMatrices(legs.Matrix, transform.LocalMatrix);
    }

    private static float[] ComputeNodeWorldMatrix(
        IReadOnlyList<NodeTransform> nodes,
        IReadOnlyList<int> parents,
        int index)
    {
        var visited = new HashSet<int>();
        var chain = new Stack<int>();
        var cursor = index;
        while (cursor >= 0)
        {
            if (!visited.Add(cursor))
            {
                return nodes[index].Matrix;
            }

            chain.Push(cursor);
            cursor = parents[cursor];
        }

        float[]? output = null;
        while (chain.Count > 0)
        {
            var item = chain.Pop();
            output = output is null
                ? nodes[item].LocalMatrix
                : MultiplyMatrices(output, nodes[item].LocalMatrix);
        }

        return output ?? nodes[index].LocalMatrix;
    }

    private static bool WouldCreateParentCycle(IReadOnlyList<int> parents, int nodeIndex, int candidateParent)
    {
        var visited = new HashSet<int>();
        var cursor = candidateParent;
        while (cursor >= 0)
        {
            if (cursor == nodeIndex || !visited.Add(cursor))
            {
                return true;
            }

            cursor = parents[cursor];
        }

        return false;
    }

    private static NodeTransform? TryReadNodeTransform(byte[] bytes, int marker)
    {
        var cursor = marker + 12;
        if (cursor + 4 > bytes.Length)
        {
            return null;
        }

        var nameLength = ReadInt32(bytes, cursor);
        var matrixOffset = cursor + 4 + nameLength;
        if (nameLength < 0 || matrixOffset + 64 > bytes.Length)
        {
            return null;
        }

        var nameBytes = new byte[nameLength];
        Array.Copy(bytes, cursor + 4, nameBytes, 0, nameLength);
        var zero = Array.IndexOf(nameBytes, (byte)0);
        var name = zero >= 0
            ? Encoding.ASCII.GetString(nameBytes, 0, zero)
            : Encoding.ASCII.GetString(nameBytes);
        var matrix = new float[16];
        for (var m = 0; m < matrix.Length; m++)
        {
            matrix[m] = ReadSingle(bytes, matrixOffset + m * 4);
        }

        return new NodeTransform(name, marker, bytes.Length, matrix, matrix, -1);
    }

    private static List<int> FindAsciiMarkers(byte[] bytes, string marker)
    {
        var markerBytes = Encoding.ASCII.GetBytes(marker);
        var positions = new List<int>();
        for (var i = 0; i <= bytes.Length - markerBytes.Length; i++)
        {
            var matches = true;
            for (var j = 0; j < markerBytes.Length; j++)
            {
                if (bytes[i + j] == markerBytes[j])
                {
                    continue;
                }

                matches = false;
                break;
            }

            if (matches)
            {
                positions.Add(i);
            }
        }

        return positions;
    }

    private static List<MeshData> ApplyNodeTransforms(
        string relativePath,
        IReadOnlyList<MeshData> meshes,
        IReadOnlyList<NodeTransform> nodeTransforms)
    {
        var output = new List<MeshData>(meshes.Count);
        var unassignedFallbackTransform = UsesUnassignedMeshLegsTransformRepair(relativePath)
            ? nodeTransforms.FirstOrDefault(static node => node.Name == "legs")
            : null;
        foreach (var mesh in meshes)
        {
            var headerStart = mesh.PositionStart - 16;
            var transform = nodeTransforms
                .LastOrDefault(node => node.Start < headerStart && headerStart < node.End);
            if (unassignedFallbackTransform != null &&
                transform != null &&
                IsModelRootNodeName(transform.Name))
            {
                transform = unassignedFallbackTransform;
            }

            if (transform == null)
            {
                output.Add(unassignedFallbackTransform == null
                    ? mesh
                    : ApplyNodeTransform(mesh, unassignedFallbackTransform.Matrix));
                continue;
            }

            var matrix = GetBelowAssemblyFallbackTransform(relativePath, transform, nodeTransforms) ??
                         transform.Matrix;
            output.Add(ApplyNodeTransform(mesh, matrix));
        }

        return output;
    }

    private static float[] MultiplyMatrices(IReadOnlyList<float> parent, IReadOnlyList<float> child)
    {
        var output = new float[16];
        for (var row = 0; row < 4; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                var value = 0f;
                for (var index = 0; index < 4; index++)
                {
                    value += parent[row * 4 + index] * child[index * 4 + column];
                }

                output[row * 4 + column] = value;
            }
        }

        return output;
    }

    private static MeshData ApplyNodeTransform(MeshData mesh, IReadOnlyList<float> matrix)
    {
        var positions = new Vector3[mesh.Positions.Length];
        for (var i = 0; i < mesh.Positions.Length; i++)
        {
            positions[i] = TransformPosition(mesh.Positions[i], matrix);
        }

        var normals = new Vector3[mesh.Normals.Length];
        for (var i = 0; i < mesh.Normals.Length; i++)
        {
            normals[i] = Normalize(TransformDirection(mesh.Normals[i], matrix));
        }

        return new MeshData(
            mesh.PositionStart,
            mesh.NormalStart,
            mesh.UvStart,
            mesh.IndexStart,
            positions,
            normals,
            mesh.Uvs,
            mesh.Indices);
    }

    private static Vector3 TransformPosition(Vector3 value, IReadOnlyList<float> matrix)
    {
        return new Vector3(
            value.X * matrix[0] + value.Y * matrix[1] + value.Z * matrix[2] + matrix[3],
            value.X * matrix[4] + value.Y * matrix[5] + value.Z * matrix[6] + matrix[7],
            value.X * matrix[8] + value.Y * matrix[9] + value.Z * matrix[10] + matrix[11]);
    }

    private static Vector3 TransformDirection(Vector3 value, IReadOnlyList<float> matrix)
    {
        return new Vector3(
            value.X * matrix[0] + value.Y * matrix[1] + value.Z * matrix[2],
            value.X * matrix[4] + value.Y * matrix[5] + value.Z * matrix[6],
            value.X * matrix[8] + value.Y * matrix[9] + value.Z * matrix[10]);
    }

    private static Vector3 Normalize(Vector3 value)
    {
        var length = Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
        return length <= 0.000001
            ? value
            : new Vector3(
                (float)(value.X / length),
                (float)(value.Y / length),
                (float)(value.Z / length));
    }

    private static List<MeshData> FindMeshes(byte[] bytes)
    {
        var meshes = new List<MeshData>();
        var occupied = new List<(int Start, int End)>();
        for (var headerStart = 0; headerStart <= bytes.Length - 16; headerStart++)
        {
            if (ReadInt32(bytes, headerStart) != 1 || ReadInt32(bytes, headerStart + 4) != 0)
            {
                continue;
            }

            var vertexCount = ReadInt32(bytes, headerStart + 8);
            var indexCount = ReadInt32(bytes, headerStart + 12);
            if (vertexCount < 3 ||
                vertexCount > MaxVertexCount ||
                indexCount < 3 ||
                indexCount > MaxIndexCount ||
                indexCount % 3 != 0)
            {
                continue;
            }

            var positionStart = headerStart + 16;
            var normalStart = positionStart + vertexCount * 12;
            var uvStart = positionStart + vertexCount * 24;
            var indexStart = positionStart + vertexCount * 32;
            var end = indexStart + indexCount * 2;
            if (end > bytes.Length)
            {
                continue;
            }

            if (occupied.Any(range => positionStart < range.End && end > range.Start))
            {
                continue;
            }

            if (!ValidatePositions(bytes, positionStart, vertexCount, out _) ||
                !ValidateNormals(bytes, normalStart, vertexCount) ||
                !ValidateUvs(bytes, uvStart, vertexCount) ||
                !ValidateIndices(bytes, indexStart, indexCount, vertexCount))
            {
                continue;
            }

            meshes.Add(ReadMesh(bytes, positionStart, normalStart, uvStart, indexStart, vertexCount, indexCount));
            occupied.Add((positionStart, end));
        }

        return meshes;
    }

    private static MeshData? FindBestMesh(byte[] bytes)
    {
        MeshData? best = null;
        var bestScore = double.NegativeInfinity;

        for (var indexStart = 0; indexStart <= bytes.Length - 12; indexStart++)
        {
            var maxPotentialIndexCount = CountPotentialIndices(bytes, indexStart);
            if (maxPotentialIndexCount < 6)
            {
                continue;
            }

            maxPotentialIndexCount -= maxPotentialIndexCount % 3;
            if (maxPotentialIndexCount < 6)
            {
                continue;
            }

            var maxIndex = 0;
            var uniqueIndices = new HashSet<int>();
            var evaluatedVertexCounts = new HashSet<int>();
            for (var indexCount = 1; indexCount <= maxPotentialIndexCount; indexCount++)
            {
                var value = ReadUInt16(bytes, indexStart + (indexCount - 1) * 2);
                maxIndex = Math.Max(maxIndex, value);
                uniqueIndices.Add(value);

                if (indexCount < 6 || indexCount % 3 != 0)
                {
                    continue;
                }

                var vertexCount = maxIndex + 1;
                if (vertexCount < 3 || vertexCount > MaxVertexCount)
                {
                    continue;
                }

                if (vertexCount > indexCount)
                {
                    continue;
                }

                if (!evaluatedVertexCounts.Add(vertexCount))
                {
                    continue;
                }

                var uniqueIndexCount = uniqueIndices.Count;
                if (uniqueIndexCount < Math.Max(3, vertexCount * 2 / 3))
                {
                    continue;
                }

                var positionStart = indexStart - vertexCount * 32;
                if (positionStart < 0)
                {
                    continue;
                }

                var normalStart = positionStart + vertexCount * 12;
                var uvStart = positionStart + vertexCount * 24;
                if (uvStart + vertexCount * 8 > indexStart)
                {
                    continue;
                }

                if (!ValidatePositions(bytes, positionStart, vertexCount, out var span))
                {
                    continue;
                }

                if (!ValidateNormals(bytes, normalStart, vertexCount))
                {
                    continue;
                }

                if (!ValidateUvs(bytes, uvStart, vertexCount))
                {
                    continue;
                }

                var utilization = uniqueIndexCount / (double)vertexCount;
                var score = indexCount + vertexCount * utilization + span * 0.05;
                if (indexStart >= 2 && ReadUInt16(bytes, indexStart - 2) <= maxIndex)
                {
                    score -= 1000;
                }

                score -= indexStart * 0.000001;
                if (score <= bestScore)
                {
                    continue;
                }

                best = ReadMesh(bytes, positionStart, normalStart, uvStart, indexStart, vertexCount, indexCount);
                bestScore = score;
            }
        }

        return best;
    }

    private static MeshData ReadMesh(
        byte[] bytes,
        int positionStart,
        int normalStart,
        int uvStart,
        int indexStart,
        int vertexCount,
        int indexCount)
    {
        var positions = ReadVector3Array(bytes, positionStart, vertexCount);
        var normals = ReadVector3Array(bytes, normalStart, vertexCount);
        var uvs = ReadVector2Array(bytes, uvStart, vertexCount);
        var indices = new int[indexCount];
        for (var i = 0; i < indexCount; i++)
        {
            indices[i] = ReadUInt16(bytes, indexStart + i * 2);
        }

        return new MeshData(positionStart, normalStart, uvStart, indexStart, positions, normals, uvs, indices);
    }

    private static int CountPotentialIndices(byte[] bytes, int offset)
    {
        var count = 0;
        var maxProbe = Math.Min(MaxIndexCount, (bytes.Length - offset) / 2);
        for (var i = 0; i < maxProbe; i++)
        {
            var value = ReadUInt16(bytes, offset + i * 2);
            if (value > MaxVertexCount)
            {
                break;
            }

            count++;
        }

        return count;
    }

    private static int CountUniqueIndices(byte[] bytes, int offset, int indexCount)
    {
        var values = new HashSet<int>();
        for (var i = 0; i < indexCount; i++)
        {
            values.Add(ReadUInt16(bytes, offset + i * 2));
        }

        return values.Count;
    }

    private static MeshSelection SelectMeshesForExport(
        string relativePath,
        IReadOnlyList<MeshData> meshes,
        XfiInfo xfiInfo,
        IReadOnlyList<NodeTransform> nodeTransforms)
    {
        if (!IsUnitLegsPath(relativePath) || meshes.Count == 0)
        {
            return new MeshSelection(
                meshes.ToArray(),
                "gx_mesh_headers",
                FormatMeshList(meshes),
                "",
                "",
                "",
                "",
                "",
                Array.Empty<MeshData>());
        }

        var kept = new List<MeshData>();
        var direction = new List<MeshData>();
        foreach (var mesh in meshes)
        {
            if (IsLegDirectionHelperMesh(relativePath, mesh) ||
                IsDetachedUnassignedLegMesh(relativePath, mesh, nodeTransforms))
            {
                direction.Add(mesh);
                continue;
            }

            kept.Add(mesh);
        }

        if (kept.Count == 0)
        {
            kept.AddRange(meshes);
            direction.Clear();
        }

        var parserMode = xfiInfo.Exists
            ? "gx_xfi_leg_assembly"
            : "gx_leg_assembly";

        return new MeshSelection(
            kept,
            parserMode,
            FormatMeshList(kept),
            FormatMeshList(direction),
            FormatMeshList(direction),
            FormatMeshDiagnostics(kept),
            FormatMeshDiagnostics(direction),
            FormatMeshDiagnostics(direction),
            direction);
    }

    private static bool IsUnitLegsPath(string relativePath)
    {
        var stem = Path.GetFileNameWithoutExtension(relativePath);
        return Regex.IsMatch(stem, @"^(?:g_|n_|s_|ss0_)?legs\d+", RegexOptions.IgnoreCase);
    }

    private static bool IsLegDirectionHelperMesh(string relativePath, MeshData mesh)
    {
        var triangles = mesh.IndexCount / 3;
        return triangles <= 12 && CalculateSpan(mesh) < LegHelperSpanThreshold ||
               IsSpiderDetachedHelperPlane(relativePath, mesh);
    }

    private static bool IsSpiderDetachedHelperPlane(string relativePath, MeshData mesh)
    {
        var stem = Path.GetFileNameWithoutExtension(relativePath);
        if (!stem.Equals("legs20_spod", StringComparison.OrdinalIgnoreCase) &&
            !stem.Equals("s_legs33_spod", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var triangles = mesh.IndexCount / 3;
        if (mesh.VertexCount != 4 || triangles != 2)
        {
            return false;
        }

        GetBounds(new[] { mesh }, out var min, out var max);
        return min.Y <= -0.9f && Math.Abs(max.Z - min.Z) <= 0.0001f;
    }

    private static bool IsDetachedUnassignedLegMesh(
        string relativePath,
        MeshData mesh,
        IReadOnlyList<NodeTransform> nodeTransforms)
    {
        var stem = Path.GetFileNameWithoutExtension(relativePath);
        if (!stem.Equals("legs23_tk", StringComparison.OrdinalIgnoreCase) &&
            !stem.Equals("s_legs30_tk", StringComparison.OrdinalIgnoreCase) &&
            !stem.Equals("legs49_otrs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (nodeTransforms.Count == 0)
        {
            return false;
        }

        var headerStart = mesh.PositionStart - 16;
        return !nodeTransforms.Any(node => node.Start < headerStart && headerStart < node.End);
    }

    private static string FormatMeshList(IReadOnlyList<MeshData> meshes)
    {
        return string.Join(";", meshes.Select(mesh =>
            "0x" + mesh.PositionStart.ToString("X", Invariant) +
            ":v" + mesh.VertexCount.ToString(Invariant) +
            ":t" + (mesh.IndexCount / 3).ToString(Invariant)));
    }

    private static string FormatMeshDiagnostics(IReadOnlyList<MeshData> meshes)
    {
        return string.Join(";", meshes.Select(mesh =>
            "0x" + mesh.PositionStart.ToString("X", Invariant) +
            ":v" + mesh.VertexCount.ToString(Invariant) +
            ":t" + (mesh.IndexCount / 3).ToString(Invariant) +
            ":span" + CalculateSpan(mesh).ToString("0.######", Invariant) +
            ":bounds" + FormatBounds(new[] { mesh })));
    }

    private static int CountLargeDroppedBlocks(IReadOnlyList<MeshData> meshes)
    {
        return meshes.Count(mesh => CalculateSpan(mesh) >= LegHelperSpanThreshold);
    }

    private static string FormatBounds(IReadOnlyList<MeshData> meshes)
    {
        if (meshes.Count == 0)
        {
            return "";
        }

        GetBounds(meshes, out var min, out var max);
        return FormatVector(min) + "|" + FormatVector(max);
    }

    private static void GetBounds(IReadOnlyList<MeshData> meshes, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        foreach (var mesh in meshes)
        {
            foreach (var position in mesh.Positions)
            {
                min = new Vector3(
                    Math.Min(min.X, position.X),
                    Math.Min(min.Y, position.Y),
                    Math.Min(min.Z, position.Z));
                max = new Vector3(
                    Math.Max(max.X, position.X),
                    Math.Max(max.Y, position.Y),
                    Math.Max(max.Z, position.Z));
            }
        }
    }

    private static float CalculateSpan(MeshData mesh)
    {
        GetBounds(new[] { mesh }, out var min, out var max);
        return (max.X - min.X) + (max.Y - min.Y) + (max.Z - min.Z);
    }

    private static string FormatVector(Vector3 value)
    {
        return value.X.ToString("0.######", Invariant) + ";" +
               value.Y.ToString("0.######", Invariant) + ";" +
               value.Z.ToString("0.######", Invariant);
    }

    private static XfiInfo ReadXfiInfo(string gxPath)
    {
        var directory = Path.GetDirectoryName(gxPath) ?? "";
        var stem = Path.GetFileNameWithoutExtension(gxPath);
        var xfiPath = Path.Combine(directory, stem + ".xfi");
        if (!File.Exists(xfiPath))
        {
            xfiPath = Path.Combine(directory, stem + ".XFI");
        }

        if (!File.Exists(xfiPath))
        {
            return XfiInfo.Missing;
        }

        var lines = File.ReadAllLines(xfiPath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var cursor = 1;
        var transformRows = 0;
        while (cursor + 3 < lines.Length &&
               IsFloat4Line(lines[cursor]) &&
               IsFloat4Line(lines[cursor + 1]) &&
               IsFloat4Line(lines[cursor + 2]) &&
               IsFloat4Line(lines[cursor + 3]))
        {
            transformRows += 4;
            cursor += 4;
        }

        var directionCount = 0;
        if (cursor < lines.Length && IsIntegerLine(lines[cursor]))
        {
            directionCount = int.Parse(SplitColumns(lines[cursor])[0], Invariant);
        }

        return new XfiInfo(true, transformRows / 4, directionCount);
    }

    private static bool IsIntegerLine(string line)
    {
        var columns = SplitColumns(line);
        return columns.Length == 1 && int.TryParse(columns[0], out _);
    }

    private static bool IsFloat4Line(string line)
    {
        var columns = SplitColumns(line);
        if (columns.Length != 4)
        {
            return false;
        }

        return columns.All(column =>
            double.TryParse(column, NumberStyles.Float, Invariant, out _));
    }

    private static string[] SplitColumns(string line)
    {
        return line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool ValidateIndices(byte[] bytes, int offset, int indexCount, int vertexCount)
    {
        var unique = new HashSet<int>();
        for (var i = 0; i < indexCount; i++)
        {
            var value = ReadUInt16(bytes, offset + i * 2);
            if (value >= vertexCount)
            {
                return false;
            }

            unique.Add(value);
        }

        return unique.Count >= Math.Max(3, vertexCount / 2);
    }

    private static bool ValidatePositions(byte[] bytes, int offset, int vertexCount, out double span)
    {
        span = 0;
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var minZ = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var maxZ = double.NegativeInfinity;

        for (var i = 0; i < vertexCount; i++)
        {
            var x = ReadSingle(bytes, offset + i * 12);
            var y = ReadSingle(bytes, offset + i * 12 + 4);
            var z = ReadSingle(bytes, offset + i * 12 + 8);
            if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z))
            {
                return false;
            }

            if (Math.Abs(x) > 100000 || Math.Abs(y) > 100000 || Math.Abs(z) > 100000)
            {
                return false;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            minZ = Math.Min(minZ, z);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            maxZ = Math.Max(maxZ, z);
        }

        span = (maxX - minX) + (maxY - minY) + (maxZ - minZ);
        return span > 0.0001;
    }

    private static bool ValidateNormals(byte[] bytes, int offset, int vertexCount)
    {
        var good = 0;
        for (var i = 0; i < vertexCount; i++)
        {
            var x = ReadSingle(bytes, offset + i * 12);
            var y = ReadSingle(bytes, offset + i * 12 + 4);
            var z = ReadSingle(bytes, offset + i * 12 + 8);
            if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z))
            {
                return false;
            }

            var len = Math.Sqrt(x * x + y * y + z * z);
            if (len is >= 0.4 and <= 1.6)
            {
                good++;
            }
        }

        return good >= Math.Max(3, vertexCount * 2 / 3);
    }

    private static bool ValidateUvs(byte[] bytes, int offset, int vertexCount)
    {
        var good = 0;
        for (var i = 0; i < vertexCount; i++)
        {
            var u = ReadSingle(bytes, offset + i * 8);
            var v = ReadSingle(bytes, offset + i * 8 + 4);
            if (!IsFinite(u) || !IsFinite(v))
            {
                return false;
            }

            if (u is >= -0.25f and <= 1.25f && v is >= -0.25f and <= 1.25f)
            {
                good++;
            }
        }

        return good >= Math.Max(3, vertexCount * 2 / 3);
    }

    private static Vector3[] ReadVector3Array(byte[] bytes, int offset, int count)
    {
        var output = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            output[i] = new Vector3(
                ReadSingle(bytes, offset + i * 12),
                ReadSingle(bytes, offset + i * 12 + 4),
                ReadSingle(bytes, offset + i * 12 + 8));
        }

        return output;
    }

    private static Vector2[] ReadVector2Array(byte[] bytes, int offset, int count)
    {
        var output = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            output[i] = new Vector2(
                ReadSingle(bytes, offset + i * 8),
                1f - ReadSingle(bytes, offset + i * 8 + 4));
        }

        return output;
    }

    private static void WriteObj(
        string objPath,
        string mtlPath,
        string materialName,
        IReadOnlyList<MeshData> meshes,
        string textureReference)
    {
        var mtlName = Path.GetFileName(mtlPath).Replace('\\', '/');
        using (var writer = new StreamWriter(objPath, false, new UTF8Encoding(false)))
        {
            writer.WriteLine("# Generated from Nova1492 GX by tools/nova1492/GxObjConverter");
            writer.WriteLine("mtllib " + mtlName);

            var vertexBase = 0;
            for (var meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
            {
                var mesh = meshes[meshIndex];
                writer.WriteLine("o " + materialName + "_mesh" + meshIndex.ToString("00", Invariant));

                foreach (var v in mesh.Positions)
                {
                    writer.WriteLine(FormatLine("v", v.X, v.Y, v.Z));
                }

                foreach (var uv in mesh.Uvs)
                {
                    writer.WriteLine(FormatLine("vt", uv.X, uv.Y));
                }

                foreach (var n in mesh.Normals)
                {
                    writer.WriteLine(FormatLine("vn", n.X, n.Y, n.Z));
                }

                writer.WriteLine("usemtl " + materialName);
                for (var i = 0; i + 2 < mesh.Indices.Length; i += 3)
                {
                    var a = vertexBase + mesh.Indices[i] + 1;
                    var b = vertexBase + mesh.Indices[i + 1] + 1;
                    var c = vertexBase + mesh.Indices[i + 2] + 1;
                    writer.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
                }

                vertexBase += mesh.VertexCount;
            }
        }

        using var mtl = new StreamWriter(mtlPath, false, new UTF8Encoding(false));
        mtl.WriteLine("# Generated from Nova1492 GX by tools/nova1492/GxObjConverter");
        mtl.WriteLine("newmtl " + materialName);
        mtl.WriteLine("Ka 0.588235 0.588235 0.588235");
        mtl.WriteLine("Kd 1.000000 1.000000 1.000000");
        mtl.WriteLine("Ks 0.000000 0.000000 0.000000");
        if (!string.IsNullOrEmpty(textureReference))
        {
            mtl.WriteLine("map_Kd " + textureReference.Replace('\\', '/'));
        }
    }

    private static string FormatLine(string prefix, params float[] values)
    {
        return prefix + " " + string.Join(" ", values.Select(v => v.ToString("0.######", Invariant)));
    }

    private static string? FindTextureForGx(string gxPath, byte[] bytes)
    {
        var directory = Path.GetDirectoryName(gxPath) ?? "";
        var baseName = Path.GetFileNameWithoutExtension(gxPath);
        foreach (var ext in new[] { ".BMP", ".bmp", ".TGA", ".tga", ".PNG", ".png", ".JPG", ".jpg" })
        {
            var sameBase = Path.Combine(directory, baseName + ext);
            if (File.Exists(sameBase))
            {
                return sameBase;
            }
        }

        var ascii = Encoding.ASCII.GetString(bytes);
        foreach (Match match in Regex.Matches(ascii, @"[A-Za-z0-9_\- .()가-힣]+?\.(?:BMP|bmp|TGA|tga|PNG|png|JPG|jpg)"))
        {
            var textureName = match.Value.Trim('\0', ' ', '\t', '\r', '\n');
            var candidate = ResolveTexturePath(directory, textureName);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveTexturePath(string directory, string textureName)
    {
        var exact = Path.Combine(directory, textureName);
        if (File.Exists(exact))
        {
            return exact;
        }

        var stem = Path.GetFileNameWithoutExtension(textureName);
        foreach (var ext in new[] { ".BMP", ".bmp", ".TGA", ".tga", ".PNG", ".png", ".JPG", ".jpg" })
        {
            var fallback = Path.Combine(directory, stem + ext);
            if (File.Exists(fallback))
            {
                return fallback;
            }
        }

        return null;
    }

    private static string AssessConversion(
        MeshSelection selection,
        string? texture,
        NodeTransformResult nodeTransforms)
    {
        if (selection.KeptMeshes.Count == 0)
        {
            return "failed";
        }

        if (texture is null)
        {
            return "needs_review";
        }

        if (nodeTransforms.RepairApplied)
        {
            return "repair_applied";
        }

        return "pass";
    }

    private static List<HierarchyDiagnosticRow> BuildHierarchyDiagnostics(
        string relativePath,
        string partId,
        IReadOnlyList<MeshData> meshes,
        NodeTransformResult nodeTransforms,
        string? texture,
        string assessment)
    {
        var rows = new List<HierarchyDiagnosticRow>();
        var nodes = nodeTransforms.Nodes;
        foreach (var mesh in meshes)
        {
            var headerStart = mesh.PositionStart - 16;
            var node = nodes.LastOrDefault(item => item.Start < headerStart && headerStart < item.End);
            rows.Add(new HierarchyDiagnosticRow(
                relativePath,
                partId,
                "0x" + headerStart.ToString("X", Invariant),
                mesh.VertexCount,
                mesh.IndexCount / 3,
                node?.Name ?? "",
                node is null ? "" : "0x" + node.Start.ToString("X", Invariant),
                node is null ? "" : "0x" + node.End.ToString("X", Invariant),
                node?.Matrix[3] ?? 0f,
                node?.Matrix[7] ?? 0f,
                node?.Matrix[11] ?? 0f,
                FormatBounds(new[] { mesh }),
                nodeTransforms.RepairRule,
                nodeTransforms.RepairReason,
                texture ?? "",
                assessment));
        }

        return rows;
    }

    private static Dictionary<string, PipelineStateRow> LoadPipelineState(string path)
    {
        var rows = new Dictionary<string, PipelineStateRow>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            return rows;
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            return rows;
        }

        var headers = ParseCsvLine(lines[0]);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            headerIndex[headers[i]] = i;
        }

        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var values = ParseCsvLine(lines[i]);
            var row = new PipelineStateRow(
                GetCsv(values, headerIndex, "source_relative_path"),
                GetCsv(values, headerIndex, "part_id"),
                GetCsv(values, headerIndex, "source_hash"),
                GetCsv(values, headerIndex, "catalog_row_hash"),
                GetCsv(values, headerIndex, "converter_version"),
                GetCsv(values, headerIndex, "status"),
                GetCsv(values, headerIndex, "assessment"),
                GetCsv(values, headerIndex, "obj_path"),
                GetCsv(values, headerIndex, "generated_at"));
            if (!string.IsNullOrWhiteSpace(row.SourceRelativePath))
            {
                rows[NormalizeRelativePath(row.SourceRelativePath)] = row;
            }
        }

        return rows;
    }

    private static void WritePipelineState(IReadOnlyList<PipelineStateRow> rows)
    {
        var mergedRows = LoadPipelineState(PipelineStatePath);
        foreach (var row in rows)
        {
            mergedRows[NormalizeRelativePath(row.SourceRelativePath)] = row;
        }

        using var writer = new StreamWriter(PipelineStatePath, false, new UTF8Encoding(false));
        writer.WriteLine("source_relative_path,part_id,source_hash,catalog_row_hash,converter_version,status,assessment,obj_path,generated_at");
        foreach (var row in mergedRows.Values.OrderBy(row => row.SourceRelativePath, StringComparer.OrdinalIgnoreCase))
        {
            writer.WriteLine(string.Join(",", new[]
            {
                Csv(row.SourceRelativePath),
                Csv(row.PartId),
                Csv(row.SourceHash),
                Csv(row.CatalogRowHash),
                Csv(row.ConverterVersion),
                Csv(row.Status),
                Csv(row.Assessment),
                Csv(row.ObjPath),
                Csv(row.GeneratedAt)
            }));
        }
    }

    private static void WriteHierarchyDiagnostics(IReadOnlyList<HierarchyDiagnosticRow> rows)
    {
        using (var writer = new StreamWriter(HierarchyDiagnosticsCsvPath, false, new UTF8Encoding(false)))
        {
            writer.WriteLine("source_relative_path,part_id,mesh_header,vertices,triangles,node,node_start,node_end,tx,ty,tz,bounds,repair_rule,repair_reason,texture_source,assessment");
            foreach (var row in rows)
            {
                writer.WriteLine(string.Join(",", new[]
                {
                    Csv(row.SourceRelativePath),
                    Csv(row.PartId),
                    Csv(row.MeshHeader),
                    Csv(row.Vertices.ToString(Invariant)),
                    Csv(row.Triangles.ToString(Invariant)),
                    Csv(row.Node),
                    Csv(row.NodeStart),
                    Csv(row.NodeEnd),
                    Csv(row.Tx.ToString("0.######", Invariant)),
                    Csv(row.Ty.ToString("0.######", Invariant)),
                    Csv(row.Tz.ToString("0.######", Invariant)),
                    Csv(row.Bounds),
                    Csv(row.RepairRule),
                    Csv(row.RepairReason),
                    Csv(row.TextureSource),
                    Csv(row.Assessment)
                }));
            }
        }

        using var md = new StreamWriter(HierarchyDiagnosticsMdPath, false, new UTF8Encoding(false));
        md.WriteLine("# Nova1492 GX Hierarchy Diagnostics");
        md.WriteLine();
        md.WriteLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.WriteLine();
        md.WriteLine($"- diagnostic rows: {rows.Count}");
        md.WriteLine($"- repaired rows: {rows.Count(row => !string.IsNullOrWhiteSpace(row.RepairRule))}");
        md.WriteLine($"- needs review rows: {rows.Count(row => row.Assessment == "needs_review")}");
        md.WriteLine($"- csv: `{HierarchyDiagnosticsCsvPath}`");
    }

    private static string ComputeFileHash(string path)
    {
        using var sha1 = SHA1.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(sha1.ComputeHash(stream)).ToLowerInvariant();
    }

    private static string ComputeStringHash(string value)
    {
        using var sha1 = SHA1.Create();
        return Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static void WriteManifest(
        List<ManifestRow> rows,
        string sourceRoot,
        string outputRoot,
        PipelineStage stage)
    {
        using (var writer = new StreamWriter(ManifestPath, false, new UTF8Encoding(false)))
        {
            writer.WriteLine("status,source_relative_path,source_path,bytes,obj_path,vertices,triangles,position_offset,normal_offset,uv_offset,index_offset,texture_source,texture_output,parser_mode,mesh_blocks,kept_block_count,assembly_blocks,direction_blocks,dropped_blocks,assembly_block_diagnostics,direction_block_diagnostics,dropped_block_diagnostics,large_dropped_block_count,bounds,xfi_transform_count,xfi_direction_range_count,source_hash,catalog_row_hash,converter_version,assessment,repair_rule,repair_reason,before_bounds,after_bounds,error");
            foreach (var row in rows)
            {
                writer.WriteLine(string.Join(",", new[]
                {
                    Csv(row.Status),
                    Csv(row.SourceRelativePath),
                    Csv(row.SourcePath),
                    Csv(row.Bytes.ToString(Invariant)),
                    Csv(row.ObjPath),
                    Csv(row.Vertices.ToString(Invariant)),
                    Csv(row.Triangles.ToString(Invariant)),
                    Csv(row.PositionOffset),
                    Csv(row.NormalOffset),
                    Csv(row.UvOffset),
                    Csv(row.IndexOffset),
                    Csv(row.TextureSource),
                    Csv(row.TextureOutput),
                    Csv(row.ParserMode),
                    Csv(row.MeshBlocks.ToString(Invariant)),
                    Csv(row.KeptBlockCount.ToString(Invariant)),
                    Csv(row.AssemblyBlocks),
                    Csv(row.DirectionBlocks),
                    Csv(row.DroppedBlocks),
                    Csv(row.AssemblyBlockDiagnostics),
                    Csv(row.DirectionBlockDiagnostics),
                    Csv(row.DroppedBlockDiagnostics),
                    Csv(row.LargeDroppedBlockCount.ToString(Invariant)),
                    Csv(row.Bounds),
                    Csv(row.XfiTransformCount.ToString(Invariant)),
                    Csv(row.XfiDirectionRangeCount.ToString(Invariant)),
                    Csv(row.SourceHash),
                    Csv(row.CatalogRowHash),
                    Csv(row.ConverterVersion),
                    Csv(row.Assessment),
                    Csv(row.RepairRule),
                    Csv(row.RepairReason),
                    Csv(row.BeforeBounds),
                    Csv(row.AfterBounds),
                    Csv(row.Error)
                }));
            }
        }

        var converted = rows.Count(row => row.Status is "converted" or "repaired");
        var analyzed = rows.Count(row => row.Status == "analyzed");
        var skipped = rows.Count(row => row.Status == "skipped");
        var failed = rows.Count(row => row.Status == "failed");
        using var md = new StreamWriter(SummaryPath, false, new UTF8Encoding(false));
        md.WriteLine("# Nova1492 GX Conversion Summary");
        md.WriteLine();
        md.WriteLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.WriteLine();
        md.WriteLine($"- stage: `{stage.ToString().ToLowerInvariant()}`");
        md.WriteLine($"- converter version: `{ConverterVersion}`");
        md.WriteLine($"- source root: `{sourceRoot}`");
        md.WriteLine($"- output root: `{outputRoot}`");
        md.WriteLine($"- total GX files: {rows.Count}");
        md.WriteLine($"- converted: {converted}");
        md.WriteLine($"- analyzed: {analyzed}");
        md.WriteLine($"- skipped: {skipped}");
        md.WriteLine($"- failed: {failed}");
        md.WriteLine($"- repair applied: {rows.Count(row => row.Assessment == "repair_applied")}");
        md.WriteLine($"- needs review: {rows.Count(row => row.Assessment == "needs_review")}");
        md.WriteLine($"- manifest: `{ManifestPath}`");
        md.WriteLine($"- pipeline state: `{PipelineStatePath}`");
        md.WriteLine();
        md.WriteLine("## Notes");
        md.WriteLine();
        md.WriteLine("- Conversion uses a heuristic parser for the confirmed GX layout: split position, normal, UV, and uint16 index streams.");
        md.WriteLine("- Unit legs use an XFI-aware assembly pass that drops tiny direction/helper mesh planes while preserving parsed assembly blocks.");
        md.WriteLine("- Catalog-driven runs can use --changed-only to skip rows whose source hash, catalog row hash, and converter version are unchanged.");
        md.WriteLine("- Hierarchy repairs are rule-based and recorded in repair_rule/repair_reason instead of hidden per-part sculpture.");
        md.WriteLine("- UV V is flipped during OBJ export to match the DAE comparison sample.");
        md.WriteLine("- Failed rows are preserved in the manifest for later parser improvements.");
    }

    private static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string BuildSafeName(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '_').Replace('/', '_');
        var stem = Path.GetFileNameWithoutExtension(normalized);
        var safe = Regex.Replace(stem, @"[^A-Za-z0-9_.-]+", "_").Trim('_');
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "gx_asset";
        }

        using var sha1 = SHA1.Create();
        var hash = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(relativePath))).Substring(0, 8).ToLowerInvariant();
        return safe + "_" + hash;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static float ReadSingle(byte[] bytes, int offset)
    {
        return BitConverter.ToSingle(bytes, offset);
    }

    private static ushort ReadUInt16(byte[] bytes, int offset)
    {
        return BitConverter.ToUInt16(bytes, offset);
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return BitConverter.ToInt32(bytes, offset);
    }

    private readonly record struct Vector2(float X, float Y);
    private readonly record struct Vector3(float X, float Y, float Z);

    private sealed record MeshData(
        int PositionStart,
        int NormalStart,
        int UvStart,
        int IndexStart,
        Vector3[] Positions,
        Vector3[] Normals,
        Vector2[] Uvs,
        int[] Indices)
    {
        public int VertexCount => Positions.Length;
        public int IndexCount => Indices.Length;
    }

    private sealed record NodeTransform(
        string Name,
        int Start,
        int End,
        float[] Matrix,
        float[] LocalMatrix,
        int ParentIndex);

    private readonly record struct StructureEvent(int Position, StructureEventKind Kind);

    private enum StructureEventKind
    {
        Frame,
        Mesh,
        Close,
    }

    private sealed record MeshSelection(
        IReadOnlyList<MeshData> KeptMeshes,
        string ParserMode,
        string AssemblyBlocks,
        string DirectionBlocks,
        string DroppedBlocks,
        string AssemblyBlockDiagnostics,
        string DirectionBlockDiagnostics,
        string DroppedBlockDiagnostics,
        IReadOnlyList<MeshData> DroppedMeshes);

    private readonly record struct XfiInfo(bool Exists, int TransformCount, int DirectionRangeCount)
    {
        public static readonly XfiInfo Missing = new(false, 0, 0);
    }

    private enum PipelineStage
    {
        Analyze,
        Convert,
    }

    private sealed record CatalogRow(
        string PartId,
        string Slot,
        string Category,
        string SourceRelativePath,
        string ModelPath,
        string RawLine);

    private sealed record NodeTransformResult(
        IReadOnlyList<NodeTransform> Nodes,
        IReadOnlyList<NodeTransform> OriginalNodes,
        string RepairRule,
        string RepairReason)
    {
        public static readonly NodeTransformResult Empty = new(
            Array.Empty<NodeTransform>(),
            Array.Empty<NodeTransform>(),
            "",
            "");

        public bool RepairApplied => !string.IsNullOrWhiteSpace(RepairRule);
    }

    private sealed record HierarchyDiagnosticRow(
        string SourceRelativePath,
        string PartId,
        string MeshHeader,
        int Vertices,
        int Triangles,
        string Node,
        string NodeStart,
        string NodeEnd,
        float Tx,
        float Ty,
        float Tz,
        string Bounds,
        string RepairRule,
        string RepairReason,
        string TextureSource,
        string Assessment);

    private sealed record PipelineStateRow(
        string SourceRelativePath,
        string PartId,
        string SourceHash,
        string CatalogRowHash,
        string ConverterVersion,
        string Status,
        string Assessment,
        string ObjPath,
        string GeneratedAt)
    {
        public static PipelineStateRow FromManifest(
            string sourceRelativePath,
            string partId,
            string sourceHash,
            string catalogRowHash,
            string objPath,
            string status,
            string assessment)
        {
            return new PipelineStateRow(
                sourceRelativePath,
                partId,
                sourceHash,
                catalogRowHash,
                Program.ConverterVersion,
                status,
                assessment,
                objPath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Invariant));
        }
    }

    private sealed record ManifestRow(
        string Status,
        string SourceRelativePath,
        string SourcePath,
        long Bytes,
        string ObjPath,
        int Vertices,
        int Triangles,
        string PositionOffset,
        string NormalOffset,
        string UvOffset,
        string IndexOffset,
        string TextureSource,
        string TextureOutput,
        string ParserMode,
        int MeshBlocks,
        int KeptBlockCount,
        string AssemblyBlocks,
        string DirectionBlocks,
        string DroppedBlocks,
        string AssemblyBlockDiagnostics,
        string DirectionBlockDiagnostics,
        string DroppedBlockDiagnostics,
        int LargeDroppedBlockCount,
        string Bounds,
        int XfiTransformCount,
        int XfiDirectionRangeCount,
        string SourceHash,
        string CatalogRowHash,
        string ConverterVersion,
        string Assessment,
        string RepairRule,
        string RepairReason,
        string BeforeBounds,
        string AfterBounds,
        string Error)
    {
        public static ManifestRow Success(
            string status,
            string relativePath,
            string sourcePath,
            long bytes,
            string objPath,
            int vertices,
            int triangles,
            string positionOffset,
            string normalOffset,
            string uvOffset,
            string indexOffset,
            string? textureSource,
            string textureOutput,
            string parserMode,
            int meshBlocks,
            int keptBlockCount,
            string assemblyBlocks,
            string directionBlocks,
            string droppedBlocks,
            string assemblyBlockDiagnostics,
            string directionBlockDiagnostics,
            string droppedBlockDiagnostics,
            int largeDroppedBlockCount,
            string bounds,
            int xfiTransformCount,
            int xfiDirectionRangeCount,
            string sourceHash,
            string catalogRowHash,
            string assessment,
            string repairRule,
            string repairReason,
            string beforeBounds,
            string afterBounds)
        {
            return new ManifestRow(
                status,
                relativePath,
                sourcePath,
                bytes,
                objPath,
                vertices,
                triangles,
                positionOffset,
                normalOffset,
                uvOffset,
                indexOffset,
                textureSource ?? "",
                textureOutput,
                parserMode,
                meshBlocks,
                keptBlockCount,
                assemblyBlocks,
                directionBlocks,
                droppedBlocks,
                assemblyBlockDiagnostics,
                directionBlockDiagnostics,
                droppedBlockDiagnostics,
                largeDroppedBlockCount,
                bounds,
                xfiTransformCount,
                xfiDirectionRangeCount,
                sourceHash,
                catalogRowHash,
                Program.ConverterVersion,
                assessment,
                repairRule,
                repairReason,
                beforeBounds,
                afterBounds,
                "");
        }

        public static ManifestRow Skipped(
            string relativePath,
            string sourcePath,
            long bytes,
            string objPath,
            string sourceHash,
            string catalogRowHash,
            string assessment,
            string reason)
        {
            return new ManifestRow(
                "skipped",
                relativePath,
                sourcePath,
                bytes,
                objPath,
                0,
                0,
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                0,
                0,
                "",
                "",
                "",
                "",
                "",
                "",
                0,
                "",
                0,
                0,
                sourceHash,
                catalogRowHash,
                Program.ConverterVersion,
                assessment,
                "",
                "",
                "",
                "",
                reason);
        }

        public static ManifestRow Failed(
            string relativePath,
            string sourcePath,
            long bytes,
            string sourceHash,
            string catalogRowHash,
            string error)
        {
            return new ManifestRow("failed", relativePath, sourcePath, bytes, "", 0, 0, "", "", "", "", "", "", "", 0, 0, "", "", "", "", "", "", 0, "", 0, 0, sourceHash, catalogRowHash, Program.ConverterVersion, "failed", "", "", "", "", error);
        }
    }
}
