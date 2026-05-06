using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Application.Dtos.En16931;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using s2industries.ZUGFeRD;
using s2industries.ZUGFeRD.PDF;

namespace EasyMitt.Infrastructure.ElectronicInvoicing;

public sealed class S2ElectronicInvoiceGenerator : IElectronicInvoiceGenerator
{
    static S2ElectronicInvoiceGenerator()
    {
        GlobalFontSettings.UseWindowsFontsUnderWindows = true;
    }

    public Task<byte[]> GenerateXRechnungXmlAsync(InvoiceDocumentDto document, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var desc = InvoiceDescriptorMapper.ToDescriptor(document);
        using var ms = new MemoryStream();
        desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
        return Task.FromResult(ms.ToArray());
    }

    public Task<byte[]> GenerateZugferdPdfAsync(
        InvoiceDocumentDto document,
        Stream? visualPdf,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var desc = InvoiceDescriptorMapper.ToDescriptor(document);
        using var shell = visualPdf is null ? CreateDefaultShellPdfStream() : CopyToOwnedMemoryStream(visualPdf);
        shell.Position = 0;

        using var output = new MemoryStream();
        InvoicePdfProcessor.SaveToPdf(
            output,
            ZUGFeRDVersion.Version23,
            Profile.Comfort,
            ZUGFeRDFormats.CII,
            shell,
            desc);
        return Task.FromResult(output.ToArray());
    }

    private static MemoryStream CopyToOwnedMemoryStream(Stream source)
    {
        var ms = new MemoryStream();
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        source.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateDefaultShellPdfStream()
    {
        var doc = new PdfDocument();
        doc.Info.Title = "EasyMitt";
        var page = doc.AddPage();
        using (var gfx = XGraphics.FromPdfPage(page))
        {
            var font = new XFont("Arial", 14, XFontStyleEx.Bold);
            gfx.DrawString("EasyMitt", font, XBrushes.DarkSlateGray, new XRect(40, 40, 400, 30), XStringFormats.TopLeft);
        }

        var ms = new MemoryStream();
        doc.Save(ms, false);
        ms.Position = 0;
        return ms;
    }
}
