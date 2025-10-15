namespace PMCRMS.API.Common
{
    using System;
    using System.IO;
    using QRCoder;

    public static class QrCodeGenerator
    {
        public static byte[] GenerateQrCode(string data, int pixelsPerModule = 10)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(pixelsPerModule);
                }
            }
        }
    }
}
