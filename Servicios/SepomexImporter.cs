using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

public record SepomexImportResult(
    bool Ok,
    string Message,
    int Estados,
    int Municipios,
    int Colonias,
    List<string> Errors,
    List<string> Warnings
);

namespace WebApiVinculacionProyectosV2.Services
{
    public class SepomexImporter
    {
        private readonly ResidenciasDbContext _db;
        private readonly IConfiguration _cfg;
        private readonly IHostEnvironment _env;

        public SepomexImporter(ResidenciasDbContext db, IConfiguration cfg, IHostEnvironment env)
        {
            _db = db;
            _cfg = cfg;
            _env = env;
        }
        // Abreviaturas tipo Dipomex (ajústalas si quieres)
        private static readonly Dictionary<string, string> EstadoAbrev = new()
        {
            ["01"] = "AGS",
            ["02"] = "BC",
            ["03"] = "BCS",
            ["04"] = "CAM",
            ["05"] = "COA",
            ["06"] = "COL",
            ["07"] = "CHS",
            ["08"] = "CHI",
            ["09"] = "CMX",
            ["10"] = "DGO",
            ["11"] = "GTO",
            ["12"] = "GRO",
            ["13"] = "HGO",
            ["14"] = "JAL",
            ["15"] = "MEX",
            ["16"] = "MIC",
            ["17"] = "MOR",
            ["18"] = "NAY",
            ["19"] = "NLE",
            ["20"] = "OAX",
            ["21"] = "PUE",
            ["22"] = "QRO",
            ["23"] = "ROO",
            ["24"] = "SLP",
            ["25"] = "SIN",
            ["26"] = "SON",
            ["27"] = "TAB",
            ["28"] = "TAM",
            ["29"] = "TLX",
            ["30"] = "VER",
            ["31"] = "YUC",
            ["32"] = "ZAC"
        };


        public async Task<SepomexImportResult> ImportFromUploadOrPathAsync(
            IFormFile? file,
            bool replace,
            bool validateOnly,
            CancellationToken ct)
        {
            string? path = null;
            var errors = new List<string>();
            var warnings = new List<string>();

            if (file != null)
            {
                if (file.Length == 0)
                    return new(false, "El archivo viene vacío.", 0, 0, 0, new() { "Archivo vacío." }, new());

                var tmp = Path.Combine(Path.GetTempPath(), $"cpdescarga_{Guid.NewGuid():N}.txt");
                await using (var fs = File.Create(tmp))
                    await file.CopyToAsync(fs, ct);

                path = tmp;
            }
            else
            {
                path = _cfg["Sepomex:DataFilePath"];
                if (string.IsNullOrWhiteSpace(path))
                    return new(false, "No se recibió archivo y no existe Sepomex:DataFilePath.", 0, 0, 0,
                        new() { "Falta file o DataFilePath válido." }, new());

                // Resuelve rutas relativas contra el ContentRoot (carpeta del proyecto cuando haces dotnet run)
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(_env.ContentRootPath, path);

                if (!File.Exists(path))
                    return new(false, $"No se encontró el archivo en: {path}", 0, 0, 0,
                        new() { "Coloca el archivo en esa ruta o sube el TXT por /api/dipomex/import" }, new());
            }

            try
            {
                return await ImportInternalAsync(path!, replace, validateOnly, ct);
            }
            finally
            {
                // si fue temporal, bórralo
                if (file != null && path != null && File.Exists(path))
                    File.Delete(path);
            }
        }

        private sealed class Layout
        {
            public int Cp, Asenta, MunNombre, EdoNombre, Cr, EdoId, MunId, ColId;
        }

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
private static readonly Encoding Win1252 = Encoding.GetEncoding(1252);

private static async IAsyncEnumerable<string> ReadLinesSmartAsync(
    string filePath,
    [EnumeratorCancellation] CancellationToken ct)
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    await using var fs = new FileStream(
        filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
        bufferSize: 1024 * 1024, useAsync: true);

