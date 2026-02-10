using System.IO.Compression;
using System.Text;
using System.Text.Json;
using GameCompanion.Module.StarRupture.Services;

Console.WriteLine("=== StarRupture Save Dumper ===\n");

string? savePath = args.Length > 0 ? args[0] : null;

if (savePath == null)
{
    // Auto-discover the newest save
    Console.WriteLine("No save path specified, discovering saves...\n");
    var discovery = new SaveDiscoveryService();
    var sessionsResult = await discovery.DiscoverSessionsAsync();

    if (sessionsResult.IsFailure)
    {
        Console.WriteLine($"Error: {sessionsResult.Error}");
        return;
    }

    var sessions = sessionsResult.Value!;
    if (sessions.Count == 0)
    {
        Console.WriteLine("No save sessions found.");
        return;
    }

    var newest = sessions.OrderByDescending(s => s.LastModified).First();
    var newestSlot = newest.Slots.FirstOrDefault();
    if (newestSlot == null)
    {
        Console.WriteLine($"Session '{newest.SessionName}' has no save slots.");
        return;
    }

    savePath = newestSlot.SaveFilePath;
    Console.WriteLine($"Using newest save: {savePath}");
    Console.WriteLine($"  Session: {newest.SessionName}");
    Console.WriteLine($"  Slot: {newestSlot.SlotName}");
    Console.WriteLine($"  Last Modified: {newestSlot.LastModified}");
    Console.WriteLine($"  Size: {newestSlot.SizeBytes / 1024:N0} KB\n");
}

if (!File.Exists(savePath))
{
    Console.WriteLine($"File not found: {savePath}");
    return;
}

// Decompress the save file
Console.WriteLine("Decompressing save file...");
var fileBytes = await File.ReadAllBytesAsync(savePath);

if (fileBytes.Length < 6)
{
    Console.WriteLine("File too small to be a valid save.");
    return;
}

var uncompressedSize = BitConverter.ToInt32(fileBytes, 0);
Console.WriteLine($"  File size: {fileBytes.Length:N0} bytes");
Console.WriteLine($"  Expected uncompressed size: {uncompressedSize:N0} bytes");

if (fileBytes[4] != 0x78 || fileBytes[5] != 0x9C)
{
    Console.WriteLine($"  Warning: No zlib header found (got 0x{fileBytes[4]:X2} 0x{fileBytes[5]:X2}, expected 0x78 0x9C)");
    Console.WriteLine("  Attempting to read as plain text...");

    // Try reading as plain text/JSON
    var plainText = Encoding.UTF8.GetString(fileBytes);
    if (plainText.TrimStart().StartsWith('{'))
    {
        Console.WriteLine("  File appears to be uncompressed JSON.");
        await DumpJson(plainText, savePath);
    }
    else
    {
        Console.WriteLine("  File is not recognizable JSON. Dumping raw hex header:");
        Console.WriteLine($"  {BitConverter.ToString(fileBytes, 0, Math.Min(64, fileBytes.Length))}");
    }
    return;
}

// Decompress (skip 4-byte size header + 2-byte zlib header)
using var compressedStream = new MemoryStream(fileBytes, 6, fileBytes.Length - 6);
using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
using var resultStream = new MemoryStream();

await deflateStream.CopyToAsync(resultStream);
var decompressed = resultStream.ToArray();
var jsonString = Encoding.UTF8.GetString(decompressed);

Console.WriteLine($"  Decompressed size: {decompressed.Length:N0} bytes");
Console.WriteLine();

await DumpJson(jsonString, savePath);

static async Task DumpJson(string jsonString, string savePath)
{
    // Write raw JSON to file
    var outputDir = Path.GetDirectoryName(savePath) ?? ".";
    var saveName = Path.GetFileNameWithoutExtension(savePath);
    var outputPath = Path.Combine(outputDir, $"{saveName}_dump.json");

    // Pretty-print the JSON
    try
    {
        using var doc = JsonDocument.Parse(jsonString);
        var prettyJson = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, prettyJson);
        Console.WriteLine($"JSON dump written to: {outputPath}");
        Console.WriteLine($"  Pretty-printed size: {new FileInfo(outputPath).Length / 1024:N0} KB\n");
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Warning: JSON parse error: {ex.Message}");
        Console.WriteLine("Writing raw text instead...");
        await File.WriteAllTextAsync(outputPath, jsonString);
        Console.WriteLine($"Raw dump written to: {outputPath}\n");
        return;
    }

    // Print structure summary
    Console.WriteLine("=== JSON STRUCTURE SUMMARY ===\n");
    using var doc2 = JsonDocument.Parse(jsonString);
    PrintStructure(doc2.RootElement, "", 0, maxDepth: 4);

    // Search for spatial data patterns
    Console.WriteLine("\n=== SPATIAL DATA SEARCH ===\n");
    SearchForSpatialData(doc2.RootElement, "");
}

