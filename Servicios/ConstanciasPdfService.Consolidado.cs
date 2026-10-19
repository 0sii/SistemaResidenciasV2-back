// ═══════════════════════════════════════════════════════════════════════
// ConstanciasPdfService.Consolidado.cs
//
// INSTRUCCIONES DE INTEGRACIÓN (léelas antes de pegar el archivo):
//
// 1) En tu archivo ORIGINAL "ConstanciaPdfService.cs":
//      - Cambia:   public interface IConstanciasPdfService
//        por:      public partial interface IConstanciasPdfService
//
//      - Cambia:   public class ConstanciasPdfService : IConstanciasPdfService
//        por:      public partial class ConstanciasPdfService : IConstanciasPdfService
//
// 2) Copia este archivo tal cual a la carpeta de tu proyecto (junto al
//    original). No necesitas tocar nada más: los métodos privados que
//    reutilizamos (WrapLines, medidas, flip de membrete, etc.) se
//    redeclaran aquí de forma local para no depender de los privados del
//    otro archivo (evita choques de "partial" con helpers locales).
//
// 3) Estos métodos NO reemplazan a los que ya tienes; los complementan:
//      - BuildOficioAsignacionAsesorInternoConsolidado(...)
//      - BuildOficioAsignacionRevisorReportePreliminarConsolidado(...)
//      - BuildConstanciasAceptacionReportePreliminarConsolidado(...)
//
//    El de "Revisor de Residencia" NO necesita método nuevo: tu
//    BuildOficiosAsignacionRevisoresFormatoFoto(...) YA soporta varias
//    filas (proyectos) agrupadas para un mismo revisor. Solo hay que
//    alimentarlo con TODOS los proyectos del docente (ver
//    ProyectosController.Oficios.cs).
// ═══════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfReader = iText.Kernel.Pdf.PdfReader;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;

// ─────────────────────────────────────────────────────────────────────────
// DTOs para los oficios consolidados (un docente, varios proyectos)
// ─────────────────────────────────────────────────────────────────────────

/// <summary>Un proyecto dentro del oficio consolidado de Asesor Interno.</summary>
public sealed class ProyectoAsesorInternoItem
{
    public string NumeroControl { get; set; } = "";      // si hay varios residentes, se puede dejar vacío
    public List<string> NumerosControl { get; set; } = new(); // uno por residente, en el mismo orden que Residentes
    public string NombreProyecto { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Carrera { get; set; } = "";
    public string PeriodoRealizacion { get; set; } = "";
    public List<string> Residentes { get; set; } = new();
}
public sealed class OficioAsignacionAsesorInternoConsolidadoRequest
{
    public string Ciudad { get; set; } = "Oaxaca de Juárez, Oaxaca";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Oficio { get; set; } = "JV-XXX/2025";

    public string DestinatarioNombre { get; set; } = "";
    public string DestinatarioCargoLinea1 { get; set; } = "CATEDRATICO(A) DEL I.T. DE OAXACA";

    public List<ProyectoAsesorInternoItem> Proyectos { get; set; } = new();

    public string FirmaNombre { get; set; } = "";
    public string FirmaCargoLinea1 { get; set; } = "JEFA(E) DEL DEPARTAMENTO";
    public string FirmaCargoLinea2 { get; set; } = "DE SISTEMAS Y COMPUTACIÓN";
}

/// <summary>Un proyecto dentro del oficio consolidado de Revisor de Reporte Preliminar.</summary>
public sealed class ProyectoRevisorReportePreliminarItem
{
    public string NombreProyecto { get; set; } = "";
    public List<RevisorReportePreliminarEstudianteItem> Estudiantes { get; set; } = new();
}

public sealed class OficioAsignacionRevisorReportePreliminarConsolidadoRequest
{
    public string Ciudad { get; set; } = "Oaxaca de Juárez, Oaxaca";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Oficio { get; set; } = "JV-XXX/2026";
    public string Asunto { get; set; } = "Asignación de revisor de reporte preliminar";
    public string NumeralLineamiento { get; set; } = "12.4.1.7";

    public string DestinatarioNombre { get; set; } = "";
    public string DestinatarioCargoLinea1 { get; set; } = "CATEDRATICO(A) DEL I.T. DE OAXACA";

    public List<ProyectoRevisorReportePreliminarItem> Proyectos { get; set; } = new();

