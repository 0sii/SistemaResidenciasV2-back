using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;

using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfReader = iText.Kernel.Pdf.PdfReader;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;
using iText.Layout.Borders;

using iText.IO.Font;
using System.Linq;

public partial interface IConstanciasPdfService
{
    byte[] BuildConstanciaAceptacionReportePreliminar(byte[] templatePdf, ConstanciaAceptacionReportePreliminarRequest req);

    byte[] BuildOficioAsignacionAsesorInterno(byte[] templatePdf, OficioAsignacionAsesorInternoRequest req);

    // existente
    byte[] BuildOficiosAsignacionRevisores(byte[] templatePdf, OficiosAsignacionRevisoresRequest req);

    // ✅ NUEVO: formato “foto” (revisores)
    byte[] BuildOficiosAsignacionRevisoresFormatoFoto(byte[] templatePdf, OficiosAsignacionRevisoresRequest req);

    // ✅ NUEVO: Asignación de revisor de reporte preliminar (rol "revisor de anteproyecto",
    // distinto de "revisor de residencia" que usa BuildOficiosAsignacionRevisores)
    byte[] BuildOficioAsignacionRevisorReportePreliminar(byte[] templatePdf, OficioAsignacionRevisorReportePreliminarRequest req);
}


public sealed class OficioAsignacionAsesorInternoRequest
{
    public string Ciudad { get; set; } = "Oaxaca de Juárez, Oaxaca";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Oficio { get; set; } = "JV-XXX/2025";

    public string DestinatarioNombre { get; set; } = "NOMBRE DEL ASESOR";
    public string DestinatarioCargoLinea1 { get; set; } = "CATEDRATICO(A) DEL I.T. DE OAXACA";

    public string NombreProyecto { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Carrera { get; set; } = "";
    public string PeriodoRealizacion { get; set; } = ""; // Ej: "FEBRERO - JUNIO 2025"

    // ✅ Número de control del residente (primera fila de la tabla en el formato real)
    public string NumeroControl { get; set; } = "";

    // En el formato dice "Nombre del Residente" (singular), pero en la práctica puede haber equipo:
    public List<string> Residentes { get; set; } = new();

    // Pie de firma
    public string FirmaNombre { get; set; } = "NOMBRE DE QUIEN FIRMA";
    public string FirmaCargoLinea1 { get; set; } = "JEFA(E) DEL DEPARTAMENTO";
    public string FirmaCargoLinea2 { get; set; } = "DE SISTEMAS Y COMPUTACIÓN";
}

public sealed class OficiosAsignacionRevisoresRequest
{
    public string Ciudad { get; set; } = "Oaxaca de Juárez, Oaxaca";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Oficio { get; set; } = "JV-XXX/2025";
    public string Asunto { get; set; } = "Revisor de Residencia Profesional";

    // Se genera 1 PDF, pero con 1 página por revisor:
    public List<OficioRevisorItem> Revisores { get; set; } = new();

    public string FirmaNombre { get; set; } = "M.C.IDARH CLAUDIO MATADAMAS ORTIZ";
    public string FirmaCargoLinea1 { get; set; } = "JEFE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN";
}

public sealed class OficioRevisorItem
{
    public string RevisorNombre { get; set; } = "NOMBRE DEL REVISOR";
    public string RevisorCargoLinea1 { get; set; } = "CATEDRATICO(A) DEL I.T. DE OAXACA";

    // Filas de tabla (para tu caso típico: 1 proyecto por revisor, pero soporta varias)
    public List<OficioRevisorRow> Rows { get; set; } = new();
}

public sealed class OficioRevisorRow
{
    public string NoControl          { get; set; } = "";
    public string Estudiante         { get; set; } = "";
    public string Proyecto           { get; set; } = "";
    public string Asesor             { get; set; } = "";

    // ✅ Propiedades agregadas para el oficio de revisor de anteproyecto
    public List<string> Estudiantes  { get; set; } = new();
    public string NombreProyecto     { get; set; } = "";
    public string Empresa            { get; set; } = "";
    public string Carrera            { get; set; } = "";
    public string PeriodoRealizacion { get; set; } = "";
}

// ✅ NUEVO: rol "revisor de anteproyecto" — revisa el REPORTE PRELIMINAR de un solo
// proyecto (con uno o varios estudiantes), distinto del "revisor de residencia"
// (OficiosAsignacionRevisoresRequest) que revisa varios proyectos a la vez.
public sealed class RevisorReportePreliminarEstudianteItem
{
    public string NumeroControl { get; set; } = "";
    public string NombreEstudiante { get; set; } = "";
}

public sealed class OficioAsignacionRevisorReportePreliminarRequest
{
    public string Ciudad { get; set; } = "Oaxaca de Juárez, Oaxaca";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Oficio { get; set; } = "JV-XXX/2026";
    public string Asunto { get; set; } = "Asignación de revisor de reporte preliminar";

    public string DestinatarioNombre { get; set; } = "NOMBRE DEL REVISOR";
    public string DestinatarioCargoLinea1 { get; set; } = "CATEDRATICO(A) DEL I.T. DE OAXACA";

    // Numeral del Lineamiento para la Operación de la Residencia Profesional
    public string NumeralLineamiento { get; set; } = "12.4.1.7";

    public string NombreProyecto { get; set; } = "";

    // Uno o varios estudiantes que presentan el mismo proyecto
    public List<RevisorReportePreliminarEstudianteItem> Estudiantes { get; set; } = new();

    // Pie de firma (normalmente Subdirección Académica)
    public string FirmaNombre { get; set; } = "NOMBRE DE QUIEN FIRMA";
    public string FirmaCargoLinea1 { get; set; } = "SUBDIRECTORA ACADÉMICA";
}

public sealed class ConstanciaAceptacionReportePreliminarRequest
{
    public string Ciudad { get; set; } = "Oaxaca de Juárez, Oaxaca";
    public DateTime Fecha { get; set; } = DateTime.Today;

    // En el formato original suele quedarse como XXX
    public string Oficio { get; set; } = "ITO/XXX/2026";

    // Nombre en la línea "C. ____"
    public string DestinatarioNombre { get; set; } = "IDAR";

    // Lo que va después de: "Jefe(a) del Departamento de ______"
    // Si lo dejas vacío, se intenta derivar de la carrera.
    public string DepartamentoNombre { get; set; } = "Ingeniería en Sistemas Computacionales";

    // ✅ Puedes mandar Carrera (nombre) o CarreraId (y aquí se mapea)
    public string Carrera { get; set; } = "";
    public int? CarreraId { get; set; } = null;

    public string NoControl { get; set; } = "";
    public string Estudiante { get; set; } = "";
    public string TituloReporte { get; set; } = "";

    // dd/MM/yyyy (si llegan vacías, se imprime "—")
    public string FechaInicio { get; set; } = "";
    public string FechaTermino { get; set; } = "";

    // "APROBADO" | "NO_APROBADO" | "RECHAZADO"
    public string Dictamen { get; set; } = "APROBADO";

    public string Comentarios { get; set; } = "";
    public string AsesorInterno { get; set; } = "";
}

 namespace WebApiVinculacionProyectosV2.Services
 {
 public partial class ConstanciasPdfService : IConstanciasPdfService    
    {

        const float headerSize = 9.6f;
        const float oficioSize = 9.9f;
        const float asuntoLabelSize = 9.6f;
        const float asuntoValueSize = 9.8f;

        const float destinatarioSize = 9.9f;
        const float cargoSize = 9.7f;

        const float bodySize = 9.45f;
        const float bodyLeading = 10.35f;