    var buffer = new byte[1024 * 1024];
    var lineBytes = new List<byte>(4096);

    int read;
    bool isFirstLine = true;

    while ((read = await fs.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
    {
        for (int i = 0; i < read; i++)
        {
            ct.ThrowIfCancellationRequested();
            byte b = buffer[i];

            if (b == (byte)'\n')
            {
                if (lineBytes.Count > 0 && lineBytes[^1] == (byte)'\r')
                    lineBytes.RemoveAt(lineBytes.Count - 1);

                yield return DecodeBestLine(lineBytes, isFirstLine);
                isFirstLine = false;
                lineBytes.Clear();
            }
            else
            {
                lineBytes.Add(b);
            }
        }
    }

    if (lineBytes.Count > 0)
        yield return DecodeBestLine(lineBytes, isFirstLine);
}

private static string DecodeBestLine(List<byte> bytes, bool firstLine)
{
    if (bytes.Count == 0) return string.Empty;

    // BOM UTF-8 al inicio
    if (firstLine && bytes.Count >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        bytes.RemoveRange(0, 3);

    var arr = bytes.ToArray();

    // Si es UTF-8 válido, usar UTF-8; si no, 1252
    if (IsValidUtf8(arr))
        return Utf8NoBom.GetString(arr);

    return Win1252.GetString(arr);
}

private static bool IsValidUtf8(byte[] s)
{
    int i = 0;
    while (i < s.Length)
    {
        byte b = s[i];

        if (b <= 0x7F) { i++; continue; }

        // 2 bytes
        if (b >= 0xC2 && b <= 0xDF)
        {
            if (i + 1 >= s.Length) return false;
            if ((s[i + 1] & 0xC0) != 0x80) return false;
            i += 2;
            continue;
        }

        // 3 bytes
        if (b >= 0xE0 && b <= 0xEF)
        {
            if (i + 2 >= s.Length) return false;
            byte b1 = s[i + 1], b2 = s[i + 2];
            if ((b1 & 0xC0) != 0x80 || (b2 & 0xC0) != 0x80) return false;

            // overlong / surrogates
            if (b == 0xE0 && b1 < 0xA0) return false;
            if (b == 0xED && b1 >= 0xA0) return false;

            i += 3;
            continue;
        }

        // 4 bytes
        if (b >= 0xF0 && b <= 0xF4)
        {
            if (i + 3 >= s.Length) return false;
            byte b1 = s[i + 1], b2 = s[i + 2], b3 = s[i + 3];
            if ((b1 & 0xC0) != 0x80 || (b2 & 0xC0) != 0x80 || (b3 & 0xC0) != 0x80) return false;

            // overlong / > U+10FFFF
            if (b == 0xF0 && b1 < 0x90) return false;
            if (b == 0xF4 && b1 > 0x8F) return false;

            i += 4;
            continue;
        }

        return false;
    }

    return true;
}

private static string FixText(string s, ref bool warned, List<string> warnings)
{
    if (string.IsNullOrEmpty(s)) return string.Empty;

    s = StripBomArtifacts(s).Trim();

    // Mojibake típico: UTF-8 leído como 1252 (Ã, Â)
    if (LooksLikeUtf8Mojibake(s))
    {
        s = FixUtf8Mojibake(s);

        if (!warned)
        {
            warnings.Add("Se detectó mojibake (Ã/Â) y se aplicó reparación automática UTF-8<->1252.");
            warned = true;
        }
    }

    return s;
}

private static bool LooksLikeUtf8Mojibake(string s)
    => s.Contains('Ã') || s.Contains('Â');

private static string FixUtf8Mojibake(string s)
{
    // Recupera UTF-8 interpretado como Windows-1252
    return Encoding.UTF8.GetString(Win1252.GetBytes(s));
}

private static bool ContainsIrrecoverable(string s)
{
    // U+FFFD o el literal ï¿½ ya no se puede recuperar confiablemente
    return s.Contains('\uFFFD') || s.Contains("ï¿½", StringComparison.OrdinalIgnoreCase) || s.Contains("Ï¿½", StringComparison.Ordinal);
}
        private async Task<SepomexImportResult> ImportInternalAsync(
    string filePath,
    bool replace,
    bool validateOnly,
    CancellationToken ct)
{
    var errors = new List<string>();
    var warnings = new List<string>();

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    await using var en = ReadLinesSmartAsync(filePath, ct).GetAsyncEnumerator(ct);

    // 1) Buscar header real (ignorar basura inicial)
    string? firstLine = null;

    while (await en.MoveNextAsync())
    {
        firstLine = en.Current;

        if (string.IsNullOrWhiteSpace(firstLine))
            continue;

        firstLine = StripBomArtifacts(firstLine).Trim();

        if (LooksLikeHtml(firstLine))
        {
            var previewHtml = firstLine.Length > 180 ? firstLine[..180] : firstLine;
            return new(false, "El archivo que llegó parece HTML (no el TXT cpdescarga).", 0, 0, 0,
                new()
                {
                    $"Primeros caracteres: {previewHtml}",
                    "Vuelve a descargar el cpdescarga.txt y súbelo sin guardarlo como página web."
                },
                new());
        }

        if (!IsSepomexHeaderLine(firstLine))
            continue;

        break;
    }

    if (firstLine is null)
    {
        return new(false, "No se encontró el encabezado SEPOMEX en el archivo.", 0, 0, 0,
            new() { "No apareció una línea como: d_codigo|d_asenta|..." }, new());
    }

    var delimiter = DetectDelimiter(firstLine);
    if (delimiter is null)
    {
        var preview = firstLine.Length > 180 ? firstLine[..180] : firstLine;
        return new(false, "Se encontró el encabezado SEPOMEX, pero no se detectó el delimitador.", 0, 0, 0,
            new() { $"Encabezado leído: {preview}" }, new());
    }

    var layout = TryBuildLayoutFromHeader(firstLine, delimiter.Value, warnings, out var headerDetected);
    if (!headerDetected)
    {
        return new(false, "No se pudo interpretar el encabezado SEPOMEX.", 0, 0, 0,
            new() { $"Encabezado leído: {firstLine}" }, warnings);
    }

    // 2) Estructuras de carga
    var estados = new Dictionary<string, (string nombre, string abrev)>();
    var municipios = new Dictionary<(string edo, string mun), string>();
    var colonias = new List<SepomexColonia>();
    var seenColoniaIds = new HashSet<string>();

    int okRows = 0, badRows = 0;
    bool regeneratedCollisionWarning = false;
    bool warnedMojibakeRepair = false;

    var prevAutoDetect = _db.ChangeTracker.AutoDetectChangesEnabled;
    _db.ChangeTracker.AutoDetectChangesEnabled = false;

    await using var tx = !validateOnly ? await _db.Database.BeginTransactionAsync(ct) : null;

    try
    {
        int maxIndex = new[]
        {
            layout.Cp, layout.Asenta, layout.MunNombre, layout.EdoNombre,
            layout.Cr, layout.EdoId, layout.MunId, layout.ColId
        }.Max();

        // 3) Procesar resto de líneas (ya tenemos header)
        while (await en.MoveNextAsync())
        {
            ct.ThrowIfCancellationRequested();

            var line = en.Current;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var p = line.Split(delimiter.Value);

            if (p.Length <= maxIndex)
            {
                badRows++;
                if (badRows <= 20)
                    errors.Add($"Fila con columnas insuficientes. Cols={p.Length}. Ejemplo: {line[..Math.Min(120, line.Length)]}");
                continue;
            }

            string cp = Get(p, layout.Cp).PadLeft(5, '0');
            string asenta = Get(p, layout.Asenta);
            string munNombre = Get(p, layout.MunNombre);
            string edoNombre = Get(p, layout.EdoNombre);
            string crRaw = Get(p, layout.Cr);
            string cr = string.IsNullOrWhiteSpace(crRaw) ? "" : crRaw.PadLeft(5, '0');
            string edoId = Get(p, layout.EdoId).PadLeft(2, '0');
            string munId = Get(p, layout.MunId).PadLeft(3, '0');
            string rawColId = Get(p, layout.ColId);

            // --- Reparación / normalización de texto (sin tumbar import) ---
            asenta = FixText(asenta, ref warnedMojibakeRepair, warnings);
            munNombre = FixText(munNombre, ref warnedMojibakeRepair, warnings);
            edoNombre = FixText(edoNombre, ref warnedMojibakeRepair, warnings);

            // Si aún queda irreparable (� / ï¿½), saltamos fila, NO cancelamos todo
            if (ContainsIrrecoverable(asenta) || ContainsIrrecoverable(munNombre) || ContainsIrrecoverable(edoNombre))
            {
                badRows++;
                if (badRows <= 20)
                    errors.Add($"Fila con texto corrupto (�/ï¿½). Ej: mun='{munNombre}', edo='{edoNombre}', asenta='{asenta}'");
                continue;
            }

            if (!IsDigits(cp, 5) || !IsDigits(edoId, 2) || !IsDigits(munId, 3))
            {
                badRows++;
                if (badRows <= 20)
                    errors.Add($"IDs inválidos (cp/edo/mun). cp={cp}, edoId={edoId}, munId={munId}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(asenta) || string.IsNullOrWhiteSpace(edoNombre) || string.IsNullOrWhiteSpace(munNombre))
            {
                badRows++;
                if (badRows <= 20)
                    errors.Add("Fila con estado/municipio/colonia vacíos.");
                continue;
            }

            var coloniaId = BuildUniqueColoniaId(edoId, munId, rawColId, cp, asenta);

            if (!seenColoniaIds.Add(coloniaId))
            {
                coloniaId = BuildUniqueColoniaId(edoId, munId, $"{rawColId}{cp}", cp, asenta);

                if (!seenColoniaIds.Add(coloniaId))
                {
                    badRows++;
                    if (badRows <= 20)
                        errors.Add($"Colisión de ColoniaId no resuelta. edo={edoId}, mun={munId}, cp={cp}, colonia={asenta}");
                    continue;
                }

                if (!regeneratedCollisionWarning)
                {
                    warnings.Add("Se detectaron colisiones en colonia_id y se regeneraron IDs internos para evitar duplicados.");
                    regeneratedCollisionWarning = true;
                }
            }

            okRows++;

            var edoNorm = NormalizeDisplayText(edoNombre);
            var munNorm = NormalizeDisplayText(munNombre);

            if (!estados.ContainsKey(edoId))
            {
                var abrev = EstadoAbrev.TryGetValue(edoId, out var a) ? a : edoId;
                estados[edoId] = (edoNorm, abrev);
            }

            municipios.TryAdd((edoId, munId), munNorm);

            if (!validateOnly)
            {
                colonias.Add(new SepomexColonia
                {
                    ColoniaId = coloniaId,
                    EstadoId = edoId,
                    MunicipioId = munId,
                    Cp = cp,
                    Nombre = asenta,
                    Cr = cr,
                    FechaAct = DateTime.UtcNow
                });
            }
        }

        if (okRows == 0)
        {
            return new(false, "No se pudo leer ningún registro válido del TXT.", 0, 0, 0,
                errors.Count > 0 ? errors : new() { "0 filas válidas." }, warnings);
        }

        if (validateOnly)
        {
            return new(true, $"Validación OK. Filas válidas: {okRows}.", 0, 0, 0, new(), warnings);
        }

        if (replace)
            await TruncateSepomexTablesAsync(ct);

        // Estados
        foreach (var kv in estados.OrderBy(x => x.Key))
        {
            _db.SepomexEstados.Add(new SepomexEstado
            {
                EstadoId = kv.Key,
                Nombre = kv.Value.nombre,
                Abreviatura = kv.Value.abrev
            });
        }

        await _db.SaveChangesAsync(ct);
        _db.ChangeTracker.Clear();

        // Municipios
        foreach (var kv in municipios.OrderBy(x => x.Key.edo).ThenBy(x => x.Key.mun))
        {
            _db.SepomexMunicipios.Add(new SepomexMunicipio
            {
                EstadoId = kv.Key.edo,
                MunicipioId = kv.Key.mun,
                Nombre = kv.Value
            });
        }

        await _db.SaveChangesAsync(ct);
        _db.ChangeTracker.Clear();

        // Colonias (lotes)
        foreach (var chunk in colonias.Chunk(5000))
        {
            _db.SepomexColonias.AddRange(chunk);
            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear();
        }

        await RecalculateRangesAsync(ct);
        await tx!.CommitAsync(ct);

        return new(true, $"Import OK. Filas válidas: {okRows}.", estados.Count, municipios.Count, colonias.Count, new(), warnings);
    }
    catch (Exception ex)
    {
        if (tx != null)
            await tx.RollbackAsync(ct);

        errors.Add(ex.ToString());
        return new(false, "Error importando SEPOMEX.", 0, 0, 0, errors, warnings);
    }
    finally
    {
        _db.ChangeTracker.AutoDetectChangesEnabled = prevAutoDetect;
    }
}
        private static Encoding ChooseSepomexEncoding(string filePath, out string note)
        {
            // Probamos UTF-8 estricto vs Windows-1252 vs ISO-8859-1
            // y elegimos el que genere menos "basura" (ï¿½, Ã, �, NULs).
            var candidates = new List<(Encoding enc, string name)>
    {
        (new UTF8Encoding(false, throwOnInvalidBytes: true), "utf-8(strict)"),
        (Encoding.GetEncoding(1252), "windows-1252"),
        (Encoding.GetEncoding("iso-8859-1"), "iso-8859-1")
    };

            (Encoding enc, string name) best = candidates[0];
            int bestScore = int.MaxValue;
            string bestWhy = "";

            foreach (var c in candidates)
            {
                var score = ScoreSepomexFileSample(filePath, c.enc, out var why);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                    bestWhy = why;
                }
            }

            note = $"Encoding elegido: {best.name} (score={bestScore}). {bestWhy}";
            return best.enc;
        }

        private static int ScoreSepomexFileSample(string filePath, Encoding enc, out string why)
        {
            try
            {
                using var sr = new StreamReader(filePath, enc, detectEncodingFromByteOrderMarks: false);

                int score = 0;
                int linesChecked = 0;
                int badTokens = 0;
                string? line;

                // Leemos suficientes líneas para topar acentos aunque al inicio haya puro ASCII.
                while ((line = sr.ReadLine()) != null && linesChecked < 8000)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    line = StripBomArtifacts(line);

                    // penalizaciones fuertes
                    if (line.Contains('\0')) score += 20000;
                    if (line.Contains('\uFFFD')) { score += 8000; badTokens++; } // "�"
                    if (line.Contains("ï¿½", StringComparison.OrdinalIgnoreCase) || line.Contains("Ï¿½", StringComparison.Ordinal))
                    { score += 6000; badTokens++; }

                    // mojibake típico de leer UTF-8 como 1252 o viceversa
                    if (line.Contains('Ã') || line.Contains('Â')) { score += 1500; badTokens++; }

                    linesChecked++;
                }

                why = $"Líneas evaluadas={linesChecked}, tokens raros={badTokens}.";
                return score;
            }
            catch (DecoderFallbackException)
            {
                // UTF-8 estricto revienta si el archivo NO es UTF-8
                why = "DecoderFallbackException (no compatible).";
                return int.MaxValue - 1;
            }
        }

        private static string StripBomArtifacts(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            s = s.TrimStart('\uFEFF');   // BOM real
            if (s.StartsWith("ï»¿"))     // BOM leído como 1252
                s = s.Substring(3);

            return s;
        }

        private static Encoding DetectSepomexEncodingRobust(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 1) BOM primero (si existe, no hay duda)
            using (var fs = File.OpenRead(filePath))
            {
                Span<byte> bom3 = stackalloc byte[3];
                var n = fs.Read(bom3);
                if (n >= 3 && bom3[0] == 0xEF && bom3[1] == 0xBB && bom3[2] == 0xBF)
                    return new UTF8Encoding(false);

                fs.Position = 0;
                Span<byte> bom2 = stackalloc byte[2];
                n = fs.Read(bom2);
                if (n >= 2 && bom2[0] == 0xFF && bom2[1] == 0xFE) return Encoding.Unicode;          // UTF-16 LE
                if (n >= 2 && bom2[0] == 0xFE && bom2[1] == 0xFF) return Encoding.BigEndianUnicode; // UTF-16 BE
            }

            // 2) Prueba real: intenta leer VARIAS líneas con UTF-8 ESTRICTO.
            // Si el archivo es 1252/latin1, al llegar a una á/é/í/ó/ú va a explotar aquí (y caemos a 1252).
            var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            try
            {
                using var sr = new StreamReader(filePath, utf8Strict, detectEncodingFromByteOrderMarks: true);

                for (int i = 0; i < 8000; i++) // suficiente para topar acentos aunque el inicio sea ASCII
                {
                    var line = sr.ReadLine();
                    if (line is null) break;

                    // si ya viene U+FFFD, no lo aceptamos
                    if (line.Contains('\uFFFD'))
                        throw new DecoderFallbackException("Se detectó U+FFFD durante prueba UTF-8.");
                }

                return new UTF8Encoding(false); // archivo realmente UTF-8
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(1252); // Windows-1252 (ANSI) típico en SEPOMEX
            }
        }

        private static string NormalizeDisplayText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .Normalize(NormalizationForm.FormC)
                .ToUpper(new CultureInfo("es-MX"));
        }