    public string FirmaNombre { get; set; } = "";
    public string FirmaCargoLinea1 { get; set; } = "SUBDIRECTORA ACADÉMICA";
}

public partial interface IConstanciasPdfService
    {
        byte[] BuildOficioAsignacionAsesorInternoConsolidado(
            byte[] templatePdf, OficioAsignacionAsesorInternoConsolidadoRequest req);

        byte[] BuildOficioAsignacionRevisorReportePreliminarConsolidado(
            byte[] templatePdf, OficioAsignacionRevisorReportePreliminarConsolidadoRequest req);

        /// <summary>
        /// Concatena en un solo PDF varias "Constancias de aceptación de reporte
        /// preliminar" (una página por proyecto/estudiante), para el mismo asesor
        /// interno. Reutiliza BuildConstanciaAceptacionReportePreliminar por dentro,
        /// por lo que el layout de cada página no cambia.
        /// </summary>
        byte[] BuildConstanciasAceptacionReportePreliminarConsolidado(
            byte[] templatePdf, List<ConstanciaAceptacionReportePreliminarRequest> items);
    }

namespace WebApiVinculacionProyectosV2.Services
{
    public partial class ConstanciasPdfService : IConstanciasPdfService
    {
        // ── Helpers locales (independientes de los privados del otro archivo) ──

        private static List<string> C_WrapLines(string text, PdfFont f, float size, float maxWidth)
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

        private PdfPage C_NewTemplatedPage(PdfDocument outPdf, byte[] templatePdf)
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

            // Compensar flip vertical del membrete escaneado (mismo criterio que el resto del servicio)
            var pdfCanvas = new PdfCanvas(page);
            var rect = page.GetPageSizeWithRotation();
            var pageRotation = page.GetRotation();
            bool hasYFlip = false;

            if (pageRotation == 0)
            {
                try
                {
                    var pageDict = page.GetPdfObject();
                    var contentsObj = pageDict.Get(PdfName.Contents);
                    PdfStream? firstStream = null;

                    if (contentsObj is PdfArray arr && arr.Size() > 0) firstStream = arr.GetAsStream(0);
                    else if (contentsObj is PdfStream s) firstStream = s;

                    if (firstStream != null)
                    {
                        var bytes = firstStream.GetBytes();
                        var preview = System.Text.Encoding.Latin1.GetString(bytes, 0, Math.Min(bytes.Length, 80));
                        hasYFlip = System.Text.RegularExpressions.Regex.IsMatch(preview, @"1\s+0\s+0\s+-1\s+0\s+[\d.]+\s+cm");
                    }
                }
                catch { }
            }

            float w = rect.GetWidth(), h = rect.GetHeight();
            if (hasYFlip) pdfCanvas.ConcatMatrix(1, 0, 0, -1, 0, h);
            else if (pageRotation == 90) pdfCanvas.ConcatMatrix(0, 1, -1, 0, h, 0);
            else if (pageRotation == 180) pdfCanvas.ConcatMatrix(-1, 0, 0, -1, w, h);
            else if (pageRotation == 270) pdfCanvas.ConcatMatrix(0, -1, 1, 0, 0, w);

            return page;
        }

        // ═══════════════════════════════════════════════════════════════════
        // FORMATO OFICIAL (comunes a los oficios de asignación)
        //
        // Se replica el formato de los oficios reales firmados por la Jefatura:
        //   • Fecha, No. de oficio y asunto alineados a la derecha.
        //   • Destinatario en 3 líneas (nombre / CATEDRATICO(A) DEL I.T. DE OAXACA / P R E S E N T E).
        //   • "A T E N T A M E N T E" a la izquierda + lema en cursiva + espacio para firma y sello.
        //   • Firma con el nombre y cargo del Jefe, y "ccp. Expediente / ICMO/mmvh*".
        // ═══════════════════════════════════════════════════════════════════
        private const string C_Lema1 = "Excelencia en Educación Tecnológica®";
        private const string C_Lema2 = "\"Tecnología Propia e Independencia Económica\"";
        private const string C_CargoJefe = "JEFE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN";
        private const string C_CcpIniciales = "ICMO/mmvh*";