        const float tableSize = 9.2f;
        const float signSize = 9.9f;
        const float smallSize = 9.2f;
        public byte[] BuildConstanciaAceptacionReportePreliminar(byte[] templatePdf, ConstanciaAceptacionReportePreliminarRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            using var output = new MemoryStream();
            using var writer = new PdfWriter(output);
            using var pdf = new PdfDocument(writer);

            PdfPage page;

            if (templatePdf != null && templatePdf.Length > 0)
            {
                using var templateStream = new MemoryStream(templatePdf);
                using var reader = new PdfReader(templateStream);
                using var src = new PdfDocument(reader);

                page = src.GetFirstPage().CopyTo(pdf);
                pdf.AddPage(page);
            }
            else
            {
                page = pdf.AddNewPage(PageSize.LETTER);
            }

            var rect = page.GetPageSizeWithRotation();
            var canvas = new PdfCanvas(page);

            // ── Compensar flip vertical del membrete escaneado ───────────────
            // Algunos membretes escaneados tienen "1 0 0 -1 0 H cm" embebido
            // en su stream de contenido (flip Y). Detectamos esto leyendo el
            // primer operador cm del stream y aplicamos la misma transformación
            // a nuestro canvas para que las coordenadas queden alineadas.
            {
                var pageRotation = page.GetRotation();
                bool hasYFlip = false;

                if (pageRotation == 0)
                {
                    try
                    {
                        var pageDict = page.GetPdfObject();
                        var contentsObj = pageDict.Get(iText.Kernel.Pdf.PdfName.Contents);
                        iText.Kernel.Pdf.PdfStream? firstStream = null;

                        if (contentsObj is iText.Kernel.Pdf.PdfArray arr && arr.Size() > 0)
                            firstStream = arr.GetAsStream(0);
                        else if (contentsObj is iText.Kernel.Pdf.PdfStream s)
                            firstStream = s;

                        if (firstStream != null)
                        {
                            var bytes = firstStream.GetBytes();
                            var preview = System.Text.Encoding.Latin1.GetString(bytes, 0,
                                Math.Min(bytes.Length, 80));
                            // Detectar "1 0 0 -1 0 <número> cm" al inicio del stream
                            hasYFlip = System.Text.RegularExpressions.Regex.IsMatch(
                                preview, @"1\s+0\s+0\s+-1\s+0\s+[\d.]+\s+cm");
                        }
                    }
                    catch { /* si falla la detección, no aplicamos nada */ }
                }

                float w = rect.GetWidth();
                float h = rect.GetHeight();

                if (hasYFlip)
                    canvas.ConcatMatrix(1, 0, 0, -1, 0, h);   // mismo flip que el membrete
                else if (pageRotation == 90)
                    canvas.ConcatMatrix(0, 1, -1, 0, h, 0);
                else if (pageRotation == 180)
                    canvas.ConcatMatrix(-1, 0, 0, -1, w, h);
                else if (pageRotation == 270)
                    canvas.ConcatMatrix(0, -1, 1, 0, 0, w);
            }
            // ────────────────────────────────────────────────────────────────

            PdfFont font = LoadFontFromCandidates(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Regular.ttf"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "NotoSans-Regular.ttf")
            );

            PdfFont fontBold = LoadFontFromCandidates(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Bold.ttf"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "NotoSans-Bold.ttf")
            );

            float Y(float yFromTop) => rect.GetHeight() - yFromTop;

            void DrawText(string text, float x, float yFromTopBaseline, PdfFont f, float size)
            {
                canvas.BeginText();
                canvas.SetFontAndSize(f, size);
                canvas.MoveText(x, Y(yFromTopBaseline));
                canvas.ShowText(text ?? "");
                canvas.EndText();
            }

            void DrawLine(float x1, float yFromTop, float x2, float y2FromTop, float width = 1f)
            {
                canvas.SaveState();
                canvas.SetLineWidth(width);
                canvas.MoveTo(x1, Y(yFromTop));
                canvas.LineTo(x2, Y(y2FromTop));
                canvas.Stroke();
                canvas.RestoreState();
            }

            void DrawRect(float x, float yFromTopTop, float w, float h, float lineWidth = 1f)
            {
                canvas.SaveState();
                canvas.SetLineWidth(lineWidth);
                canvas.Rectangle(x, Y(yFromTopTop + h), w, h);
                canvas.Stroke();
                canvas.RestoreState();
            }

            void DrawTextRight(string text, float rightX, float yFromTopBaseline, PdfFont f, float size)
            {
                text ??= "";
                float textWidth = f.GetWidth(text, size);
                DrawText(text, rightX - textWidth, yFromTopBaseline, f, size);
            }

            void DrawMixedRight(
                string leftText, PdfFont leftFont, float leftSize,
                string rightText, PdfFont rightFont, float rightSize,
                float rightX, float yFromTopBaseline, float gap = 3f)
            {
                leftText ??= "";
                rightText ??= "";

                float leftWidth = leftFont.GetWidth(leftText, leftSize);
                float rightWidth = rightFont.GetWidth(rightText, rightSize);

                float startX = rightX - (leftWidth + gap + rightWidth);

                DrawText(leftText, startX, yFromTopBaseline, leftFont, leftSize);
                DrawText(rightText, startX + leftWidth + gap, yFromTopBaseline, rightFont, rightSize);
            }


            string TruncateWithEllipsis(string text, PdfFont f, float size, float maxWidth)
            {
                text ??= "";
                const string ell = "…";
                if (f.GetWidth(text, size) <= maxWidth) return text;

                var t = text;
                while (t.Length > 0 && f.GetWidth(t + ell, size) > maxWidth)
                    t = t.Substring(0, t.Length - 1);

                return t.Length == 0 ? "" : (t + ell);
            }

            string TruncateWithoutEllipsis(string text, PdfFont f, float size, float maxWidth)
            {
                text ??= "";
                if (f.GetWidth(text, size) <= maxWidth) return text;

                var t = text;
                while (t.Length > 0 && f.GetWidth(t, size) > maxWidth)
                    t = t.Substring(0, t.Length - 1);

                return t;
            }

            void DrawFitSingleLine(string text, float x, float yFromTopBaseline, float maxWidth, PdfFont f, float size, float minSize = 7.5f)
            {
                text ??= "";
                float s = size;

                while (s > minSize && f.GetWidth(text, s) > maxWidth)
                    s -= 0.25f;

                if (f.GetWidth(text, s) > maxWidth)
                    text = TruncateWithEllipsis(text, f, s, maxWidth);

                DrawText(text, x, yFromTopBaseline, f, s);
            }

            void DrawFitSingleLineNoEllipsis(string text, float x, float yFromTopBaseline, float maxWidth, PdfFont f, float size, float minSize = 7.5f)
            {
                text ??= "";
                float s = size;

                while (s > minSize && f.GetWidth(text, s) > maxWidth)
                    s -= 0.25f;

                if (f.GetWidth(text, s) > maxWidth)
                    text = TruncateWithoutEllipsis(text, f, s, maxWidth);

                DrawText(text, x, yFromTopBaseline, f, s);
            }

            List<string> WrapLines(string text, PdfFont f, float size, float maxWidth)
            {
                text ??= "";
                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lines = new List<string>();
                string line = "";

                foreach (var w in words)
                {
                    var test = string.IsNullOrEmpty(line) ? w : $"{line} {w}";
                    if (f.GetWidth(test, size) > maxWidth && !string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                        line = w;
                    }
                    else
                    {
                        line = test;
                    }
                }

                if (!string.IsNullOrEmpty(line))
                    lines.Add(line);

                return lines;
            }

            string ResolveCarreraNombre()
            {
                if (!string.IsNullOrWhiteSpace(req.Carrera))
                    return req.Carrera.Trim();

                if (req.CarreraId.HasValue)
                {
                    return req.CarreraId.Value switch
                    {
                        1 => "Ingeniería en Sistemas Computacionales",
                        2 => "Ingeniería Industrial",
                        3 => "Ingeniería Electrónica",
                        4 => "Ingeniería Mecánica",
                        _ => "—"
                    };
                }

                return "—";
            }

            string SafeDate(string s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();

            float MeasureWrappedCellHeight(string text, PdfFont f, float size, float width, float padding, float leading)
            {
                text ??= "";
                float innerW = Math.Max(0, width - (padding * 2));
                var lines = WrapLines(text, f, size, innerW);
                int count = Math.Max(1, lines.Count);
                return (count * leading) + (padding * 2) + 2f;
            }

            void FillCellWrappedDynamic((float x0, float x1) col, string text, float cellTop, float cellHeight, float size, float padding, float leading)
            {
                text ??= "";

                float x = col.x0 + padding;
                float w = (col.x1 - col.x0) - (padding * 2);

                var cellLines = WrapLines(text, font, size, w);
                if (cellLines.Count == 0)
                    cellLines = new List<string> { "" };

                int maxLines = Math.Max(1, (int)Math.Floor((cellHeight - (padding * 2)) / leading));

                if (cellLines.Count > maxLines)
                {
                    string last = string.Join(" ", cellLines.Skip(maxLines - 1));
                    cellLines = cellLines.Take(maxLines - 1).ToList();
                    cellLines.Add(TruncateWithoutEllipsis(last, font, size, w));
                }

                float baseY = cellTop + padding + size;
                for (int i = 0; i < cellLines.Count; i++)
                    DrawText(cellLines[i], x, baseY + (i * leading), font, size);
            }

            void FillCellSingleNoEllipsis((float x0, float x1) col, string text, float yBase, PdfFont f, float size, float padding = 4f)
            {
                float x = col.x0 + padding;
                float w = (col.x1 - col.x0) - (padding * 2);
                DrawFitSingleLineNoEllipsis(text ?? "", x, yBase, w, f, size);
            }

            var esMx = CultureInfo.GetCultureInfo("es-MX");

            string carrera = ResolveCarreraNombre();
            string jefeNombre = string.IsNullOrWhiteSpace(req.DestinatarioNombre)
                ? "NOMBRE DEL JEFE(A)"
                : req.DestinatarioNombre.Trim();

            string jefeCargoFijo = "JEFE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN";

            float left = 72f;
            float right = rect.GetWidth() - 72f;
            float bodyW = right - left;

            // =========================
            // Encabezado derecho
            // =========================
            string fechaSolo = req.Fecha.ToString("dd'/'MMMM'/'yyyy", esMx).ToLower(esMx);
            string ciudadPrefix = $"{req.Ciudad}, ";

            float fechaY = 140f;
            float asuntoY = 154f;

            float ciudadWidth = font.GetWidth(ciudadPrefix, 9.6f);
            float fechaWidth = font.GetWidth(fechaSolo, 9.6f);
            float fechaStartX = right - (ciudadWidth + fechaWidth);

            DrawText(ciudadPrefix, fechaStartX, fechaY, font, 9.6f);
            DrawText(fechaSolo, fechaStartX + ciudadWidth, fechaY, font, 9.6f);

            DrawMixedRight(
                "ASUNTO:",
                font,
                9.6f,
                "Aceptación del Reporte Preliminar Residencia Profesional",
                fontBold,
                9.8f,
                right,
                asuntoY,
                4f
            );

            // =========================
            // Destinatario
            // =========================
            float destY = 214f;
            float cargoY = 231f;
            float presenteY = 248f;

            DrawText(jefeNombre, left, destY, fontBold, 9.9f);
            DrawText(jefeCargoFijo, left, cargoY, fontBold, 9.7f);
            DrawText("P R E S E N T E.", left, presenteY, fontBold, 9.7f);

            // =========================
            // Párrafo principal
            // =========================
            string oficio = string.IsNullOrWhiteSpace(req.Oficio) ? "ITO/XXX/2026" : req.Oficio.Trim();

            string parrafo =
                $"En respuesta al oficio {oficio}, informo a usted que el Reporte Preliminar de Residencias Profesionales " +
                $"del estudiante de la carrera de {carrera},";

            float parrafoX = 70f;
            float parrafoTopBaseline = 286f;
            float parrafoWidth = 505f;
            float parrafoSize = 9.6f;
            float leading = 11.4f;

            var parrafoLines = WrapLines(parrafo, font, parrafoSize, parrafoWidth);
            if (parrafoLines.Count > 2)
            {
                string second = parrafoLines[1];
                for (int i = 2; i < parrafoLines.Count; i++)
                    second += " " + parrafoLines[i];

                parrafoLines = new List<string>
        {
            parrafoLines[0],
            TruncateWithEllipsis(second, font, parrafoSize, parrafoWidth)
        };
            }

            for (int i = 0; i < parrafoLines.Count; i++)
                DrawText(parrafoLines[i], parrafoX, parrafoTopBaseline + (i * leading), font, parrafoSize);

            // =========================
            // Tabla dinámica
            // =========================
            float tableLeft = 71.424f;
            float tableRight = 573.964f;
            float tableWidth = tableRight - tableLeft;

            // Más ancho para título y un poco más para estudiante
            float w1 = 58f;   // No. Control
            float w2 = 112f;  // Estudiante
            float w3 = 205f;  // Título
            float w4 = 58f;   // Fecha inicio
            float w5 = tableWidth - (w1 + w2 + w3 + w4); // Fecha término

            (float x0, float x1) c1 = (tableLeft, tableLeft + w1);
            (float x0, float x1) c2 = (c1.x1, c1.x1 + w2);
            (float x0, float x1) c3 = (c2.x1, c2.x1 + w3);
            (float x0, float x1) c4 = (c3.x1, c3.x1 + w4);
            (float x0, float x1) c5 = (c4.x1, tableRight);

            float tableTop = 308f;
            float headerH = 22f;
            float pad = 4f;

            float rowSize = 9.0f;
            float rowLeading = rowSize + 1.2f;

            float rowH = 24f;
            float afterTableY = 0f;
            float aprobadoY = 0f;
            float boxYTop = 0f;
            float commentsY = 0f;
            float l1 = 0f;
            float l2 = 0f;
            float l3 = 0f;
            float atentamenteY = 0f;
            float firmaY = 0f;
            float firmaTextoY = 0f;
            float ccp1Y = 0f;
            float ccp2Y = 0f;

            // Zona segura antes del membretado inferior
            const float footerSafeTop = 700f;

            // Bloque inferior compacto para dar más espacio a la fila dinámica
            const float gapAfterTable = 12f;
            const float gapToAprobado = 24f;
            const float gapToComments = 28f;
            const float gapBetweenCommentLines = 16f;
            const float gapToAtentamente = 22f;
            const float gapToFirma = 54f;
            const float gapFirmaToTexto = 16f;
            const float gapTextoToCcp = 28f;
            const float gapBetweenCcp = 12f;

            float lowerBlockHeight =
                gapAfterTable +
                gapToAprobado +
                gapToComments +
                gapBetweenCommentLines +
                gapBetweenCommentLines +
                gapBetweenCommentLines +
                gapToAtentamente +
                gapToFirma +
                gapFirmaToTexto +
                gapTextoToCcp +
                gapBetweenCcp;

            float maxRowH = footerSafeTop - tableTop - headerH - lowerBlockHeight;
            if (maxRowH < 30f) maxRowH = 30f;

            while (true)
            {
                rowLeading = rowSize + 1.2f;

                float neededRowH = new[]
                {
            MeasureWrappedCellHeight(req.NoControl, font, rowSize, c1.x1 - c1.x0, pad, rowLeading),
            MeasureWrappedCellHeight(req.Estudiante, font, rowSize, c2.x1 - c2.x0, pad, rowLeading),
            MeasureWrappedCellHeight(req.TituloReporte, font, rowSize, c3.x1 - c3.x0, pad, rowLeading),
            MeasureWrappedCellHeight(SafeDate(req.FechaInicio), font, rowSize, c4.x1 - c4.x0, pad, rowLeading),
            MeasureWrappedCellHeight(SafeDate(req.FechaTermino), font, rowSize, c5.x1 - c5.x0, pad, rowLeading)
        }.Max();

                neededRowH = Math.Max(24f, neededRowH);

                rowH = Math.Min(neededRowH, maxRowH);

                afterTableY = tableTop + headerH + rowH + gapAfterTable;
                aprobadoY = afterTableY + gapToAprobado;
                boxYTop = aprobadoY - 13f;
                commentsY = aprobadoY + gapToComments;

                l1 = commentsY + gapBetweenCommentLines;
                l2 = l1 + gapBetweenCommentLines;
                l3 = l2 + gapBetweenCommentLines;

                atentamenteY = l3 + gapToAtentamente;
                firmaY = atentamenteY + gapToFirma;
                firmaTextoY = firmaY + gapFirmaToTexto;

                ccp1Y = firmaTextoY + gapTextoToCcp;
                ccp2Y = ccp1Y + gapBetweenCcp;

                if (neededRowH <= maxRowH || rowSize <= 7.6f)
                    break;

                rowSize -= 0.2f;
            }

            DrawRect(tableLeft, tableTop, tableWidth, headerH + rowH, 1f);
            DrawLine(tableLeft, tableTop + headerH, tableRight, tableTop + headerH, 1f);

            DrawLine(c1.x1, tableTop, c1.x1, tableTop + headerH + rowH, 1f);
            DrawLine(c2.x1, tableTop, c2.x1, tableTop + headerH + rowH, 1f);
            DrawLine(c3.x1, tableTop, c3.x1, tableTop + headerH + rowH, 1f);
            DrawLine(c4.x1, tableTop, c4.x1, tableTop + headerH + rowH, 1f);

            void DrawCentered(string text, (float x0, float x1) col, float yFromTopBaseline, PdfFont f, float size)
            {
                text ??= "";
                float w = col.x1 - col.x0;
                float tw = f.GetWidth(text, size);
                float x = col.x0 + (w - tw) / 2f;
                DrawText(text, x, yFromTopBaseline, f, size);
            }

            float headerBase = tableTop + 15f;
            float headerSize = 8.8f;

            DrawCentered("No. Control", c1, headerBase, font, headerSize);
            DrawCentered("Estudiante", c2, headerBase, font, headerSize);
            DrawCentered("Título del Reporte Preliminar", c3, headerBase, font, headerSize);
            DrawCentered("Fecha de inicio", c4, headerBase, font, 8.2f);
            DrawCentered("Fecha de termino", c5, headerBase, font, 8.2f);

            float dataTop = tableTop + headerH;
            float rowBase = dataTop + 4f + rowSize;

            FillCellSingleNoEllipsis(c1, req.NoControl, rowBase, font, rowSize);
            FillCellWrappedDynamic(c2, req.Estudiante, dataTop, rowH, rowSize, pad, rowLeading);
            FillCellWrappedDynamic(c3, req.TituloReporte, dataTop, rowH, rowSize, pad, rowLeading);
            FillCellSingleNoEllipsis(c4, SafeDate(req.FechaInicio), rowBase, font, rowSize);
            FillCellSingleNoEllipsis(c5, SafeDate(req.FechaTermino), rowBase, font, rowSize);

            // =========================
            // Dictamen + checkbox
            // =========================
            DrawText("Una vez que se ha revisado es considerado como:", 70f, afterTableY, font, 9.6f);
            DrawText("Aprobado:", 70f, aprobadoY, font, 9.6f);

            float boxX = 150f;
            float boxSize = 18f;
            DrawRect(boxX, boxYTop, boxSize, boxSize, 1f);

            bool aprobado = string.Equals(req.Dictamen?.Trim(), "APROBADO", StringComparison.OrdinalIgnoreCase);
            if (aprobado)
            {
                float m = 3f;
                DrawLine(boxX + m, boxYTop + m, boxX + boxSize - m, boxYTop + boxSize - m, 1.2f);
                DrawLine(boxX + m, boxYTop + boxSize - m, boxX + boxSize - m, boxYTop + m, 1.2f);
            }

            // =========================
            // Comentarios / observaciones
            // =========================
            DrawText("Comentarios  u  observaciones:", 70f, commentsY, font, 9.6f);

            float lineLeft = 70f;
            float lineRight = 575f;

            DrawLine(lineLeft, l1, lineRight, l1, 1f);
            DrawLine(lineLeft, l2, lineRight, l2, 1f);
            DrawLine(lineLeft, l3, lineRight, l3, 1f);

            var commLines = WrapLines(req.Comentarios ?? "", font, 9.2f, 500f);
            if (commLines.Count > 3)
            {
                string third = commLines[2];
                for (int i = 3; i < commLines.Count; i++)
                    third += " " + commLines[i];

                commLines = new List<string>
        {
            commLines[0],
            commLines[1],
            TruncateWithoutEllipsis(third, font, 9.2f, 500f)
        };
            }

            float commX = 72f;
            float[] commBase = { l1 - 3f, l2 - 3f, l3 - 3f };
            for (int i = 0; i < 3; i++)
            {
                string t = i < commLines.Count ? commLines[i] : "";
                DrawText(t, commX, commBase[i], font, 9.2f);
            }

            // =========================
            // Firma
            // =========================
            DrawText("Atentamente", 70f, atentamenteY, fontBold, 9.9f);

            float firmaLeft = 70f;
            float firmaRight = 300f;
            DrawLine(firmaLeft, firmaY, firmaRight, firmaY, 1f);
            DrawText("Nombre y Firma del Asesor interno", 70f, firmaTextoY, fontBold, 9.9f);

            if (!string.IsNullOrWhiteSpace(req.AsesorInterno))
            {
                float nameSize = 9.0f;
                var name = req.AsesorInterno.Trim();
                float nameWidth = font.GetWidth(name, nameSize);
                float maxNameWidth = firmaRight - firmaLeft - 8f;

                if (nameWidth > maxNameWidth)
                    DrawFitSingleLineNoEllipsis(name, firmaLeft + 4f, firmaY - 6f, maxNameWidth, font, nameSize, 7.6f);
                else
                    DrawText(name, firmaLeft + ((firmaRight - firmaLeft - nameWidth) / 2f), firmaY - 6f, font, nameSize);
            }

            // =========================
            // C.c.p.
            // =========================
            DrawText("C.c.p. Jefatura de la División de Estudios Profesionales", 70f, ccp1Y, font, 8.8f);
            DrawText("C.c.p. Archivo", 70f, ccp2Y, font, 8.8f);

            pdf.Close();
            return output.ToArray();
        }

        public byte[] BuildOficiosAsignacionRevisores(byte[] templatePdf, OficiosAsignacionRevisoresRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var outPdf = new PdfDocument(writer);

            PdfFont font = LoadFontFromCandidates(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Regular.ttf"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "NotoSans-Regular.ttf")
            );

            PdfFont fontBold = LoadFontFromCandidates(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Bold.ttf"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "NotoSans-Bold.ttf")
            );

            var esMx = CultureInfo.GetCultureInfo("es-MX");

            foreach (var rev in req.Revisores ?? new List<OficioRevisorItem>())
            {
                PdfPage page;
                if (templatePdf != null && templatePdf.Length > 0)
                {
                    using var src = new PdfDocument(new PdfReader(new MemoryStream(templatePdf)));
                    src.CopyPagesTo(1, 1, outPdf);
                    page = outPdf.GetLastPage();
                }
                else
                {
                    page = outPdf.AddNewPage(PageSize.LETTER);
                }

                var rect = page.GetPageSizeWithRotation();
                var canvas = new PdfCanvas(page);

                // ── Compensar flip vertical del membrete escaneado ───────────────
                {
                    var pageRotation = page.GetRotation();
                    bool hasYFlip = false;

                    if (pageRotation == 0)
                    {
                        try
                        {
                            var pageDict = page.GetPdfObject();
                            var contentsObj = pageDict.Get(iText.Kernel.Pdf.PdfName.Contents);
                            iText.Kernel.Pdf.PdfStream? firstStream = null;

                            if (contentsObj is iText.Kernel.Pdf.PdfArray arr && arr.Size() > 0)
                                firstStream = arr.GetAsStream(0);
                            else if (contentsObj is iText.Kernel.Pdf.PdfStream s)
                                firstStream = s;

                            if (firstStream != null)
                            {
                                var bytes = firstStream.GetBytes();
                                var preview = System.Text.Encoding.Latin1.GetString(bytes, 0,
                                    Math.Min(bytes.Length, 80));
                                hasYFlip = System.Text.RegularExpressions.Regex.IsMatch(
                                    preview, @"1\s+0\s+0\s+-1\s+0\s+[\d.]+\s+cm");
                            }
                        }
                        catch { }
                    }

                    float w = rect.GetWidth();
                    float h = rect.GetHeight();

                    if (hasYFlip)
                        canvas.ConcatMatrix(1, 0, 0, -1, 0, h);
                    else if (pageRotation == 90)
                        canvas.ConcatMatrix(0, 1, -1, 0, h, 0);
                    else if (pageRotation == 180)
                        canvas.ConcatMatrix(-1, 0, 0, -1, w, h);
                    else if (pageRotation == 270)
                        canvas.ConcatMatrix(0, -1, 1, 0, 0, w);
                }
                // ────────────────────────────────────────────────────────────────

                float Y(float yFromTop) => rect.GetHeight() - yFromTop;

                void DrawText(string text, float x, float yFromTopBaseline, PdfFont f, float size)
                {
                    canvas.BeginText();
                    canvas.SetFontAndSize(f, size);
                    canvas.MoveText(x, Y(yFromTopBaseline));
                    canvas.ShowText(text ?? "");
                    canvas.EndText();
                }

                void DrawLine(float x1, float yFromTop, float x2, float y2FromTop, float width = 0.85f)
                {
                    canvas.SaveState();
                    canvas.SetLineWidth(width);
                    canvas.MoveTo(x1, Y(yFromTop));
                    canvas.LineTo(x2, Y(y2FromTop));
                    canvas.Stroke();
                    canvas.RestoreState();
                }

                void DrawRect(float x, float yFromTopTop, float w, float h, float lineWidth = 0.85f)
                {
                    canvas.SaveState();
                    canvas.SetLineWidth(lineWidth);
                    canvas.Rectangle(x, Y(yFromTopTop + h), w, h);
                    canvas.Stroke();
                    canvas.RestoreState();
                }

                void DrawTextRight(string text, float rightX, float yFromTopBaseline, PdfFont f, float size)
                {
                    text ??= "";
                    float textWidth = f.GetWidth(text, size);
                    DrawText(text, rightX - textWidth, yFromTopBaseline, f, size);
                }

                void DrawTextCentered(string text, float centerX, float yFromTopBaseline, PdfFont f, float size)
                {
                    text ??= "";
                    float textWidth = f.GetWidth(text, size);
                    DrawText(text, centerX - (textWidth / 2f), yFromTopBaseline, f, size);
                }

                void DrawMixedRight(
                    string leftText, PdfFont leftFont, float leftSize,
                    string rightText, PdfFont rightFont, float rightSize,
                    float rightX, float yFromTopBaseline, float gap = 3f)
                {
                    leftText ??= "";
                    rightText ??= "";

                    float leftWidth = leftFont.GetWidth(leftText, leftSize);
                    float rightWidth = rightFont.GetWidth(rightText, rightSize);

                    float startX = rightX - (leftWidth + gap + rightWidth);

                    DrawText(leftText, startX, yFromTopBaseline, leftFont, leftSize);
                    DrawText(rightText, startX + leftWidth + gap, yFromTopBaseline, rightFont, rightSize);
                }


                string TruncateWithoutEllipsis(string text, PdfFont f, float size, float maxWidth)
                {
                    text ??= "";
                    if (f.GetWidth(text, size) <= maxWidth) return text;

                    var t = text;
                    while (t.Length > 0 && f.GetWidth(t, size) > maxWidth)
                        t = t.Substring(0, t.Length - 1);

                    return t;
                }

                List<string> WrapLines(string text, PdfFont f, float size, float maxWidth)
                {
                    text ??= "";
                    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var lines = new List<string>();
                    string line = "";

                    foreach (var w in words)
                    {
                        var test = string.IsNullOrEmpty(line) ? w : $"{line} {w}";
                        if (f.GetWidth(test, size) > maxWidth && !string.IsNullOrEmpty(line))
                        {
                            lines.Add(line);
                            line = w;
                        }
                        else
                        {
                            line = test;
                        }
                    }

                    if (!string.IsNullOrEmpty(line))
                        lines.Add(line);

                    return lines;
                }

                float MeasureWrappedHeight(string text, PdfFont f, float size, float width, float padding, float leading, float minHeight = 22f)
                {
                    float innerW = Math.Max(0, width - (padding * 2));
                    var lines = WrapLines(text, f, size, innerW);
                    int count = Math.Max(1, lines.Count);
                    return Math.Max(minHeight, (count * leading) + (padding * 2) + 1f);
                }

                void DrawWrappedInCell(string text, float x, float top, float w, float h, PdfFont f, float size, float padding, float leading)
                {
                    text ??= "";

                    float innerW = Math.Max(0, w - (padding * 2));
                    var lines = WrapLines(text, f, size, innerW);
                    if (lines.Count == 0) lines = new List<string> { "" };

                    int maxLines = Math.Max(1, (int)Math.Floor((h - (padding * 2)) / leading));
                    if (lines.Count > maxLines)
                    {
                        string last = string.Join(" ", lines.Skip(maxLines - 1));
                        lines = lines.Take(maxLines - 1).ToList();
                        lines.Add(TruncateWithoutEllipsis(last, f, size, innerW));
                    }

                    float baseY = top + padding + size;
                    for (int i = 0; i < lines.Count; i++)
                        DrawText(lines[i], x + padding, baseY + (i * leading), f, size);
                }

                void DrawWrappedParagraph(string text, float x, float top, float width, PdfFont f, float size, float leading)
                {
                    var lines = WrapLines(text, f, size, width);
                    for (int i = 0; i < lines.Count; i++)
                        DrawText(lines[i], x, top + (i * leading), f, size);
                }

                string CleanReviewerName(string? raw)
                {
                    var s = (raw ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(s)) return "";

                    var markers = new[]
                    {
                " · As:",
                "· As:",
                " As:",
                "·As:",
                " Rev:",
                " Ant:",
                " Tot:"
            };

                    int cut = -1;
                    foreach (var m in markers)
                    {
                        int idx = s.IndexOf(m, StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0 && (cut < 0 || idx < cut))
                            cut = idx;
                    }

                    return cut >= 0 ? s.Substring(0, cut).Trim() : s;
                }

                float left = 72f;
                float right = rect.GetWidth() - 72f;
                float bodyW = right - left;
                float centerX = left + (bodyW / 2f);

                // =========================
                // Encabezado
                // =========================
                string ciudad = string.IsNullOrWhiteSpace(req.Ciudad) ? "Oaxaca de Juárez, Oaxaca" : req.Ciudad.Trim();
                string oficio = string.IsNullOrWhiteSpace(req.Oficio) ? "__________" : req.Oficio.Trim();
                string asunto = string.IsNullOrWhiteSpace(req.Asunto) ? "Revisor de Residencia Profesional" : req.Asunto.Trim();

                string fechaSolo = req.Fecha.ToString("dd'/'MMMM'/'yyyy", esMx).ToLower(esMx);
                string ciudadPrefix = $"{ciudad} ";

                float fechaY = 140f;
                float oficioY = 154f;
                float asuntoY = 168f;

                float ciudadWidth = font.GetWidth(ciudadPrefix, 9.6f);
                float fechaWidth = font.GetWidth(fechaSolo, 9.6f);
                float fechaStartX = right - (ciudadWidth + fechaWidth);

                DrawText(ciudadPrefix, fechaStartX, fechaY, font, 9.6f);
                DrawText(fechaSolo, fechaStartX + ciudadWidth, fechaY, font, 9.6f);

                DrawTextRight($"OFICIO No. {oficio}", right, oficioY, fontBold, 9.9f);

                DrawMixedRight(
                    "ASUNTO:",
                    font,
                    9.6f,
                    asunto,
                    fontBold,
                    9.8f,
                    right,
                    asuntoY,
                    4f
                );

                // =========================
                // Destinatario
                // =========================
                string revisorNombre = CleanReviewerName(rev.RevisorNombre);
                string revisorCargo = string.IsNullOrWhiteSpace(rev.RevisorCargoLinea1)
                    ? "CATEDRATICO(A) DEL I.T. DE OAXACA"
                    : rev.RevisorCargoLinea1.Trim();

                float destY = 214f;
                float cargoY = 231f;
                float presenteY = 248f;

                DrawText(revisorNombre, left, destY, fontBold, 9.9f);
                DrawText(revisorCargo, left, cargoY, fontBold, 9.7f);
                DrawText("P R E S E N T E.", left, presenteY, fontBold, 9.7f);

                // =========================
                // Intro
                // =========================
                string intro =
                    "Por este conducto informo a usted que ha sido asignado para fungir como Revisor de los Proyectos de " +
                    "Residencia Profesional que a continuación se describen:";

                float introTop = 285f;
                float bodySize = 9.4f;
                float bodyLeading = 10.8f;

                var introLines = WrapLines(intro, font, bodySize, bodyW);
                for (int i = 0; i < introLines.Count; i++)
                    DrawText(introLines[i], left, introTop + (i * bodyLeading), font, bodySize);

                // =========================
                // Agrupar por proyecto + asesor
                // =========================
                var rawRows = (rev.Rows ?? new List<OficioRevisorRow>())
                    .Where(r => r != null)
                    .ToList();

                var groups = rawRows
                    .GroupBy(r => new
                    {
                        Proyecto = (r.Proyecto ?? "").Trim().ToUpperInvariant(),
                        Asesor = (r.Asesor ?? "").Trim().ToUpperInvariant()
                    })
                    .Select(g => new
                    {
                        Proyecto = g.First().Proyecto?.Trim() ?? "",
                        Asesor = g.First().Asesor?.Trim() ?? "",
                        Integrantes = g.Select(x => new
                        {
                            NoControl = (x.NoControl ?? "").Trim(),
                            Estudiante = (x.Estudiante ?? "").Trim()
                        }).ToList()
                    })
                    .ToList();

                if (groups.Count == 0)
                {
                    groups = new[]
                    {
                new
                {
                    Proyecto = "",
                    Asesor = "",
                    Integrantes = new[]
                    {
                        new { NoControl = "", Estudiante = "" }
                    }.ToList()
                }
            }.ToList();
                }

                // =========================
                // Tabla dinámica
                // =========================
                float tableLeft = left;
                float tableWidth = bodyW;

                float w1 = 82f;   // No. control
                float w2 = 122f;  // Estudiante
                float w3 = 184f;  // Proyecto
                float w4 = tableWidth - (w1 + w2 + w3); // Asesor

                (float x0, float x1) c1 = (tableLeft, tableLeft + w1);
                (float x0, float x1) c2 = (c1.x1, c1.x1 + w2);
                (float x0, float x1) c3 = (c2.x1, c2.x1 + w3);
                (float x0, float x1) c4 = (c3.x1, tableLeft + tableWidth);

                float tableTop = 322f;
                float headerH = 22f;
                float headerFont = 8.8f;
                float rowFont = 8.9f;
                float rowLeading = rowFont + 1.15f;
                float cellPad = 4f;

                float pSize = 9.4f;
                float pLeading = 10.8f;

                string p2 =
                    "Así mismo, le solicito dar el seguimiento pertinente a la realización del proyecto aplicando los lineamientos " +
                    "establecidos para ello, en el procedimiento para Residencia Profesional.";

                string p3 =
                    "Agradezco de antemano su valioso apoyo en esta importante actividad para la formación profesional de nuestros estudiantes.";

                float tableH = 0f;
                List<dynamic> layouts = new();

                const float footerSafeTop = 690f;

                while (true)
                {
                    rowLeading = rowFont + 1.15f;
                    layouts = new List<dynamic>();

                    foreach (var g in groups)
                    {
                        var memberHeights = new List<float>();
                        foreach (var m in g.Integrantes)
                        {
                            float hNo = MeasureWrappedHeight(m.NoControl, font, rowFont, c1.x1 - c1.x0, cellPad, rowLeading, 20f);
                            float hEst = MeasureWrappedHeight(m.Estudiante, font, rowFont, c2.x1 - c2.x0, cellPad, rowLeading, 20f);
                            memberHeights.Add(Math.Max(hNo, hEst));
                        }

                        float membersTotal = memberHeights.Sum();

                        float hProyecto = MeasureWrappedHeight(g.Proyecto, font, rowFont, c3.x1 - c3.x0, cellPad, rowLeading, 20f);
                        float hAsesor = MeasureWrappedHeight(g.Asesor, font, rowFont, c4.x1 - c4.x0, cellPad, rowLeading, 20f);

                        float groupTotal = Math.Max(membersTotal, Math.Max(hProyecto, hAsesor));

                        if (groupTotal > membersTotal)
                            memberHeights[memberHeights.Count - 1] += (groupTotal - membersTotal);

                        layouts.Add(new
                        {
                            Proyecto = g.Proyecto,
                            Asesor = g.Asesor,
                            Integrantes = g.Integrantes,
                            MemberHeights = memberHeights,
                            TotalHeight = groupTotal
                        });
                    }

                    tableH = headerH + layouts.Sum(x => (float)x.TotalHeight);

                    float afterTableYTmp = tableTop + tableH + 14f;

                    var p2Lines = WrapLines(p2, font, pSize, bodyW);
                    var p3Lines = WrapLines(p3, font, pSize, bodyW);

                    float p3YTmp = afterTableYTmp + (p2Lines.Count * pLeading) + 6f;

                    float atentamenteYTmp = p3YTmp + (p3Lines.Count * pLeading) + 24f;
                    float lema1YTmp = atentamenteYTmp + 16f;
                    float lema2YTmp = atentamenteYTmp + 29f;
                    float deptoYTmp = atentamenteYTmp + 82f;
                    float firmaNombreYTmp = deptoYTmp + 15f;
                    float firmaCargoYTmp = firmaNombreYTmp + 15f;
                    float ccp1YTmp = firmaCargoYTmp + 28f;
                    float ccp2YTmp = ccp1YTmp + 12f;

                    if (ccp2YTmp <= footerSafeTop || rowFont <= 7.6f)
                        break;

                    rowFont -= 0.2f;
                    if (rowFont < 7.6f) rowFont = 7.6f;
                }

                DrawRect(tableLeft, tableTop, tableWidth, tableH, 0.85f);
                DrawLine(c1.x1, tableTop, c1.x1, tableTop + tableH, 0.85f);
                DrawLine(c2.x1, tableTop, c2.x1, tableTop + tableH, 0.85f);
                DrawLine(c3.x1, tableTop, c3.x1, tableTop + tableH, 0.85f);
                DrawLine(tableLeft, tableTop + headerH, tableLeft + tableWidth, tableTop + headerH, 0.85f);

                void DrawCentered(string text, (float x0, float x1) col, float yFromTopBaseline, PdfFont f, float size)
                {
                    text ??= "";
                    float w = col.x1 - col.x0;
                    float tw = f.GetWidth(text, size);
                    float x = col.x0 + (w - tw) / 2f;
                    DrawText(text, x, yFromTopBaseline, f, size);
                }

                float headerBase = tableTop + 15f;
                DrawCentered("N° CONTROL:", c1, headerBase, font, headerFont);
                DrawCentered("ESTUDIANTE", c2, headerBase, font, headerFont);
                DrawCentered("PROYECTO", c3, headerBase, font, headerFont);
                DrawCentered("ASESOR", c4, headerBase, font, headerFont);

                float currentTop = tableTop + headerH;

                foreach (var g in layouts)
                {
                    float groupTop = currentTop;
                    float groupHeight = (float)g.TotalHeight;

                    DrawWrappedInCell(g.Proyecto, c3.x0, groupTop, c3.x1 - c3.x0, groupHeight, font, rowFont, cellPad, rowLeading);
                    DrawWrappedInCell(g.Asesor, c4.x0, groupTop, c4.x1 - c4.x0, groupHeight, font, rowFont, cellPad, rowLeading);

                    float memberTop = groupTop;
                    for (int i = 0; i < g.Integrantes.Count; i++)
                    {
                        float rh = (float)g.MemberHeights[i];
                        var m = g.Integrantes[i];

                        DrawWrappedInCell(m.NoControl, c1.x0, memberTop, c1.x1 - c1.x0, rh, font, rowFont, cellPad, rowLeading);
                        DrawWrappedInCell(m.Estudiante, c2.x0, memberTop, c2.x1 - c2.x0, rh, font, rowFont, cellPad, rowLeading);

                        if (i < g.Integrantes.Count - 1)
                            DrawLine(tableLeft, memberTop + rh, c2.x1, memberTop + rh, 0.85f);

                        memberTop += rh;
                    }

                    DrawLine(tableLeft, groupTop + groupHeight, tableLeft + tableWidth, groupTop + groupHeight, 0.85f);
                    currentTop += groupHeight;
                }

                // =========================
                // Cierre
                // =========================
                float afterTableY = tableTop + tableH + 14f;
                DrawWrappedParagraph(p2, left, afterTableY, bodyW, font, pSize, pLeading);

                var p2LinesFinal = WrapLines(p2, font, pSize, bodyW);
                float p3Y = afterTableY + (p2LinesFinal.Count * pLeading) + 6f;
                DrawWrappedParagraph(p3, left, p3Y, bodyW, font, pSize, pLeading);

                var p3LinesFinal = WrapLines(p3, font, pSize, bodyW);

                // =========================
                // Firma / atento
                // =========================
                string firmaNombre = string.IsNullOrWhiteSpace(req.FirmaNombre)
    ? "NOMBRE DE QUIEN FIRMA"
    : req.FirmaNombre.Trim();

string firmaCargo = string.IsNullOrWhiteSpace(req.FirmaCargoLinea1)
    ? "JEFA(E) DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN"
    : req.FirmaCargoLinea1.Trim();

float atentamenteY = p3Y + (p3LinesFinal.Count * pLeading) + 24f;
float lema1Y = atentamenteY + 16f;
float lema2Y = atentamenteY + 29f;
float firmaNombreY = atentamenteY + 82f;
float firmaCargoY = firmaNombreY + 15f;
float ccp1Y = firmaCargoY + 28f;
float ccp2Y = ccp1Y + 12f;

DrawTextCentered("A T E N T A M E N T E", centerX, atentamenteY, fontBold, 9.9f);
DrawTextCentered("Excelencia en Educación Tecnológica®", centerX, lema1Y, font, 9.2f);
DrawTextCentered("“Tecnología Propia e Independencia Económica”", centerX, lema2Y, font, 9.2f);

DrawTextCentered(firmaNombre, centerX, firmaNombreY, fontBold, 9.8f);
DrawTextCentered(firmaCargo, centerX, firmaCargoY, fontBold, 9.5f);

DrawText("ccp. Expediente", left, ccp1Y, font, 8.8f);
DrawText("MMH/mmvh", left, ccp2Y, font, 8.8f);
            }

            outPdf.Close();
            return ms.ToArray();
        }

        public byte[] BuildOficioAsignacionAsesorInterno(byte[] templatePdf, OficioAsignacionAsesorInternoRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            using var output = new MemoryStream();
            using var writer = new PdfWriter(output);
            using var pdf = new PdfDocument(writer);

            PdfPage page;

            // 1) Fondo como BACKGROUND
            if (templatePdf != null && templatePdf.Length > 0)
            {
                using var templateStream = new MemoryStream(templatePdf);
                using var reader = new PdfReader(templateStream);
                using var src = new PdfDocument(reader);

                page = src.GetFirstPage().CopyTo(pdf);
                pdf.AddPage(page);
            }
            else
            {
                page = pdf.AddNewPage(PageSize.LETTER);
            }

            var rect = page.GetPageSizeWithRotation();
            var canvas = new PdfCanvas(page);

            // ── Compensar flip vertical del membrete escaneado ───────────────
            // Algunos membretes escaneados tienen "1 0 0 -1 0 H cm" embebido
            // en su stream de contenido (flip Y). Detectamos esto leyendo el
            // primer operador cm del stream y aplicamos la misma transformación
            // a nuestro canvas para que las coordenadas queden alineadas.
            {
                var pageRotation = page.GetRotation();
                bool hasYFlip = false;

                if (pageRotation == 0)
                {
                    try
                    {
                        var pageDict = page.GetPdfObject();
                        var contentsObj = pageDict.Get(iText.Kernel.Pdf.PdfName.Contents);
                        iText.Kernel.Pdf.PdfStream? firstStream = null;

                        if (contentsObj is iText.Kernel.Pdf.PdfArray arr && arr.Size() > 0)
                            firstStream = arr.GetAsStream(0);
                        else if (contentsObj is iText.Kernel.Pdf.PdfStream s)
                            firstStream = s;

                        if (firstStream != null)
                        {
                            var bytes = firstStream.GetBytes();
                            var preview = System.Text.Encoding.Latin1.GetString(bytes, 0,
                                Math.Min(bytes.Length, 80));
                            // Detectar "1 0 0 -1 0 <número> cm" al inicio del stream
                            hasYFlip = System.Text.RegularExpressions.Regex.IsMatch(
                                preview, @"1\s+0\s+0\s+-1\s+0\s+[\d.]+\s+cm");
                        }
                    }
                    catch { /* si falla la detección, no aplicamos nada */ }
                }

                float w = rect.GetWidth();
                float h = rect.GetHeight();

                if (hasYFlip)
                    canvas.ConcatMatrix(1, 0, 0, -1, 0, h);   // mismo flip que el membrete
                else if (pageRotation == 90)
                    canvas.ConcatMatrix(0, 1, -1, 0, h, 0);
                else if (pageRotation == 180)
                    canvas.ConcatMatrix(-1, 0, 0, -1, w, h);
                else if (pageRotation == 270)
                    canvas.ConcatMatrix(0, -1, 1, 0, 0, w);
            }
            // ────────────────────────────────────────────────────────────────

            PdfFont font = LoadFontFromCandidates(
            System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Regular.ttf"),
            System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "NotoSans-Regular.ttf")
        );

            PdfFont fontBold = LoadFontFromCandidates(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Bold.ttf"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "NotoSans-Bold.ttf")
            );

            var esMx = CultureInfo.GetCultureInfo("es-MX");

            // Y desde arriba
            float Y(float yFromTop) => rect.GetHeight() - yFromTop;

            void DrawText(string text, float x, float yFromTopBaseline, PdfFont f, float size)
            {
                canvas.BeginText();
                canvas.SetFontAndSize(f, size);
                canvas.MoveText(x, Y(yFromTopBaseline));
                canvas.ShowText(text ?? "");
                canvas.EndText();
            }

            void DrawTextCentered(string text, float centerX, float yFromTopBaseline, PdfFont f, float size)
            {
                text ??= "";
                float textWidth = f.GetWidth(text, size);
                DrawText(text, centerX - (textWidth / 2f), yFromTopBaseline, f, size);
            }

            void DrawTextRight(string text, float rightX, float yFromTopBaseline, PdfFont f, float size)
            {
                text ??= "";
                float textWidth = f.GetWidth(text, size);
                DrawText(text, rightX - textWidth, yFromTopBaseline, f, size);
            }

            void DrawMixedRight(
                string leftText, PdfFont leftFont, float leftSize,
                string rightText, PdfFont rightFont, float rightSize,
                float rightX, float yFromTopBaseline, float gap = 3f)
            {
                leftText ??= "";
                rightText ??= "";

                float leftWidth = leftFont.GetWidth(leftText, leftSize);
                float rightWidth = rightFont.GetWidth(rightText, rightSize);

                float startX = rightX - (leftWidth + gap + rightWidth);

                DrawText(leftText, startX, yFromTopBaseline, leftFont, leftSize);
                DrawText(rightText, startX + leftWidth + gap, yFromTopBaseline, rightFont, rightSize);
            }

            void DrawLine(float x1, float yFromTop, float x2, float y2FromTop, float width = 0.85f)
            {
                canvas.SaveState();
                canvas.SetLineWidth(width);
                canvas.MoveTo(x1, Y(yFromTop));
                canvas.LineTo(x2, Y(y2FromTop));
                canvas.Stroke();
                canvas.RestoreState();
            }

            void DrawRect(float x, float yFromTopTop, float w, float h, float lineWidth = 0.85f)
            {
                canvas.SaveState();
                canvas.SetLineWidth(lineWidth);
                canvas.Rectangle(x, Y(yFromTopTop + h), w, h);
                canvas.Stroke();
                canvas.RestoreState();
            }

            void DrawTextCenteredFit(string text, float centerX, float yFromTopBaseline, float maxWidth, PdfFont f, float startSize, float minSize = 8.25f)
            {
                text ??= "";
                float s = startSize;

                while (s > minSize && f.GetWidth(text, s) > maxWidth)
                    s -= 0.10f;

                if (f.GetWidth(text, s) > maxWidth)
                    text = TruncateWithEllipsis(text, f, s, maxWidth);

                float tw = f.GetWidth(text, s);
                DrawText(text, centerX - (tw / 2f), yFromTopBaseline, f, s);
            }

            void DrawJustifiedLine(string text, float x, float yFromTopBaseline, float maxWidth, PdfFont f, float size)
            {
                text ??= "";
                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length <= 1)
                {
                    DrawText(text, x, yFromTopBaseline, f, size);
                    return;
                }

                float wordsWidth = words.Sum(w => f.GetWidth(w, size));
                int gaps = words.Length - 1;
                float gapWidth = (maxWidth - wordsWidth) / gaps;

                float cursor = x;
                foreach (var word in words)
                {
                    DrawText(word, cursor, yFromTopBaseline, f, size);
                    cursor += f.GetWidth(word, size) + gapWidth;
                }
            }

