export function generateQR(canvasId, text) {
    QRCode.toCanvas(document.getElementById(canvasId), text, function (error) {
        if (error) console.error("QR Code error:", error);
    });
}