        /// <summary>Escritor de oficios: maneja páginas con membrete, cursor vertical, párrafos y tablas.</summary>
        private sealed class C_Oficio
        {
            public sealed class Run
            {
                public string T; public bool Bold;
                public Run(string t, bool bold = false) { T = t; Bold = bold; }
            }

            public sealed class Cell
            {
                public string T; public bool Bold; public TextAlignment Al;
                public Cell(string t, bool bold = false, TextAlignment al = TextAlignment.LEFT) { T = t ?? ""; Bold = bold; Al = al; }
            }

            private readonly ConstanciasPdfService _svc;
            private readonly PdfDocument _pdf;
            private readonly byte[] _tpl;
            private readonly Action<C_Oficio> _header;

            public readonly PdfFont F, B, I;
            public PdfPage Page = null!;
            public PdfCanvas Cv = null!;
            public float PageW, PageH, Right, Width;
            public readonly float Left = 72f;
            public float Y;                         // cursor (medido desde arriba de la página)
            public const float BodyTop = 268f;      // donde empieza el cuerpo, debajo del destinatario
            public const float BodyBottom = 652f;   // el cuerpo no pasa de aquí (ccp y pie del membrete van abajo)

            public C_Oficio(ConstanciasPdfService svc, PdfDocument pdf, byte[] tpl, Action<C_Oficio> header)
            {
                _svc = svc; _pdf = pdf; _tpl = tpl; _header = header;
                F = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                B = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                I = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
            }

            public void NewPage()
            {
                Page = _svc.C_NewTemplatedPage(_pdf, _tpl);
                var ps = Page.GetPageSize();
                PageW = ps.GetWidth(); PageH = ps.GetHeight();
                Right = PageW - 72f; Width = Right - Left;
                Cv = new PdfCanvas(Page);
                Y = BodyTop;
                _header(this);
            }

            public void Ensure(float h) { if (Y + h > BodyBottom) NewPage(); }

            // ── Primitivas ───────────────────────────────────────────────
            public float W(string t, PdfFont f, float size) => f.GetWidth(t ?? "", size);

            public void Text(string t, float x, float baseline, PdfFont f, float size)
            {
                Cv.BeginText();
                Cv.SetFontAndSize(f, size);
                Cv.MoveText(x, PageH - baseline);
                Cv.ShowText(t ?? "");
                Cv.EndText();
            }

            public void TextRight(string t, float rightX, float baseline, PdfFont f, float size)
                => Text(t, rightX - W(t, f, size), baseline, f, size);

            // ── Párrafos con negritas ────────────────────────────────────
            private List<List<(string w, bool b, bool glue)>> WrapRuns(IEnumerable<Run> runs, float size, float maxW)
            {
                var lines = new List<List<(string, bool, bool)>>();
                var cur = new List<(string, bool, bool)>();
                float curW = 0f, space = F.GetWidth(" ", size);
                bool hasPrev = false, prevEndsSpace = false;

                foreach (var r in runs)
                {
                    var txt = r.T ?? "";
                    bool startsSpace = txt.Length > 0 && char.IsWhiteSpace(txt[0]);
                    bool first = true;
                    foreach (var w in txt.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var f = r.Bold ? B : F;
                        float ww = f.GetWidth(w, size);
                        bool glue = first && hasPrev && !startsSpace && !prevEndsSpace;
                        first = false;
                        float add = cur.Count == 0 ? ww : (glue ? ww : space + ww);
                        if (cur.Count > 0 && curW + add > maxW)
                        {
                            lines.Add(cur); cur = new List<(string, bool, bool)>(); curW = 0f; add = ww; glue = false;
                        }
                        cur.Add((w, r.Bold, glue));
                        curW += add;
                        hasPrev = true;
                    }
                    if (txt.Length > 0) prevEndsSpace = char.IsWhiteSpace(txt[txt.Length - 1]);
                }
                if (cur.Count > 0) lines.Add(cur);
                return lines;
            }

            public float ParaHeight(IEnumerable<Run> runs, float size, float leading)
                => WrapRuns(runs, size, Width).Count * leading;

            public void Para(IEnumerable<Run> runs, float size, float leading, float gapAfter)
            {
                var lines = WrapRuns(runs, size, Width);
                Ensure(lines.Count * leading);
                float space = F.GetWidth(" ", size);

                foreach (var line in lines)
                {
                    float x = Left;
                    bool firstTok = true;
                    foreach (var (w, b, glue) in line)
                    {
                        if (!firstTok && !glue) x += space;
                        var f = b ? B : F;
                        Text(w, x, Y + size, f, size);
                        x += f.GetWidth(w, size);
                        firstTok = false;
                    }
                    Y += leading;
                }
                Y += gapAfter;
            }