        private Layout TryBuildLayoutFromHeader(string firstLine, char delimiter, List<string> warnings, out bool headerDetected)
        {
            headerDetected = firstLine.Contains("d_codigo", StringComparison.OrdinalIgnoreCase)
                             || firstLine.Contains("d_asenta", StringComparison.OrdinalIgnoreCase);

            if (!headerDetected)
                return new Layout();

            var cols = firstLine.Split(delimiter).Select((name, idx) => (name: name.Trim().ToLowerInvariant(), idx))
                                .ToDictionary(x => x.name, x => x.idx);

            // Campos mínimos que ocupamos
            string[] required = { "d_codigo", "d_asenta", "d_estado", "d_mnpio", "d_cp", "c_estado", "c_mnpio", "id_asenta_cpcons" };

            var missing = required.Where(r => !cols.ContainsKey(r)).ToList();
            if (missing.Count > 0)
            {
                warnings.Add("Header detectado, pero faltan columnas: " + string.Join(", ", missing) + ". Se intentará layout por posición.");
                headerDetected = false;
                return new Layout();
            }

            return new Layout
            {
                Cp = cols["d_codigo"],
                Asenta = cols["d_asenta"],
                EdoNombre = cols["d_estado"],
                MunNombre = cols["d_mnpio"],
                Cr = cols["d_cp"],
                EdoId = cols["c_estado"],
                MunId = cols["c_mnpio"],
                ColId = cols["id_asenta_cpcons"]
            };
        }