            void DrawJustifiedParagraph(string text, float x, float yFromTopBaseline, float maxWidth, PdfFont f, float size, float leading, int maxLines)
            {
                var lines = WrapLinesMax(text, f, size, maxWidth, maxLines);

                for (int i = 0; i < lines.Count; i++)
                {
                    bool isLast = i == lines.Count - 1;
                    if (isLast)
                        DrawText(lines[i], x, yFromTopBaseline + (i * leading), f, size);
                    else
                        DrawJustifiedLine(lines[i], x, yFromTopBaseline + (i * leading), maxWidth, f, size);
                }
            }




            string TruncateWithEllipsis(string text, PdfFont f, float size, float maxWidth)
            {
                text ??= "";
                const string ell = "…";
                if (f.GetWidth(text, size) <= maxWidth) return text;

                var t = text;
                while (t.Length > 0 && f.GetWidth(t + ell, size) > maxWidth)
                    t = t.Substring(0, t.Length - 1);

                return t.Length == 0 ? "" : t + ell;
            }

            float FitSize(string text, PdfFont f, float start, float min, float maxWidth)
            {
                text ??= "";
                float s = start;
                while (s > min && f.GetWidth(text, s) > maxWidth)
                    s -= 0.25f;
                return s;
            }

            List<string> WrapLines(string text, PdfFont f, float size, float maxWidth)
            {
                text ??= "";
                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lines = new List<string>();
                string line = "";

                foreach (var w in words)
                {
                    var test = string.IsNullOrEmpty(line) ? w : $"{line} {w}";
                    if (f.GetWidth(test, size) > maxWidth && !string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                        line = w;
                    }
                    else
                    {
                        line = test;
                    }
                }

                if (!string.IsNullOrEmpty(line))
                    lines.Add(line);

                return lines;
            }

