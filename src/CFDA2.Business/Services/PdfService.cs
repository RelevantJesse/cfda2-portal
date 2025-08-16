using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Microsoft.EntityFrameworkCore;
using Portal.Server.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Server.Services;

public class PdfService : IPdfService
{
    private readonly AppDbContext _db;
    public PdfService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateStatementAsync(int familyId, int year, int month)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);
        var ledger = await _db.LedgerEntries.Where(l => l.FamilyId == familyId && l.PostedUtc >= from && l.PostedUtc < to).OrderBy(l => l.PostedUtc).ToListAsync();
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        using var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Arial", 12);
        double y = 40;
        gfx.DrawString($"Statement for Family {familyId} - {year}-{month:D2}", font, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
        y += 30;
        foreach (var entry in ledger)
        {
            gfx.DrawString(entry.PostedUtc.ToString("yyyy-MM-dd"), font, XBrushes.Black, new XRect(40, y, 100, 20), XStringFormats.TopLeft);
            gfx.DrawString(entry.Type.ToString(), font, XBrushes.Black, new XRect(150, y, 100, 20), XStringFormats.TopLeft);
            gfx.DrawString((entry.AmountCents / 100.0m).ToString("C2"), font, XBrushes.Black, new XRect(260, y, 100, 20), XStringFormats.TopLeft);
            gfx.DrawString(entry.Memo, font, XBrushes.Black, new XRect(370, y, page.Width - 410, 20), XStringFormats.TopLeft);
            y += 20;
            if (y > page.Height - 50)
            {
                page = document.AddPage();
                gfx.Dispose();
                gfx = XGraphics.FromPdfPage(page);
                y = 40;
            }
        }
        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }
}