        private async Task TruncateSepomexTablesAsync(CancellationToken ct)
        {
            // Ojo: usamos DELETE y no TRUNCATE para que el rollback sí sea seguro.
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM SepomexColonias;", ct);
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM SepomexMunicipios;", ct);
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM SepomexEstados;", ct);
        }

        private async Task RecalculateRangesAsync(CancellationToken ct)
        {
            var edoRanges = await _db.SepomexColonias
                .GroupBy(c => c.EstadoId)
                .Select(g => new { g.Key, Min = g.Min(x => x.Cp), Max = g.Max(x => x.Cp) })
                .ToListAsync(ct);

            foreach (var r in edoRanges)
            {
                var e = await _db.SepomexEstados.FirstAsync(x => x.EstadoId == r.Key, ct);
                e.Rango1 = r.Min; e.Rango2 = r.Max;
            }
            await _db.SaveChangesAsync(ct);

            var munRanges = await _db.SepomexColonias
                .GroupBy(c => new { c.EstadoId, c.MunicipioId })
                .Select(g => new { g.Key.EstadoId, g.Key.MunicipioId, Min = g.Min(x => x.Cp), Max = g.Max(x => x.Cp) })
                .ToListAsync(ct);

            foreach (var r in munRanges)
            {
                var m = await _db.SepomexMunicipios.FirstAsync(x => x.EstadoId == r.EstadoId && x.MunicipioId == r.MunicipioId, ct);
                m.Rango1 = r.Min; m.Rango2 = r.Max;
            }
            await _db.SaveChangesAsync(ct);
        }