            List<string> WrapLinesMax(string text, PdfFont f, float size, float maxWidth, int maxLines)
            {
                var lines = WrapLines(text, f, size, maxWidth);
                if (lines.Count <= maxLines) return lines;

                string last = lines[maxLines - 1];
                for (int i = maxLines; i < lines.Count; i++)
                    last += " " + lines[i];

                var result = lines.Take(maxLines - 1).ToList();
                result.Add(TruncateWithEllipsis(last, f, size, maxWidth));
                return result;
            }

            void DrawWrappedInBox(string text, float x, float yFromTopTop, float w, float h, PdfFont f, float size, float padding = 3.5f)
            {
                text ??= "";

                float innerW = Math.Max(0, w - (padding * 2));
                float innerH = Math.Max(0, h - (padding * 2));

                float s = FitSize(text, f, size, 7.75f, innerW);
                if (f.GetWidth(text, s) <= innerW)
                {
                    float baseY = yFromTopTop + padding + s;
                    DrawText(TruncateWithEllipsis(text, f, s, innerW), x + padding, baseY, f, s);
                    return;
                }

                float ws = Math.Min(size, 9.0f);
                var lines = WrapLines(text, f, ws, innerW);

                float leading = ws + 1.60f;
                int maxLines = (int)Math.Floor(innerH / leading);
                if (maxLines < 1) maxLines = 1;

                if (lines.Count > maxLines)
                {
                    string last = lines[maxLines - 1];
                    for (int i = maxLines; i < lines.Count; i++)
                        last += " " + lines[i];

                    lines = lines.Take(maxLines - 1).ToList();
                    lines.Add(TruncateWithEllipsis(last, f, ws, innerW));
                }

                float yBase = yFromTopTop + padding + ws;
                for (int i = 0; i < lines.Count; i++)
                    DrawText(lines[i], x + padding, yBase + (i * leading), f, ws);
            }