            // ── Tablas con bordes ────────────────────────────────────────
            public static float[] Cols(float totalWidth, params float[] proportions)
            {
                float sum = proportions.Sum();
                return proportions.Select(p => p * totalWidth / sum).ToArray();
            }

            private List<string> CellLines(Cell c, float colW, float size, float pad)
            {
                var f = c.Bold ? B : F;
                var res = new List<string>();
                foreach (var seg in (c.T ?? "").Split('\n'))
                {
                    var wl = C_WrapLines(seg, f, size, colW - pad * 2);
                    if (wl.Count == 0) wl.Add("");
                    res.AddRange(wl);
                }
                return res;
            }

            public float RowHeight(Cell[] cells, float[] colW, float size, float leading, float pad, float min)
            {
                int max = 1;
                for (int i = 0; i < cells.Length; i++)
                    max = Math.Max(max, CellLines(cells[i], colW[i], size, pad).Count);
                return Math.Max(min, max * leading + pad * 2);
            }

            public void DrawRow(Cell[] cells, float[] colW, float top, float h, float size, float leading, float pad)
            {
                float cx = Left;
                for (int i = 0; i < cells.Length; i++)
                {
                    Cv.SaveState();
                    Cv.SetLineWidth(0.8f);
                    Cv.Rectangle(cx, PageH - (top + h), colW[i], h);
                    Cv.Stroke();
                    Cv.RestoreState();

                    var f = cells[i].Bold ? B : F;
                    var lines = CellLines(cells[i], colW[i], size, pad);
                    float textH = lines.Count * leading;
                    float ty = top + (h - textH) / 2f;

                    for (int l = 0; l < lines.Count; l++)
                    {
                        float tw = f.GetWidth(lines[l], size);
                        float x = cells[i].Al == TextAlignment.CENTER ? cx + (colW[i] - tw) / 2f
                                : cells[i].Al == TextAlignment.RIGHT ? cx + colW[i] - pad - tw
                                : cx + pad;
                        Text(lines[l], x, ty + l * leading + size, f, size);
                    }
                    cx += colW[i];
                }
            }

            /// <summary>Tabla que cabe completa en una página (si no cabe, salta de página antes).</summary>
            public void GridBlock(float[] colW, List<Cell[]> rows, float size, float leading, float pad, float min)
            {
                float total = rows.Sum(r => RowHeight(r, colW, size, leading, pad, min));
                Ensure(total);
                foreach (var r in rows)
                {
                    float rh = RowHeight(r, colW, size, leading, pad, min);
                    DrawRow(r, colW, Y, rh, size, leading, pad);
                    Y += rh;
                }
            }

            /// <summary>Tabla con encabezado que se repite si continúa en otra página.</summary>
            public void GridPaged(float[] colW, Cell[] header, List<Cell[]> rows, float size, float leading, float pad, float min)
            {
                float hH = RowHeight(header, colW, size, leading, pad, min);
                float firstH = rows.Count > 0 ? RowHeight(rows[0], colW, size, leading, pad, min) : 0f;
                Ensure(hH + firstH);

                DrawRow(header, colW, Y, hH, size, leading, pad);
                Y += hH;

                foreach (var r in rows)
                {
                    float rh = RowHeight(r, colW, size, leading, pad, min);
                    if (Y + rh > BodyBottom)
                    {
                        NewPage();
                        DrawRow(header, colW, Y, hH, size, leading, pad);
                        Y += hH;
                    }
                    DrawRow(r, colW, Y, rh, size, leading, pad);
                    Y += rh;
                }
            }
        }