static void PrintStructure(JsonElement element, string path, int depth, int maxDepth)
{
    var indent = new string(' ', depth * 2);

    switch (element.ValueKind)
    {
        case JsonValueKind.Object:
            var props = element.EnumerateObject().ToList();
            if (depth < maxDepth)
            {
                Console.WriteLine($"{indent}{(path == "" ? "Root" : path)}: Object ({props.Count} properties)");
                foreach (var prop in props)
                {
                    PrintStructure(prop.Value, prop.Name, depth + 1, maxDepth);
                }
            }
            else
            {
                Console.WriteLine($"{indent}{path}: Object ({props.Count} properties) [truncated]");
            }
            break;

        case JsonValueKind.Array:
            var items = element.EnumerateArray().ToList();
            if (items.Count > 0)
            {
                Console.WriteLine($"{indent}{path}: Array [{items.Count} items]");
                if (depth < maxDepth && items.Count > 0)
                {
                    Console.WriteLine($"{indent}  [0]:");
                    PrintStructure(items[0], "[0]", depth + 2, maxDepth);
                }
            }
            else
            {
                Console.WriteLine($"{indent}{path}: Array [empty]");
            }
            break;

        case JsonValueKind.String:
            var strVal = element.GetString() ?? "";
            if (strVal.Length > 80)
                strVal = strVal[..80] + "...";
            Console.WriteLine($"{indent}{path}: String = \"{strVal}\"");
            break;

        case JsonValueKind.Number:
            Console.WriteLine($"{indent}{path}: Number = {element.GetRawText()}");
            break;

        case JsonValueKind.True:
        case JsonValueKind.False:
            Console.WriteLine($"{indent}{path}: Boolean = {element.GetRawText()}");
            break;

        case JsonValueKind.Null:
            Console.WriteLine($"{indent}{path}: null");
            break;
    }
}

static void SearchForSpatialData(JsonElement root, string basePath)
{
    var spatialKeywords = new[]
    {
        "position", "location", "transform", "coordinate", "coord",
        "building", "entity", "structure", "placed", "construct",
        "machine", "processor", "assembler", "producer", "generator",
        "track", "conveyor", "belt", "pipe", "cable", "connection", "logistics",
        "chunk", "region", "explored", "visited", "discovered", "fog",
        "base", "outpost", "hub", "factory",
        "x", "y", "z", "rotation", "scale"
    };

    var found = new List<(string path, string keyword, JsonValueKind kind, string preview)>();
    SearchRecursive(root, "", spatialKeywords, found, 0, maxDepth: 8);

    if (found.Count == 0)
    {
        Console.WriteLine("No spatial data keywords found in the save file.");
        Console.WriteLine("The save may store world/entity data in a separate file or binary format.");
    }
    else
    {
        Console.WriteLine($"Found {found.Count} potential spatial data matches:\n");
        foreach (var (path, keyword, kind, preview) in found.OrderBy(f => f.path))
        {
            Console.WriteLine($"  [{keyword}] {path}");
            Console.WriteLine($"    Type: {kind}, Preview: {preview}");
            Console.WriteLine();
        }
    }
}

static void SearchRecursive(
    JsonElement element,
    string currentPath,
    string[] keywords,
    List<(string path, string keyword, JsonValueKind kind, string preview)> results,
    int depth,
    int maxDepth)
{
    if (depth > maxDepth) return;

    if (element.ValueKind == JsonValueKind.Object)
    {
        foreach (var prop in element.EnumerateObject())
        {
            var propPath = string.IsNullOrEmpty(currentPath) ? prop.Name : $"{currentPath}.{prop.Name}";
            var propNameLower = prop.Name.ToLowerInvariant();

            foreach (var keyword in keywords)
            {
                if (propNameLower.Contains(keyword))
                {
                    var preview = GetPreview(prop.Value);
                    results.Add((propPath, keyword, prop.Value.ValueKind, preview));
                    break; // Only match once per property
                }
            }

            SearchRecursive(prop.Value, propPath, keywords, results, depth + 1, maxDepth);
        }
    }
    else if (element.ValueKind == JsonValueKind.Array)
    {
        var items = element.EnumerateArray().ToList();
        if (items.Count > 0)
        {
            // Only recurse into the first element to find structure
            SearchRecursive(items[0], $"{currentPath}[0]", keywords, results, depth + 1, maxDepth);
        }
    }
}

static string GetPreview(JsonElement element)
{
    var raw = element.GetRawText();
    if (raw.Length > 120)
        return raw[..120] + "...";
    return raw;
}