            // -------------------------
            // Datos normalizados
            // -------------------------
            string ciudad = string.IsNullOrWhiteSpace(req.Ciudad) ? "Oaxaca de Juárez, Oaxaca" : req.Ciudad.Trim();
            string oficio = string.IsNullOrWhiteSpace(req.Oficio) ? "__________" : req.Oficio.Trim();

            string destinatario = (req.DestinatarioNombre ?? "").Trim();
            string cargoDestinatario = (req.DestinatarioCargoLinea1 ?? "").Trim();
            string proyecto = (req.NombreProyecto ?? "").Trim();
            string empresa = (req.Empresa ?? "").Trim();
            string carrera = (req.Carrera ?? "").Trim();
            string periodo = (req.PeriodoRealizacion ?? "").Trim();

            string residenteTexto;
            if (req.Residentes == null || req.Residentes.Count == 0)
            {
                residenteTexto = "";
            }
            else if (req.Residentes.Count == 1)
            {
                residenteTexto = req.Residentes[0]?.Trim() ?? "";
            }
            else
            {
                residenteTexto = string.Join("  |  ", req.Residentes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select((x, i) => $"{i + 1}) {x.Trim()}"));
            }

            // -------------------------
            // Layout base
            // -------------------------
            float left = 72f;
            float right = rect.GetWidth() - 72f;
            float bodyW = right - left;
            float centerX = left + (bodyW / 2f);

            // =========================
            // Encabezado derecho
            // =========================
            string fechaSolo = req.Fecha.ToString("dd'/'MMMM'/'yyyy", esMx).ToLower(esMx);
            string ciudadPrefix = $"{ciudad} ";

            float headerRight = right;
            float fechaY = 140f;
            float oficioY = 154f;
            float asuntoY = 168f;

            float ciudadWidth = font.GetWidth(ciudadPrefix, headerSize);
            float fechaWidth = font.GetWidth(fechaSolo, headerSize);
            float fechaStartX = headerRight - (ciudadWidth + fechaWidth);

            DrawText(ciudadPrefix, fechaStartX, fechaY, font, headerSize);
            DrawText(fechaSolo, fechaStartX + ciudadWidth, fechaY, font, headerSize);

            DrawTextRight($"OFICIO No. {oficio}", headerRight, oficioY, fontBold, oficioSize);

            DrawMixedRight(
                "ASUNTO:",
                font,
                asuntoLabelSize,
                "Asesor Interno de Residencia Profesional",
                fontBold,
                asuntoValueSize,
                headerRight,
                asuntoY,
                4f
            );
            // =========================
            // Destinatario
            // =========================
            float destY = 228f;
            float cargoY = 245f;
            float presenteY = 262f;