        /// <summary>Fecha / oficio / asunto (derecha) + destinatario (izquierda). Se repite en cada página.</summary>
        private static void C_DrawOficioHeader(C_Oficio o, string fechaTxt, string oficio, string asunto, string destNombre, string destCargo)
        {
            const float fechaY = 140f, oficioY = 154f, asuntoY = 168f;
            const float destY = 214f, cargoY = 231f, presenteY = 248f;

            o.TextRight(fechaTxt, o.Right, fechaY, o.F, 9.6f);
            o.TextRight($"OFICIO No. {oficio}", o.Right, oficioY, o.B, 9.9f);
            o.TextRight($"ASUNTO: {asunto}", o.Right, asuntoY, o.F, 9.6f);

            o.Text((destNombre ?? "").ToUpperInvariant(), o.Left, destY, o.B, 9.9f);
            o.Text((destCargo ?? "").ToUpperInvariant(), o.Left, cargoY, o.B, 9.7f);
            o.Text("P R E S E N T E", o.Left, presenteY, o.B, 9.7f);
        }

        /// <summary>A T E N T A M E N T E + lema + espacio para firma/sello + nombre y cargo + ccp.</summary>
        private static void C_DrawFirmaBloque(C_Oficio o, string firmaNombre, string firmaCargo)
        {
            o.Ensure(126f);
            o.Y += 4f;
            o.Text("A T E N T A M E N T E", o.Left, o.Y + 10f, o.B, 9.9f); o.Y += 16f;
            o.Text(C_Lema1, o.Left, o.Y + 8f, o.I, 8.4f); o.Y += 11f;
            o.Text(C_Lema2, o.Left, o.Y + 8f, o.I, 8.4f); o.Y += 11f;
            o.Y += 54f; // espacio para la firma y el sello
            o.Text((firmaNombre ?? "").ToUpperInvariant(), o.Left, o.Y + 10f, o.B, 9.8f); o.Y += 13f;
            o.Text((firmaCargo ?? "").ToUpperInvariant(), o.Left, o.Y + 10f, o.B, 9.6f); o.Y += 13f;

            // ccp: fijo en la parte baja (por encima del pie del membrete)
            o.Text("ccp. Expediente", o.Left, 668f, o.F, 8.8f);
            o.Text(C_CcpIniciales, o.Left, 680f, o.F, 8.8f);
        }

        /// <summary>"ENE-JUN 2026" → "ENERO-JUNIO/2026" (como aparece en el oficio real).</summary>
        private static string C_PeriodoLargo(string periodo)
        {
            var txt = (periodo ?? "").Trim().ToUpperInvariant();
            if (txt.Length == 0) return "—";

            var meses = new Dictionary<string, string>
            {
                ["ENE"] = "ENERO", ["FEB"] = "FEBRERO", ["MAR"] = "MARZO", ["ABR"] = "ABRIL",
                ["MAY"] = "MAYO", ["JUN"] = "JUNIO", ["JUL"] = "JULIO", ["AGO"] = "AGOSTO",
                ["SEP"] = "SEPTIEMBRE", ["OCT"] = "OCTUBRE", ["NOV"] = "NOVIEMBRE", ["DIC"] = "DICIEMBRE"
            };

            var m = System.Text.RegularExpressions.Regex.Match(txt, @"^([A-ZÁÉÍÓÚ]{3,})\s*-\s*([A-ZÁÉÍÓÚ]{3,})[\s/]+(\d{4})$");
            if (!m.Success) return txt;

            string Largo(string s) => meses.TryGetValue(s, out var l) ? l : s;
            return $"{Largo(m.Groups[1].Value)}-{Largo(m.Groups[2].Value)}/{m.Groups[3].Value}";
        }