        private static string Get(string[] p, int idx) => (idx >= 0 && idx < p.Length) ? (p[idx] ?? "").Trim() : "";
        private static bool IsDigits(string s, int len) => s.Length == len && s.All(char.IsDigit);


        private static string NormalizeUpperNoDiacritics(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim().Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            return sb
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .ToUpper(new CultureInfo("es-MX"));
        }

        private static string DefaultAbrev(string edoId) => edoId switch
        {
            "09" => "CMX",
            _ => edoId
        };

       
        private static char? DetectDelimiter(string firstLine)
        {
            if (firstLine.Contains('|')) return '|';
            if (firstLine.Contains('\t')) return '\t';
            return null;
        }

        private static bool LooksLikeHtml(string s)
            => s.Contains("<!doctype", StringComparison.OrdinalIgnoreCase)
               || s.Contains("<html", StringComparison.OrdinalIgnoreCase)
               || s.Contains("<head", StringComparison.OrdinalIgnoreCase);

        private static string EscapeDelimiter(char d) => d switch
        {
            '\t' => "\\t",
            _ => d.ToString()
        };

        private static bool IsSepomexHeaderLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            line = line.TrimStart('\uFEFF').Trim();

            return line.StartsWith("d_codigo|d_asenta|", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("d_codigo\td_asenta\t", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildUniqueColoniaId(string edoId, string munId, string rawColId, string cp, string asenta)
        {
            var baseId = new string((rawColId ?? "").Trim().Where(char.IsLetterOrDigit).ToArray());

            if (string.IsNullOrWhiteSpace(baseId))
                baseId = (Math.Abs($"{cp}|{asenta}".GetHashCode()) % 100000).ToString("00000");

            if (baseId.Length > 5)
                baseId = (Math.Abs(baseId.GetHashCode()) % 100000).ToString("00000");

            baseId = baseId.PadLeft(5, '0');

            // 2 + 3 + 5 = 10, cabe perfecto en tu modelo actual
            return $"{edoId}{munId}{baseId}";
        }

        private static Encoding DetectSepomexEncoding(string filePath)
        {
            // Lee una muestra pequeña para detectar sin cargar todo el archivo
            using var fs = File.OpenRead(filePath);

            Span<byte> buf = stackalloc byte[8192];
            int read = fs.Read(buf);
            var bytes = buf[..read].ToArray();

            // UTF-8 BOM: EF BB BF
            if (read >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            // Intentar UTF-8 estricto (si falla, caemos a Windows-1252)
            try
            {
                _ = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
                return new UTF8Encoding(false);
            }
            catch (DecoderFallbackException)
            {
                // SEPOMEX frecuentemente viene como Windows-1252 en español
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1252); // windows-1252
            }
        }

    }

}