            DrawText(destinatario, left, destY, fontBold, destinatarioSize);

            DrawText(cargoDestinatario, left, cargoY, fontBold, cargoSize);
            DrawText("P R E S E N T E.", left, presenteY, fontBold, cargoSize);

            // =========================
            // Intro
            // =========================
            string intro =
                "Por este conducto informo a usted que ha sido asignado para fungir como Asesor Interno del Proyecto de " +
                "Residencias Profesionales que a continuación se describe:";

            float bodySize = 9.6f;
            float introTop = 285f;
            DrawJustifiedParagraph(intro, left, introTop, bodyW, font, bodySize, bodyLeading, 3);


            // =========================
            // Tabla centrada
            // =========================
            float tableW = 470f;
            float tableX = left + ((bodyW - tableW) / 2f);
            float tableTop = 310f;
            float col1 = 168f;
            float col2 = tableW - col1;

            float r0 = 22f;
            float r1 = 26f;
            float r2 = 24f;
            float r3 = 46f;
            float r4 = 24f;
            float r5 = 24f;
            float tableH = r0 + r1 + r2 + r3 + r4 + r5;

            DrawRect(tableX, tableTop, tableW, tableH, 0.85f);
            DrawLine(tableX + col1, tableTop, tableX + col1, tableTop + tableH, 0.85f);

            float y = tableTop;
            y += r0; DrawLine(tableX, y, tableX + tableW, y, 0.85f);
            y += r1; DrawLine(tableX, y, tableX + tableW, y, 0.85f);
            y += r2; DrawLine(tableX, y, tableX + tableW, y, 0.85f);
            y += r3; DrawLine(tableX, y, tableX + tableW, y, 0.85f);
            y += r4; DrawLine(tableX, y, tableX + tableW, y, 0.85f);

            float labelSize = tableSize;
            string numeroControl = (req.NumeroControl ?? "").Trim();

            DrawText("Número de control:", tableX + 6f, tableTop + 15.5f, font, labelSize);
            DrawText("Nombre del residente:", tableX + 6f, tableTop + r0 + 16f, font, labelSize);
            DrawText("Carrera:", tableX + 6f, tableTop + r0 + r1 + 15.5f, font, labelSize);
            DrawText("Nombre del proyecto:", tableX + 6f, tableTop + r0 + r1 + r2 + 16f, font, labelSize);
            DrawText("Período de realización:", tableX + 6f, tableTop + r0 + r1 + r2 + r3 + 15.5f, font, labelSize);
            DrawText("Empresa:", tableX + 6f, tableTop + r0 + r1 + r2 + r3 + r4 + 15.5f, font, labelSize);

            float valX = tableX + col1;
            float valW = col2;
            float cellFont = tableSize;

            DrawWrappedInBox(numeroControl, valX, tableTop, valW, r0, font, cellFont);
            DrawWrappedInBox(residenteTexto, valX, tableTop + r0, valW, r1, font, cellFont);
            DrawWrappedInBox(carrera, valX, tableTop + r0 + r1, valW, r2, font, cellFont);
            DrawWrappedInBox(proyecto, valX, tableTop + r0 + r1 + r2, valW, r3, font, cellFont);
            DrawWrappedInBox(periodo, valX, tableTop + r0 + r1 + r2 + r3, valW, r4, font, cellFont);
            DrawWrappedInBox(empresa, valX, tableTop + r0 + r1 + r2 + r3 + r4, valW, r5, font, cellFont);

            // =========================
            // Párrafos debajo de la tabla
            // =========================
            float afterTableY = tableTop + tableH + 18f;


            string p2 =
                "Así mismo, le solicito dar el seguimiento pertinente a la realización del proyecto aplicando los lineamientos " +
                "establecidos para ello, en el Procedimiento para Realizar y Acreditar la Residencia Profesional.";
            DrawJustifiedParagraph(p2, left, afterTableY, bodyW, font, bodySize, bodyLeading, 3);


            float p3Y = afterTableY + (3 * bodyLeading) + 6f;

            string p3 =
                "Agradezco de antemano su valioso apoyo en esta importante actividad para la formación profesional de nuestro estudiantado.";
            DrawJustifiedParagraph(p3, left, p3Y, bodyW, font, bodySize, bodyLeading, 2);


            float p3End = p3Y + (2 * bodyLeading);

            // =========================
            // Atentamente + espacio para sello/firma
            // =========================
            float maxAllowed = rect.GetHeight() - 88f;

            float atentamenteY = Math.Max(p3End + 26f, 575f);
            float lema1Y = atentamenteY + 16f;
            float lema2Y = atentamenteY + 29f;

            float firmaNombreY = atentamenteY + 96f;
            float firmaCargoY = firmaNombreY + 15f;
            float ccpY = firmaCargoY + 28f;

            float lastLineY = ccpY + 12f;
            float overflow = lastLineY - maxAllowed;
            if (overflow > 0)
            {
                atentamenteY -= overflow;
                lema1Y -= overflow;
                lema2Y -= overflow;
                firmaNombreY -= overflow;
                firmaCargoY -= overflow;
                ccpY -= overflow;
            }

            string firmaCargoUnaLinea = string.Join(" ",
                new[] { req.FirmaCargoLinea1, req.FirmaCargoLinea2 }
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()));

            DrawTextCentered("A T E N T A M E N T E", centerX, atentamenteY, fontBold, signSize);
            DrawTextCentered("Excelencia en Educación Tecnológica®", centerX, lema1Y, font, smallSize);
            DrawTextCentered("“Tecnología Propia e Independencia Económica”", centerX, lema2Y, font, smallSize);

            DrawTextCenteredFit(req.FirmaNombre ?? "", centerX, firmaNombreY, bodyW - 10f, fontBold, signSize, 8.6f);
            DrawTextCenteredFit(firmaCargoUnaLinea, centerX, firmaCargoY, bodyW - 10f, fontBold, 9.8f, 8.2f);

            DrawText("ccp. Expediente", left, ccpY, font, smallSize);
            DrawText("MMH/mmvh", left, ccpY + 12f, font, smallSize);