        // ═══════════════════════════════════════════════════════════════════
        // ASESOR INTERNO — consolidado (varios proyectos del mismo docente)
        //
        // Formato real: tabla vertical por proyecto con las filas
        //   Número de control / Nombre del residente / Carrera /
        //   Nombre del proyecto / Período de realización / Empresa
        // ═══════════════════════════════════════════════════════════════════
        public byte[] BuildOficioAsignacionAsesorInternoConsolidado(
            byte[] templatePdf, OficioAsignacionAsesorInternoConsolidadoRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            var proyectos = req.Proyectos ?? new List<ProyectoAsesorInternoItem>();
            if (proyectos.Count == 0)
                proyectos.Add(new ProyectoAsesorInternoItem { NombreProyecto = "—", Empresa = "—", Carrera = "—", PeriodoRealizacion = "—" });

            var esMx = CultureInfo.GetCultureInfo("es-MX");
            string fechaTxt = $"{req.Ciudad} {req.Fecha.ToString("dd'/'MMMM'/'yyyy", esMx)}";
            string cargoFirma = string.Join(" ", new[] { req.FirmaCargoLinea1, req.FirmaCargoLinea2 }.Where(s => !string.IsNullOrWhiteSpace(s)));

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var outPdf = new PdfDocument(writer);

            var o = new C_Oficio(this, outPdf, templatePdf,
                h => C_DrawOficioHeader(h, fechaTxt, req.Oficio, "Asesor Interno de Residencia Profesional",
                                        req.DestinatarioNombre, req.DestinatarioCargoLinea1));
            o.NewPage();

            const float size = 9.6f, lead = 11.4f;
            bool varios = proyectos.Count > 1;

            o.Para(new[] { new C_Oficio.Run(varios
                ? "Por este conducto informo a usted que ha sido asignado(a) para fungir como Asesor Interno de los siguientes proyectos de Residencia Profesional:"
                : "Por este conducto informo a usted que ha sido asignado(a) para fungir como Asesor Interno del Proyecto de Residencia Profesional que a continuación se describe:") },
                size, lead, 8f);

            var colW = C_Oficio.Cols(o.Width, 135f, 333f); // etiqueta | valor
            string Up(string s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim().ToUpperInvariant();

            foreach (var p in proyectos)
            {
                var controles = (p.NumerosControl != null && p.NumerosControl.Count > 0)
                    ? p.NumerosControl
                    : (string.IsNullOrWhiteSpace(p.NumeroControl) ? new List<string>() : new List<string> { p.NumeroControl });

                C_Oficio.Cell[] Fila(string etiqueta, string valor) =>
                    new[] { new C_Oficio.Cell(etiqueta), new C_Oficio.Cell(valor) };

                var filas = new List<C_Oficio.Cell[]>
                {
                    Fila("Número de control:", controles.Count > 0 ? string.Join("\n", controles) : "—"),
                    Fila("Nombre del residente:", (p.Residentes != null && p.Residentes.Count > 0)
                        ? string.Join("\n", p.Residentes.Select(Up)) : "—"),
                    Fila("Carrera:", Up(p.Carrera)),
                    Fila("Nombre del proyecto:", Up(p.NombreProyecto)),
                    Fila("Período de realización:", C_PeriodoLargo(p.PeriodoRealizacion)),
                    Fila("Empresa:", Up(p.Empresa)),
                };

                o.GridBlock(colW, filas, 9.4f, 11.2f, 3f, 16f);
                o.Y += 10f;
            }

            o.Y += 2f;
            o.Para(new[] { new C_Oficio.Run(
                $"Así mismo, le solicito dar el seguimiento pertinente a la realización {(varios ? "de cada proyecto" : "del proyecto")} aplicando los lineamientos establecidos para ello, en el procedimiento para Residencia Profesional.") },
                size, lead, 7f);
            o.Para(new[] { new C_Oficio.Run(
                "Agradezco de antemano su valioso apoyo en esta importante actividad para la formación profesional de nuestro estudiantado.") },
                size, lead, 4f);

            C_DrawFirmaBloque(o, req.FirmaNombre, cargoFirma);

            outPdf.Close();
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════════════════
        // REVISOR DE REPORTE PRELIMINAR — consolidado (varios proyectos)
        //
        // Formato real: texto con el nombre del proyecto en negritas y tabla
        // "Número de control | Estudiante" (una fila por estudiante).
        // ═══════════════════════════════════════════════════════════════════
        public byte[] BuildOficioAsignacionRevisorReportePreliminarConsolidado(
            byte[] templatePdf, OficioAsignacionRevisorReportePreliminarConsolidadoRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            var proyectos = req.Proyectos ?? new List<ProyectoRevisorReportePreliminarItem>();
            if (proyectos.Count == 0)
                proyectos.Add(new ProyectoRevisorReportePreliminarItem { NombreProyecto = "—" });

            var esMx = CultureInfo.GetCultureInfo("es-MX");
            string fechaTxt = $"{req.Ciudad} {req.Fecha.ToString("dd 'de' MMMM 'de' yyyy", esMx)}";

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var outPdf = new PdfDocument(writer);

            var o = new C_Oficio(this, outPdf, templatePdf,
                h => C_DrawOficioHeader(h, fechaTxt, req.Oficio, req.Asunto,
                                        req.DestinatarioNombre, req.DestinatarioCargoLinea1));
            o.NewPage();

            const float size = 9.6f, lead = 11.4f;
            bool varios = proyectos.Count > 1;

            o.Para(new[] { new C_Oficio.Run(
                $"Con fundamento en el Lineamiento para la Operación de la Residencia Profesional, específicamente atendiendo al numeral {req.NumeralLineamiento}, " +
                "el cual establece que todo proyecto debe ser autorizado por la Jefatura del Departamento Académico previo análisis de la Academia, se comunica lo siguiente:") },
                size, lead, 8f);

            var colW = C_Oficio.Cols(o.Width, 220f, 260f); // Número de control | Estudiante

            foreach (var p in proyectos)
            {
                var estudiantes = p.Estudiantes ?? new List<RevisorReportePreliminarEstudianteItem>();
                if (estudiantes.Count == 0)
                    estudiantes = new List<RevisorReportePreliminarEstudianteItem> { new() { NumeroControl = "—", NombreEstudiante = "—" } };

                var runs = new[]
                {
                    new C_Oficio.Run("Se le ha designado como "),
                    new C_Oficio.Run("Revisor(a)", true),
                    new C_Oficio.Run(" para evaluar el reporte preliminar del proyecto para Residencia Profesional: "),
                    new C_Oficio.Run((p.NombreProyecto ?? "—").Trim().ToUpperInvariant(), true),
                    new C_Oficio.Run(estudiantes.Count > 1 ? ", el cual presentan los estudiantes:" : ", el cual presenta el estudiante:")
                };

                // El texto y el arranque de la tabla se quedan juntos
                o.Ensure(o.ParaHeight(runs, size, lead) + 8f + 36f);
                o.Para(runs, size, lead, 8f);

                var header = new[]
                {
                    new C_Oficio.Cell("Número de control", true, TextAlignment.CENTER),
                    new C_Oficio.Cell("Estudiante", true, TextAlignment.CENTER)
                };
                var filas = estudiantes.Select(e => new[]
                {
                    new C_Oficio.Cell(e.NumeroControl, false, TextAlignment.CENTER),
                    new C_Oficio.Cell((e.NombreEstudiante ?? "—").Trim().ToUpperInvariant(), false, TextAlignment.CENTER)
                }).ToList();

                o.GridPaged(colW, header, filas, 9.4f, 11.2f, 3f, 16f);
                o.Y += 10f;
            }

            o.Para(new[] { new C_Oficio.Run(
                $"Su análisis técnico permitirá a la Academia determinar la viabilidad y pertinencia {(varios ? "de los proyectos" : "del proyecto")} para su posterior autorización por esta Jefatura.") },
                size, lead, 7f);
            o.Para(new[]
            {
                new C_Oficio.Run($"En caso de autorizar {(varios ? "los proyectos" : "el proyecto")}, deberá entregar a la oficina de Vinculación el "),
                new C_Oficio.Run("Anexo II", true),
                new C_Oficio.Run(", por cada estudiante asignado.")
            }, size, lead, 7f);
            o.Para(new[] { new C_Oficio.Run("Agradezco de antemano su valiosa colaboración en este proceso.") },
                size, lead, 4f);

            C_DrawFirmaBloque(o, req.FirmaNombre, req.FirmaCargoLinea1);

            outPdf.Close();
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════════════════
        // ACEPTACIÓN DE REPORTE PRELIMINAR — consolidado
        // (concatena N páginas, una por proyecto/estudiante, reutilizando
        //  BuildConstanciaAceptacionReportePreliminar sin tocar su layout)
        // ═══════════════════════════════════════════════════════════════════
        public byte[] BuildConstanciasAceptacionReportePreliminarConsolidado(
            byte[] templatePdf, List<ConstanciaAceptacionReportePreliminarRequest> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Debe incluir al menos un reporte a aceptar.", nameof(items));

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var outPdf = new PdfDocument(writer);

            foreach (var item in items)
            {
                var singlePdfBytes = BuildConstanciaAceptacionReportePreliminar(templatePdf, item);

                using var srcMs = new MemoryStream(singlePdfBytes);
                using var srcReader = new PdfReader(srcMs);
                using var srcPdf = new PdfDocument(srcReader);

                srcPdf.CopyPagesTo(1, srcPdf.GetNumberOfPages(), outPdf);
            }

            outPdf.Close();
            return ms.ToArray();
        }
    }
}
