using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

internal static class Program
{
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
        var catalogOnly = HasArg(args, "--catalog-only");
        var limitRaw = GetArg(args, "--limit");
        var limit = int.TryParse(limitRaw, out var parsedLimit) ? parsedLimit : int.MaxValue;
        var clean = HasArg(args, "--clean");
        var writeManifest = !HasArg(args, "--no-manifest");

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

        var catalogModelPaths = LoadCatalogModelPaths(catalogPath);
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
        else if (catalogOnly)
        {
            if (catalogModelPaths.Count == 0)
            {
                Console.Error.WriteLine("--catalog-only requires a catalog with source_relative_path/model_path rows.");
                return 2;
            }

            gxFilesQuery = gxFilesQuery.Where(path =>
                catalogModelPaths.ContainsKey(NormalizeRelativePath(Path.GetRelativePath(sourceRoot, path))));
        }

        var gxFiles = gxFilesQuery
            .Take(limit)
            .ToArray();

        var rows = new List<ManifestRow>();
        var success = 0;

        foreach (var gxPath in gxFiles)
        {
            var relative = Path.GetRelativePath(sourceRoot, gxPath);
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
                    rows.Add(ManifestRow.Failed(relative, gxPath, bytes.Length, "no_valid_mesh_stream"));
                    continue;
                }

                var nodeTransforms = FindNodeTransforms(bytes);
                if (nodeTransforms.Count > 0)
                {
                    meshes = ApplyNodeTransforms(relative, meshes, nodeTransforms);
                }

                var xfiInfo = ReadXfiInfo(gxPath);
                var selection = SelectMeshesForExport(relative, meshes, xfiInfo);
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

                WriteObj(objPath, mtlPath, safeName, selection.KeptMeshes, textureReference);

                if (texture is not null)
                {
                    File.Copy(texture, textureOutputPath, overwrite: true);
                }

                success++;
                rows.Add(ManifestRow.Converted(
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
                    xfiInfo.DirectionRangeCount));
            }
            catch (Exception ex)
            {
                rows.Add(ManifestRow.Failed(relative, gxPath, 0, ex.GetType().Name + ": " + ex.Message));
            }
        }

        if (writeManifest)
        {
            WriteManifest(rows);
        }

        Console.WriteLine($"GX files: {gxFiles.Length}");
        Console.WriteLine($"Converted: {success}");
        Console.WriteLine($"Failed: {gxFiles.Length - success}");
        Console.WriteLine(writeManifest
            ? "Manifest: artifacts/nova1492/gx_conversion_manifest.csv"
            : "Manifest: skipped (--no-manifest)");
        return success > 0 ? 0 : 1;
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

    private static List<NodeTransform> FindNodeTransforms(byte[] bytes)
    {
        var frameMarkers = FindAsciiMarkers(bytes, "4294901778d{");
        if (frameMarkers.Count == 0)
        {
            return new List<NodeTransform>();
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

                    var world = stack.Count == 0
                        ? local.Matrix
                        : MultiplyMatrices(stack[^1].Matrix, local.Matrix);
                    var node = new NodeTransform(local.Name, local.Start, bytes.Length, world);
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

        return output;
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

        return new NodeTransform(name, marker, bytes.Length, matrix);
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
        foreach (var mesh in meshes)
        {
            var headerStart = mesh.PositionStart - 16;
            var transform = nodeTransforms
                .LastOrDefault(node => node.Start < headerStart && headerStart < node.End);
            if (transform == null)
            {
                output.Add(mesh);
                continue;
            }

            output.Add(ApplyNodeTransform(mesh, transform.Matrix));
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
        XfiInfo xfiInfo)
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
            if (IsLegDirectionHelperMesh(mesh))
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

    private static bool IsLegDirectionHelperMesh(MeshData mesh)
    {
        var triangles = mesh.IndexCount / 3;
        return triangles <= 12 && CalculateSpan(mesh) < LegHelperSpanThreshold;
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

    private static void WriteManifest(List<ManifestRow> rows)
    {
        using (var writer = new StreamWriter("artifacts/nova1492/gx_conversion_manifest.csv", false, new UTF8Encoding(false)))
        {
            writer.WriteLine("status,source_relative_path,source_path,bytes,obj_path,vertices,triangles,position_offset,normal_offset,uv_offset,index_offset,texture_source,texture_output,parser_mode,mesh_blocks,kept_block_count,assembly_blocks,direction_blocks,dropped_blocks,assembly_block_diagnostics,direction_block_diagnostics,dropped_block_diagnostics,large_dropped_block_count,bounds,xfi_transform_count,xfi_direction_range_count,error");
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
                    Csv(row.Error)
                }));
            }
        }

        var converted = rows.Count(row => row.Status == "converted");
        var failed = rows.Count - converted;
        using var md = new StreamWriter("artifacts/nova1492/gx_conversion_summary.md", false, new UTF8Encoding(false));
        md.WriteLine("# Nova1492 GX Conversion Summary");
        md.WriteLine();
        md.WriteLine($"> generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.WriteLine();
        md.WriteLine($"- source root: `C:\\Program Files (x86)\\Nova1492`");
        md.WriteLine($"- output root: `Assets/Art/Nova1492/GXConverted`");
        md.WriteLine($"- total GX files: {rows.Count}");
        md.WriteLine($"- converted: {converted}");
        md.WriteLine($"- failed: {failed}");
        md.WriteLine("- manifest: `artifacts/nova1492/gx_conversion_manifest.csv`");
        md.WriteLine();
        md.WriteLine("## Notes");
        md.WriteLine();
        md.WriteLine("- Conversion uses a heuristic parser for the confirmed GX layout: split position, normal, UV, and uint16 index streams.");
        md.WriteLine("- Unit legs use an XFI-aware assembly pass that drops tiny direction/helper mesh planes while preserving parsed assembly blocks.");
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

    private sealed record NodeTransform(string Name, int Start, int End, float[] Matrix);

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
        string Error)
    {
        public static ManifestRow Converted(
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
            int xfiDirectionRangeCount)
        {
            return new ManifestRow(
                "converted",
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
                "");
        }

        public static ManifestRow Failed(string relativePath, string sourcePath, long bytes, string error)
        {
            return new ManifestRow("failed", relativePath, sourcePath, bytes, "", 0, 0, "", "", "", "", "", "", "", 0, 0, "", "", "", "", "", "", 0, "", 0, 0, error);
        }
    }
}