            pdf.Close();
            return output.ToArray();
        }
        public byte[] BuildOficioAsignacionRevisorReportePreliminar(byte[] templatePdf, OficioAsignacionRevisorReportePreliminarRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            using var output = new MemoryStream();
            using var writer = new PdfWriter(output);
            using var pdf = new PdfDocument(writer);

            PdfPage page;

            if (templatePdf != null && templatePdf.Length > 0)
            {
                using var templateStream = new MemoryStream(templatePdf);
                using var reader = new PdfReader(templateStream);
                using var src = new PdfDocument(reader);

                page = src.GetFirstPage().CopyTo(pdf);
                pdf.AddPage(page);
            }
            else
            {
                page = pdf.AddNewPage(PageSize.LETTER);
            }

            var rect = page.GetPageSizeWithRotation();
            var canvas = new PdfCanvas(page);

            // ── Compensar flip vertical del membrete escaneado ───────────────
            {
                var pageRotation = page.GetRotation();
                bool hasYFlip = false;

                if (pageRotation == 0)
                {
                    try
                    {
                        var pageDict = page.GetPdfObject();
                        var contentsObj = pageDict.Get(iText.Kernel.Pdf.PdfName.Contents);
                        iText.Kernel.Pdf.PdfStream? firstStream = null;

                        if (contentsObj is iText.Kernel.Pdf.PdfArray arr && arr.Size() > 0)
                            firstStream = arr.GetAsStream(0);
                        else if (contentsObj is iText.Kernel.Pdf.PdfStream s)
                            firstStream = s;

                        if (firstStream != null)
                        {
                            var bytes = firstStream.GetBytes();
                            var preview = System.Text.Encoding.Latin1.GetString(bytes, 0,
                                Math.Min(bytes.Length, 80));
                            hasYFlip = System.Text.RegularExpressions.Regex.IsMatch(
                                preview, @"1\s+0\s+0\s+-1\s+0\s+[\d.]+\s+cm");
                        }
                    }
                    catch { /* si falla la detección, no aplicamos nada */ }
                }

                float w = rect.GetWidth();
                float h = rect.GetHeight();

                if (hasYFlip)
                    canvas.ConcatMatrix(1, 0, 0, -1, 0, h);
                else if (pageRotation == 90)
                    canvas.ConcatMatrix(0, 1, -1, 0, h, 0);
                else if (pageRotation == 180)
                    canvas.ConcatMatrix(-1, 0, 0, -1, w, h);
                else if (pageRotation == 270)
                    canvas.ConcatMatrix(0, -1, 1, 0, 0, w);
            }
            // ────────────────────────────────────────────────────────────────

            PdfFont font = LoadFontFromCandidates(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Regular.ttf"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "NotoSans-Regular.ttf")
            );

            PdfFont fontBold = LoadFontFromCandidates(
                System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Bold.ttf"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "NotoSans-Bold.ttf")
            );

            var esMx = CultureInfo.GetCultureInfo("es-MX");

            float Y(float yFromTop) => rect.GetHeight() - yFromTop;

            void DrawText(string text, float x, float yFromTopBaseline, PdfFont f, float size)
            {
                canvas.BeginText();
                canvas.SetFontAndSize(f, size);
                canvas.MoveText(x, Y(yFromTopBaseline));
                canvas.ShowText(text ?? "");
                canvas.EndText();
            }

            void DrawTextRight(string text, float rightX, float yFromTopBaseline, PdfFont f, float size)
            {
                text ??= "";
                float textWidth = f.GetWidth(text, size);
                DrawText(text, rightX - textWidth, yFromTopBaseline, f, size);
            }

            void DrawMixedRight(
                string leftText, PdfFont leftFont, float leftSize,
                string rightText, PdfFont rightFont, float rightSize,
                float rightX, float yFromTopBaseline, float gap = 3f)
            {
                leftText ??= "";
                rightText ??= "";

                float leftWidth = leftFont.GetWidth(leftText, leftSize);
                float rightWidth = rightFont.GetWidth(rightText, rightSize);

                float startX = rightX - (leftWidth + gap + rightWidth);

                DrawText(leftText, startX, yFromTopBaseline, leftFont, leftSize);
                DrawText(rightText, startX + leftWidth + gap, yFromTopBaseline, rightFont, rightSize);
            }

            void DrawLine(float x1, float yFromTop, float x2, float y2FromTop, float width = 0.85f)
            {
                canvas.SaveState();
                canvas.SetLineWidth(width);
                canvas.MoveTo(x1, Y(yFromTop));
                canvas.LineTo(x2, Y(y2FromTop));
                canvas.Stroke();
                canvas.RestoreState();
            }

            void DrawRect(float x, float yFromTopTop, float w, float h, float lineWidth = 0.85f)
            {
                canvas.SaveState();
                canvas.SetLineWidth(lineWidth);
                canvas.Rectangle(x, Y(yFromTopTop + h), w, h);
                canvas.Stroke();
                canvas.RestoreState();
            }

            void DrawCentered(string text, (float x0, float x1) col, float yFromTopBaseline, PdfFont f, float size)
            {
                text ??= "";
                float w = col.x1 - col.x0;
                float tw = f.GetWidth(text, size);
                float x = col.x0 + (w - tw) / 2f;
                DrawText(text, x, yFromTopBaseline, f, size);
            }

            // ── Párrafo con segmentos en negritas usando marcadores **texto** ──
            List<(string word, bool bold)> TokenizeBold(string markedText)
            {
                markedText ??= "";
                var parts = markedText.Split(new[] { "**" }, StringSplitOptions.None);
                var words = new List<(string, bool)>();
                for (int i = 0; i < parts.Length; i++)
                {
                    bool bold = (i % 2 == 1);
                    foreach (var w in parts[i].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        words.Add((w, bold));
                }
                return words;
            }

            // Dibuja el párrafo envuelto (izquierda) y regresa el Y (desde arriba) donde terminó
            float DrawMixedParagraph(string markedText, float x, float topBaseline, float maxWidth, float size, float leading)
            {
                var words = TokenizeBold(markedText);
                float spaceWidth = font.GetWidth(" ", size);

                var lines = new List<List<(string word, bool bold)>>();
                var currentLine = new List<(string word, bool bold)>();
                float currentWidth = 0f;

                foreach (var (word, bold) in words)
                {
                    var f = bold ? fontBold : font;
                    float wWidth = f.GetWidth(word, size);
                    float testWidth = currentWidth + (currentLine.Count > 0 ? spaceWidth : 0) + wWidth;

                    if (testWidth > maxWidth && currentLine.Count > 0)
                    {
                        lines.Add(currentLine);
                        currentLine = new List<(string, bool)> { (word, bold) };
                        currentWidth = wWidth;
                    }
                    else
                    {
                        currentLine.Add((word, bold));
                        currentWidth = testWidth;
                    }
                }
                if (currentLine.Count > 0) lines.Add(currentLine);

                for (int i = 0; i < lines.Count; i++)
                {
                    float cursorX = x;
                    float yBase = topBaseline + (i * leading);
                    foreach (var (word, bold) in lines[i])
                    {
                        var f = bold ? fontBold : font;
                        DrawText(word, cursorX, yBase, f, size);
                        cursorX += f.GetWidth(word, size) + spaceWidth;
                    }
                }

                return topBaseline + (Math.Max(1, lines.Count) - 1) * leading;
            }

            List<string> WrapLines(string text, PdfFont f, float size, float maxWidth)
            {
                text ??= "";
                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lines = new List<string>();
                string line = "";

                foreach (var w in words)
                {
                    var test = string.IsNullOrEmpty(line) ? w : $"{line} {w}";
                    if (f.GetWidth(test, size) > maxWidth && !string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                        line = w;
                    }
                    else
                    {
                        line = test;
                    }
                }

                if (!string.IsNullOrEmpty(line))
                    lines.Add(line);

                return lines;
            }

            string TruncateWithEllipsis(string text, PdfFont f, float size, float maxWidth)
            {
                text ??= "";
                const string ell = "…";
                if (f.GetWidth(text, size) <= maxWidth) return text;

                var t = text;
                while (t.Length > 0 && f.GetWidth(t + ell, size) > maxWidth)
                    t = t.Substring(0, t.Length - 1);

                return t.Length == 0 ? "" : (t + ell);
            }

            List<string> WrapLinesMax(string text, PdfFont f, float size, float maxWidth, int maxLines)
            {
                var lines = WrapLines(text, f, size, maxWidth);
                if (lines.Count <= maxLines) return lines;

                string last = lines[maxLines - 1];
                for (int i = maxLines; i < lines.Count; i++)
                    last += " " + lines[i];

                var result = lines.Take(maxLines - 1).ToList();
                result.Add(TruncateWithEllipsis(last, f, size, maxWidth));
                return result;
            }

            // -------------------------
            // Datos normalizados
            // -------------------------
            string ciudad = string.IsNullOrWhiteSpace(req.Ciudad) ? "Oaxaca de Juárez, Oaxaca" : req.Ciudad.Trim();
            string oficio = string.IsNullOrWhiteSpace(req.Oficio) ? "__________" : req.Oficio.Trim();
            string asunto = string.IsNullOrWhiteSpace(req.Asunto) ? "Asignación de revisor de reporte preliminar" : req.Asunto.Trim();
            string numeral = string.IsNullOrWhiteSpace(req.NumeralLineamiento) ? "12.4.1.7" : req.NumeralLineamiento.Trim();
            string proyecto = (req.NombreProyecto ?? "").Trim().ToUpperInvariant();
            string destinatario = (req.DestinatarioNombre ?? "").Trim();
            string cargoDestinatario = string.IsNullOrWhiteSpace(req.DestinatarioCargoLinea1)
                ? "CATEDRATICO(A) DEL I.T. DE OAXACA"
                : req.DestinatarioCargoLinea1.Trim();
            var estudiantes = req.Estudiantes ?? new List<RevisorReportePreliminarEstudianteItem>();

            float left = 72f;
            float right = rect.GetWidth() - 72f;
            float bodyW = right - left;

            // =========================
            // Encabezado derecho
            // =========================
            string fechaSolo = req.Fecha.ToString("dd 'de' MMMM 'de' yyyy", esMx).ToLower(esMx);
            string ciudadPrefix = $"{ciudad} ";

            float fechaY = 140f;
            float oficioY = 154f;
            float asuntoY = 168f;

            float ciudadWidth = font.GetWidth(ciudadPrefix, headerSize);
            float fechaWidth = font.GetWidth(fechaSolo, headerSize);
            float fechaStartX = right - (ciudadWidth + fechaWidth);

            DrawText(ciudadPrefix, fechaStartX, fechaY, font, headerSize);
            DrawText(fechaSolo, fechaStartX + ciudadWidth, fechaY, font, headerSize);

            DrawTextRight($"OFICIO No. {oficio}", right, oficioY, fontBold, oficioSize);

            DrawMixedRight(
                "ASUNTO:",
                font,
                asuntoLabelSize,
                asunto,
                fontBold,
                asuntoValueSize,
                right,
                asuntoY,
                4f
            );

            // =========================
            // Destinatario
            // =========================
            float destY = 214f;
            float cargoY = 231f;
            float presenteY = 248f;

            DrawText(destinatario, left, destY, fontBold, destinatarioSize);
            DrawText(cargoDestinatario, left, cargoY, fontBold, cargoSize);
            DrawText("P R E S E N T E", left, presenteY, fontBold, cargoSize);

            // =========================
            // Párrafo 1: fundamento legal
            // =========================
            string p1 =
                $"Con fundamento en el **Lineamiento para la Operación de la Residencia Profesional**, específicamente " +
                $"atendiendo al **numeral {numeral}**, el cual establece que todo proyecto debe ser autorizado por la " +
                $"Jefatura del Departamento Académico previo análisis de la Academia, se le comunica lo siguiente:";

            float p1Top = 280f;
            float bodySizeLocal = bodySize;
            float leadingLocal = bodyLeading + 1f;

            float p1End = DrawMixedParagraph(p1, left, p1Top, bodyW, bodySizeLocal, leadingLocal);

            // =========================
            // Párrafo 2: designación como revisor + proyecto
            // =========================
            string p2 =
                $"Se le ha designado como **Revisor(a)** para evaluar el reporte preliminar del proyecto para " +
                $"Residencia Profesional: **{proyecto}**, el cual presentan los estudiantes:";

            float p2Top = p1End + leadingLocal + 10f;
            float p2End = DrawMixedParagraph(p2, left, p2Top, bodyW, bodySizeLocal, leadingLocal);

            // =========================
            // Tabla: Número de control | Estudiante
            // =========================
            float tableTop = p2End + leadingLocal + 16f;
            float tableLeft = left;
            float tableWidth = bodyW;
            float wCol1 = 150f;
            float wCol2 = tableWidth - wCol1;

            (float x0, float x1) c1 = (tableLeft, tableLeft + wCol1);
            (float x0, float x1) c2 = (c1.x1, tableLeft + tableWidth);

            float headerH = 20f;
            float rowH = 20f;
            float tableH = headerH + (rowH * Math.Max(1, estudiantes.Count));

            DrawRect(tableLeft, tableTop, tableWidth, tableH, 0.85f);
            DrawLine(tableLeft, tableTop + headerH, tableLeft + tableWidth, tableTop + headerH, 0.85f);
            DrawLine(c1.x1, tableTop, c1.x1, tableTop + tableH, 0.85f);

            float headerBase = tableTop + 14f;
            DrawCentered("Número de control", c1, headerBase, fontBold, tableSize);
            DrawCentered("Estudiante", c2, headerBase, fontBold, tableSize);

            for (int i = 0; i < estudiantes.Count; i++)
            {
                float rowTop = tableTop + headerH + (i * rowH);
                if (i > 0)
                    DrawLine(tableLeft, rowTop, tableLeft + tableWidth, rowTop, 0.85f);

                float rowBase = rowTop + 14f;
                DrawCentered(estudiantes[i].NumeroControl ?? "", c1, rowBase, font, tableSize);
                DrawText(estudiantes[i].NombreEstudiante ?? "", c2.x0 + 8f, rowBase, font, tableSize);
            }

            // =========================
            // Párrafos de cierre
            // =========================
            float afterTableY = tableTop + tableH + 20f;

            string p3 =
                "Su análisis técnico permitirá a la Academia determinar la viabilidad y pertinencia del proyecto para su " +
                "posterior autorización por esta Jefatura.";
            var p3Lines = WrapLinesMax(p3, font, bodySizeLocal, bodyW, 3);
            for (int i = 0; i < p3Lines.Count; i++)
                DrawText(p3Lines[i], left, afterTableY + (i * leadingLocal), font, bodySizeLocal);

            float p4Top = afterTableY + (p3Lines.Count * leadingLocal) + 10f;
            string p4 =
                "En caso de autorizar el proyecto, deberá entregar a la oficina de Vinculación el **Anexo II**, por cada " +
                "estudiante asignado.";
            float p4End = DrawMixedParagraph(p4, left, p4Top, bodyW, bodySizeLocal, leadingLocal);

            float p5Top = p4End + leadingLocal + 10f;
            string p5 = "Agradezco de antemano su valiosa colaboración en este proceso.";
            var p5Lines = WrapLinesMax(p5, font, bodySizeLocal, bodyW, 2);
            for (int i = 0; i < p5Lines.Count; i++)
                DrawText(p5Lines[i], left, p5Top + (i * leadingLocal), font, bodySizeLocal);

            // =========================
            // Firma
            // =========================
            float maxAllowed = rect.GetHeight() - 88f;
            float atentamenteY = Math.Max(p5Top + (p5Lines.Count * leadingLocal) + 40f, 620f);
            float firmaNombreY = atentamenteY + 70f;
            float firmaCargoY = firmaNombreY + 15f;
            float ccpY = firmaCargoY + 28f;

            float lastLineY = ccpY + 12f;
            float overflow = lastLineY - maxAllowed;
            if (overflow > 0)
            {
                atentamenteY -= overflow;
                firmaNombreY -= overflow;
                firmaCargoY -= overflow;
                ccpY -= overflow;
            }

            DrawText("Atentamente", left, atentamenteY, fontBold, signSize);
            DrawText((req.FirmaNombre ?? "").Trim(), left, firmaNombreY, fontBold, signSize);
            DrawText((req.FirmaCargoLinea1 ?? "").Trim(), left, firmaCargoY, fontBold, 9.8f);

            DrawText("ccp. Expediente", left, ccpY, font, smallSize);
            DrawText("ICMO/mmvh", left, ccpY + 12f, font, smallSize);

            pdf.Close();
            return output.ToArray();
        }

        public byte[] BuildOficiosAsignacionRevisoresFormatoFoto(byte[] templatePdf, OficiosAsignacionRevisoresRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Revisores == null || req.Revisores.Count == 0)
                throw new ArgumentException("No hay revisores en la solicitud.", nameof(req.Revisores));

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var outPdf = new PdfDocument(writer);

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var esMx = new CultureInfo("es-MX");

            // ========= helpers para medir texto =========
            List<string> WrapLines(string text, PdfFont f, float size, float maxWidth)
            {
                text ??= "";
                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lines = new List<string>();
                string line = "";

                foreach (var w in words)
                {
                    var test = string.IsNullOrEmpty(line) ? w : $"{line} {w}";
                    if (f.GetWidth(test, size) > maxWidth && !string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                        line = w;
                    }
                    else line = test;
                }

                if (!string.IsNullOrEmpty(line)) lines.Add(line);
                return lines;
            }

            float MeasureRowHeight(OficioRevisorRow r, float fontSize, float leading, float pad,
                                   float wNo, float wEst, float wProy, float wAse)
            {
                // líneas por celda
                int lNo = Math.Max(1, WrapLines(r?.NoControl ?? "—", font, fontSize, wNo - (pad * 2)).Count);
                int lEst = Math.Max(1, WrapLines(r?.Estudiante ?? "—", font, fontSize, wEst - (pad * 2)).Count);
                int lPro = Math.Max(1, WrapLines(r?.Proyecto ?? "—", font, fontSize, wProy - (pad * 2)).Count);
                int lAs = Math.Max(1, WrapLines(r?.Asesor ?? "—", font, fontSize, wAse - (pad * 2)).Count);

                int maxLines = Math.Max(Math.Max(lNo, lEst), Math.Max(lPro, lAs));
                float h = (maxLines * leading) + (pad * 2);

                // altura mínima “tipo foto”
                return Math.Max(26f, h);
            }

            // ========= layout base (ajustable) =========
            const float left = 72f;
            const float rightMargin = 72f;

            const float headerY = 150f;     // fecha
            const float oficioY = 172f;     // oficio
            const float asuntoY = 198f;

            const float destinatarioTop = 250f;
            const float introTop = 320f;

            const float tableTop = 400f;    // desde arriba
            const float footerSafe = 70f;   // para no invadir pie/imagenes del template

            // firma / cierre SOLO en última página
            const float cierreGap = 18f;
            const float atentamenteY = 610f;
            const float firmaNombreY = 650f;
            const float firmaCargoY = 665f;
            const float ccpY = 720f;

            // columnas “como foto”
            float[] colW = { 90f, 140f, 220f, 90f };
            float pad = 4f;
            float fontSize = 9.8f;
            float leading = 12f;
            float headerH = 22f;

            // ancho total usable
            float pageW = PageSize.LETTER.GetWidth();
            float pageH = PageSize.LETTER.GetHeight();
            float right = pageW - rightMargin;
            float width = right - left;

            // Asegurar que la tabla “quepa” con tu width real
            // Si width difiere, reescala columnas proporcionalmente
            float sumCol = colW.Sum();
            float scale = (width / sumCol);
            colW = colW.Select(w => w * scale).ToArray();

            float wNo = colW[0], wEst = colW[1], wProy = colW[2], wAse = colW[3];

            foreach (var rev in req.Revisores)
            {
                var allRows = (rev.Rows ?? new List<OficioRevisorRow>()).ToList();
                if (allRows.Count == 0)
                {
                    allRows.Add(new OficioRevisorRow { NoControl = "—", Estudiante = "—", Proyecto = "—", Asesor = "—" });
                }

                int idx = 0;
                bool firstPage = true;

                while (idx < allRows.Count)
                {
                    // 1) Crear página con template (siempre)
                    PdfPage page;
                    if (templatePdf != null && templatePdf.Length > 0)
                    {
                        using var src = new PdfDocument(new PdfReader(new MemoryStream(templatePdf)));
                        src.CopyPagesTo(1, 1, outPdf);
                        page = outPdf.GetLastPage();
                    }
                    else
                    {
                        page = outPdf.AddNewPage(PageSize.LETTER);
                    }

                    var ps = page.GetPageSize();
                    pageW = ps.GetWidth();
                    pageH = ps.GetHeight();
                    right = pageW - rightMargin;
                    width = right - left;

                    float Y(float yFromTop) => pageH - yFromTop;

                    var pdfCanvas = new PdfCanvas(page);

                    // ✅ FIX: esta era la única variante de Build* sin compensación de
                    // flip vertical. El membrete escaneado trae "1 0 0 -1 0 h cm" en su
                    // propio content stream (se auto-corrige al verse), pero el texto que
                    // dibujamos encima NO tenía esa misma compensación, así que quedaba
                    // al revés respecto al membrete. Se replica la misma detección que
                    // usan BuildOficiosAsignacionRevisores / BuildOficioAsignacionAsesorInterno / etc.
                    {
                        var pageRotation = page.GetRotation();
                        bool hasYFlip = false;

                        if (pageRotation == 0)
                        {
                            try
                            {
                                var pageDict = page.GetPdfObject();
                                var contentsObj = pageDict.Get(iText.Kernel.Pdf.PdfName.Contents);
                                iText.Kernel.Pdf.PdfStream? firstStream = null;

                                if (contentsObj is iText.Kernel.Pdf.PdfArray arr && arr.Size() > 0)
                                    firstStream = arr.GetAsStream(0);
                                else if (contentsObj is iText.Kernel.Pdf.PdfStream s)
                                    firstStream = s;

                                if (firstStream != null)
                                {
                                    var bytes = firstStream.GetBytes();
                                    var preview = System.Text.Encoding.Latin1.GetString(bytes, 0,
                                        Math.Min(bytes.Length, 80));
                                    hasYFlip = System.Text.RegularExpressions.Regex.IsMatch(
                                        preview, @"1\s+0\s+0\s+-1\s+0\s+[\d.]+\s+cm");
                                }
                            }
                            catch { /* si falla la detección, no aplicamos nada */ }
                        }

                        if (hasYFlip)
                            pdfCanvas.ConcatMatrix(1, 0, 0, -1, 0, pageH);
                        else if (pageRotation == 90)
                            pdfCanvas.ConcatMatrix(0, 1, -1, 0, pageH, 0);
                        else if (pageRotation == 180)
                            pdfCanvas.ConcatMatrix(-1, 0, 0, -1, pageW, pageH);
                        else if (pageRotation == 270)
                            pdfCanvas.ConcatMatrix(0, -1, 1, 0, 0, pageW);
                    }

                    var canvas = new iText.Layout.Canvas(pdfCanvas, ps);

                    void AbsText(string text, float x, float yFromTop, PdfFont f, float size, TextAlignment align)
                    {
                        var p = new Paragraph(text ?? "")
                            .SetFont(f)
                            .SetFontSize(size)
                            .SetMargin(0)
                            .SetPadding(0);
                        canvas.ShowTextAligned(p, x, Y(yFromTop), align);
                    }

                    // ========== Encabezado (en TODAS las páginas) ==========
                    string fechaTxt = $"{req.Ciudad} {req.Fecha.ToString("dd'/'MMMM'/'yyyy", esMx)}";
                    AbsText(fechaTxt, right, headerY, font, 10.5f, TextAlignment.RIGHT);
                    AbsText($"OFICIO No. {req.Oficio}", right, oficioY, fontBold, 10.5f, TextAlignment.RIGHT);

                    AbsText("ASUNTO:", right - 180f, asuntoY, fontBold, 10.5f, TextAlignment.LEFT);
                    AbsText(req.Asunto ?? "Revisor de Residencia Profesional", right - 125f, asuntoY, font, 10.5f, TextAlignment.LEFT);

                    // ========== Destinatario (en TODAS) ==========
                    float y = destinatarioTop;
                    AbsText((rev.RevisorNombre ?? "").ToUpperInvariant(), left, y, fontBold, 10.8f, TextAlignment.LEFT);
                    y += 16f;
                    AbsText((rev.RevisorCargoLinea1 ?? "").ToUpperInvariant(), left, y, fontBold, 10.2f, TextAlignment.LEFT);
                    y += 18f;
                    AbsText("P R E S E N T E", left, y, fontBold, 10.8f, TextAlignment.LEFT);

                    // ========== Intro (solo en primera página del revisor) ==========
                    if (firstPage)
                    {
                        var intro = new Paragraph(
                            "Por este conducto informo a usted que ha sido asignado para fungir como " +
                            "Revisor de los Proyectos de Residencia Profesional que a continuación se describen:")
                            .SetFont(font)
                            .SetFontSize(10.5f)
                            .SetFixedLeading(14f)
                            .SetMargin(0);

                        float introBoxH = 55f;
                        intro.SetFixedPosition(left, Y(introTop) - introBoxH, width);
                        canvas.Add(intro);
                    }

                    // ========== Definir cuánto espacio tiene la tabla en ESTA hoja ==========
                    // Si es la ÚLTIMA hoja (o potencialmente última), reservamos cierre+firma.
                    // Si no, dejamos la tabla crecer más (pero sin invadir el footer).
                    bool isLastPageCandidate;

                    // Heurística: intentamos meter el resto; si NO cabe con espacio "final", partimos.
                    // Espacio final:
                    float maxTableEndTopFinal = atentamenteY - 18f; // antes de "ATENTAMENTE"
                    float maxTableHeightFinal = maxTableEndTopFinal - tableTop;

                    // Espacio intermedio:
                    float maxTableEndTopMid = pageH - footerSafe;   // hasta antes del pie
                    float maxTableHeightMid = maxTableEndTopMid - tableTop;

                    // Primero probamos si todo lo restante cabe en "final"
                    float neededHFinal = headerH;
                    for (int j = idx; j < allRows.Count; j++)
                    {
                        neededHFinal += MeasureRowHeight(allRows[j], fontSize, leading, pad, wNo, wEst, wProy, wAse);
                        if (neededHFinal > maxTableHeightFinal) break;
                    }
                    isLastPageCandidate = (neededHFinal <= maxTableHeightFinal);

                    float maxTableHeight = isLastPageCandidate ? maxTableHeightFinal : maxTableHeightMid;

                    // ========== Seleccionar filas que caben ==========
                    float used = headerH;
                    var takeRows = new List<OficioRevisorRow>();

                    while (idx < allRows.Count)
                    {
                        float rh = MeasureRowHeight(allRows[idx], fontSize, leading, pad, wNo, wEst, wProy, wAse);
                        if (used + rh > maxTableHeight)
                            break;

                        takeRows.Add(allRows[idx]);
                        used += rh;
                        idx++;
                    }

                    // (seguro) si una fila gigantesca no cabe, forzamos 1
                    if (takeRows.Count == 0 && idx < allRows.Count)
                    {
                        takeRows.Add(allRows[idx]);
                        idx++;
                    }

                    // ========== Construir tabla (fixed) con altura exacta ==========
                    float tableH = used;
                    float tableBottom = Y(tableTop) - tableH;

                    var table = new Table(UnitValue.CreatePointArray(colW))
                        .SetWidth(UnitValue.CreatePointValue(width))
                        .SetFixedPosition(left, tableBottom, width)
                        .SetFont(font)
                        .SetFontSize(fontSize);

                    Cell MakeCell(string txt, bool bold = false, bool header = false, float? height = null)
                    {
                        var para = new Paragraph(txt ?? "")
                            .SetFont(bold ? fontBold : font)
                            .SetFontSize(fontSize)
                            .SetFixedLeading(leading)
                            .SetMargin(0);

                        var c = new Cell().Add(para)
                            .SetPadding(pad)
                            .SetBorder(new SolidBorder(ColorConstants.BLACK, 1))
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE);

                        if (header) c.SetBackgroundColor(new DeviceGray(0.90f));
                        if (height.HasValue) c.SetHeight(height.Value);

                        return c;
                    }

                    // header
                    table.AddHeaderCell(MakeCell("N° CONTROL:", bold: true, header: true, height: headerH));
                    table.AddHeaderCell(MakeCell("ESTUDIANTE", bold: true, header: true, height: headerH));
                    table.AddHeaderCell(MakeCell("PROYECTO", bold: true, header: true, height: headerH));
                    table.AddHeaderCell(MakeCell("ASESOR", bold: true, header: true, height: headerH));

                    // rows
                    foreach (var r in takeRows)
                    {
                        float rh = MeasureRowHeight(r, fontSize, leading, pad, wNo, wEst, wProy, wAse);

                        table.AddCell(MakeCell(r.NoControl, height: rh));
                        table.AddCell(MakeCell(r.Estudiante, height: rh));
                        table.AddCell(MakeCell(r.Proyecto, height: rh));
                        table.AddCell(MakeCell(r.Asesor, height: rh));
                    }

                    canvas.Add(table);

                    // ========== Cierre + firma SOLO si ya se acabaron filas ==========
                    bool lastPage = (idx >= allRows.Count);
                    if (lastPage)
                    {
                        float cierreTop = tableTop + tableH + cierreGap;

                        var cierre = new Paragraph(
                            "Así mismo, le solicito dar el seguimiento pertinente a la realización del proyecto aplicando los lineamientos\n" +
                            "establecidos para ello, en el procedimiento para Residencia Profesional.\n\n" +
                            "Agradezco de antemano su valioso apoyo en esta importante actividad para la formación profesional de nuestros\n" +
                            "estudiantes.")
                            .SetFont(font)
                            .SetFontSize(10.5f)
                            .SetFixedLeading(14f)
                            .SetMargin(0);

                        float cierreBoxH = 120f;
                        cierre.SetFixedPosition(left, Y(cierreTop) - cierreBoxH, width);
                        canvas.Add(cierre);

                        AbsText("A T E N T A M E N T E", left, atentamenteY, fontBold, 10.5f, TextAlignment.LEFT);
                        AbsText(req.FirmaNombre ?? "", left, firmaNombreY, fontBold, 10.5f, TextAlignment.LEFT);
                        AbsText(req.FirmaCargoLinea1 ?? "", left, firmaCargoY, fontBold, 10.0f, TextAlignment.LEFT);

                        AbsText("ccp. Expediente", left, ccpY, font, 9.5f, TextAlignment.LEFT);
                        AbsText("MMH/mmvh", left, ccpY + 14f, font, 9.5f, TextAlignment.LEFT);
                    }

                    canvas.Close();
                    firstPage = false;
                }
            }

            outPdf.Close();
            return ms.ToArray();
        }




        private static void DrawText(PdfCanvas canvas, PdfFont font, float size, float x, float y, string text, TextAlignment align)
        {
            canvas.BeginText();
            canvas.SetFontAndSize(font, size);
            canvas.MoveText(x, y);
            // iText Canvas simple no alinea; para alineación real, haríamos showTextAligned en layout,
            // pero aquí lo resolvemos rápido:
            canvas.ShowText(text);
            canvas.EndText();
        }



        private static PdfFont LoadFontFromCandidates(params string[] candidates)
        {
            var path = candidates.FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(path))
                throw new FileNotFoundException(
                    "No se encontró la fuente Noto Sans. Coloca los archivos .ttf en una ruta conocida del proyecto.");

            return PdfFontFactory.CreateFont(
                path,
                PdfEncodings.IDENTITY_H,
                PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
        }
    }
}

