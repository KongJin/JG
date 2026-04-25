using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

internal static class Program
{
    private const int MaxVertexCount = 10000;
    private const int MaxIndexCount = 200000;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static int Main(string[] args)
    {
        var sourceRoot = GetArg(args, "--source-root") ?? @"C:\Program Files (x86)\Nova1492";
        var outputRoot = GetArg(args, "--output-root") ?? "Assets/Art/Nova1492/GXConverted";
        var limitRaw = GetArg(args, "--limit");
        var limit = int.TryParse(limitRaw, out var parsedLimit) ? parsedLimit : int.MaxValue;
        var clean = HasArg(args, "--clean");

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

        var gxFiles = Directory.EnumerateFiles(sourceRoot, "*.gx", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(sourceRoot, "*.GX", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
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
                var mesh = FindBestMesh(bytes);
                if (mesh is null)
                {
                    rows.Add(ManifestRow.Failed(relative, gxPath, bytes.Length, "no_valid_mesh_stream"));
                    continue;
                }

                var safeName = BuildSafeName(relative);
                var objPath = Path.Combine(modelDir, safeName + ".obj");
                var mtlPath = Path.Combine(modelDir, safeName + ".mtl");
                var texture = FindTextureForGx(gxPath, bytes);
                var textureOutputName = texture is null ? "" : safeName + Path.GetExtension(texture);

                WriteObj(objPath, mtlPath, safeName, mesh, textureOutputName);

                if (texture is not null)
                {
                    File.Copy(texture, Path.Combine(textureDir, textureOutputName), overwrite: true);
                }

                success++;
                rows.Add(ManifestRow.Converted(
                    relative,
                    gxPath,
                    bytes.Length,
                    objPath,
                    mesh.VertexCount,
                    mesh.IndexCount / 3,
                    mesh.PositionStart,
                    mesh.NormalStart,
                    mesh.UvStart,
                    mesh.IndexStart,
                    texture,
                    textureOutputName));
            }
            catch (Exception ex)
            {
                rows.Add(ManifestRow.Failed(relative, gxPath, 0, ex.GetType().Name + ": " + ex.Message));
            }
        }

        WriteManifest(rows);
        Console.WriteLine($"GX files: {gxFiles.Length}");
        Console.WriteLine($"Converted: {success}");
        Console.WriteLine($"Failed: {gxFiles.Length - success}");
        Console.WriteLine("Manifest: artifacts/nova1492/gx_conversion_manifest.csv");
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

                var positions = ReadVector3Array(bytes, positionStart, vertexCount);
                var normals = ReadVector3Array(bytes, normalStart, vertexCount);
                var uvs = ReadVector2Array(bytes, uvStart, vertexCount);
                var indices = new int[indexCount];
                for (var i = 0; i < indexCount; i++)
                {
                    indices[i] = ReadUInt16(bytes, indexStart + i * 2);
                }

                best = new MeshData(positionStart, normalStart, uvStart, indexStart, positions, normals, uvs, indices);
                bestScore = score;
            }
        }

        return best;
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

    private static void WriteObj(string objPath, string mtlPath, string materialName, MeshData mesh, string textureName)
    {
        var mtlName = Path.GetFileName(mtlPath).Replace('\\', '/');
        using (var writer = new StreamWriter(objPath, false, new UTF8Encoding(false)))
        {
            writer.WriteLine("# Generated from Nova1492 GX by tools/nova1492/GxObjConverter");
            writer.WriteLine("mtllib " + mtlName);
            writer.WriteLine("o " + materialName);

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
                var a = mesh.Indices[i] + 1;
                var b = mesh.Indices[i + 1] + 1;
                var c = mesh.Indices[i + 2] + 1;
                writer.WriteLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
            }
        }

        using var mtl = new StreamWriter(mtlPath, false, new UTF8Encoding(false));
        mtl.WriteLine("# Generated from Nova1492 GX by tools/nova1492/GxObjConverter");
        mtl.WriteLine("newmtl " + materialName);
        mtl.WriteLine("Ka 0.588235 0.588235 0.588235");
        mtl.WriteLine("Kd 1.000000 1.000000 1.000000");
        mtl.WriteLine("Ks 0.000000 0.000000 0.000000");
        if (!string.IsNullOrEmpty(textureName))
        {
            mtl.WriteLine("map_Kd ../Textures/" + textureName.Replace('\\', '/'));
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
            var candidate = Path.Combine(directory, textureName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void WriteManifest(List<ManifestRow> rows)
    {
        using (var writer = new StreamWriter("artifacts/nova1492/gx_conversion_manifest.csv", false, new UTF8Encoding(false)))
        {
            writer.WriteLine("status,source_relative_path,source_path,bytes,obj_path,vertices,triangles,position_offset,normal_offset,uv_offset,index_offset,texture_source,texture_output,error");
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
        string Error)
    {
        public static ManifestRow Converted(
            string relativePath,
            string sourcePath,
            long bytes,
            string objPath,
            int vertices,
            int triangles,
            int positionOffset,
            int normalOffset,
            int uvOffset,
            int indexOffset,
            string? textureSource,
            string textureOutput)
        {
            return new ManifestRow(
                "converted",
                relativePath,
                sourcePath,
                bytes,
                objPath,
                vertices,
                triangles,
                "0x" + positionOffset.ToString("X", Invariant),
                "0x" + normalOffset.ToString("X", Invariant),
                "0x" + uvOffset.ToString("X", Invariant),
                "0x" + indexOffset.ToString("X", Invariant),
                textureSource ?? "",
                textureOutput,
                "");
        }

        public static ManifestRow Failed(string relativePath, string sourcePath, long bytes, string error)
        {
            return new ManifestRow("failed", relativePath, sourcePath, bytes, "", 0, 0, "", "", "", "", "", "", error);
        }
    }
}
