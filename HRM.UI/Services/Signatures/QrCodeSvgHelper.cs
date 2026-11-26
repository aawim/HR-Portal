using System.Text;
using QRCoder;

namespace HRM.UI.Services.Signatures;

public static class QrCodeSvgHelper
{
    public static string GenerateSvgDataUri(string payload, int pixelsPerModule = 4)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return string.Empty;

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var svgQr = new SvgQRCode(data);
        var svg = svgQr.GetGraphic(pixelsPerModule, "#000000", "#ffffff", drawQuietZones: true);
        return $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";
    }
}
