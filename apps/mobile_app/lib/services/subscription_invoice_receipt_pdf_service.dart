import 'dart:io';
import 'dart:typed_data';

import 'package:path_provider/path_provider.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

class SubscriptionInvoiceReceiptPdfData {
  const SubscriptionInvoiceReceiptPdfData({
    required this.brandName,
    required this.title,
    required this.invoiceLabel,
    required this.invoiceId,
    required this.amountLabel,
    required this.amount,
    required this.paymentMethodLabel,
    required this.paymentMethod,
    required this.paymentReferenceLabel,
    required this.paymentReference,
    required this.statusLabel,
    required this.status,
    required this.disclaimer,
  });

  final String brandName;
  final String title;
  final String invoiceLabel;
  final String invoiceId;
  final String amountLabel;
  final String amount;
  final String paymentMethodLabel;
  final String paymentMethod;
  final String paymentReferenceLabel;
  final String paymentReference;
  final String statusLabel;
  final String status;
  final String disclaimer;
}

class SubscriptionInvoiceReceiptPdfExportResult {
  const SubscriptionInvoiceReceiptPdfExportResult({
    required this.fileName,
    required this.path,
    required this.bytesLength,
  });

  final String fileName;
  final String path;
  final int bytesLength;
}

typedef ReceiptDirectoryProvider = Future<Directory> Function();

class SubscriptionInvoiceReceiptPdfService {
  SubscriptionInvoiceReceiptPdfService({
    ReceiptDirectoryProvider? directoryProvider,
  }) : _directoryProvider = directoryProvider ?? getApplicationDocumentsDirectory;

  final ReceiptDirectoryProvider _directoryProvider;

  String fileNameForInvoice(String invoiceId) {
    final safeInvoiceId = invoiceId.replaceAll(RegExp(r'[^A-Za-z0-9._-]'), '-');
    return 'afwal-receipt-$safeInvoiceId.pdf';
  }

  Future<Uint8List> buildPdfBytes(SubscriptionInvoiceReceiptPdfData data) async {
    final document = pw.Document();

    document.addPage(
      pw.MultiPage(
        pageFormat: PdfPageFormat.a4,
        margin: const pw.EdgeInsets.all(40),
        build: (context) => [
          pw.Text(
            data.brandName,
            style: pw.TextStyle(
              fontSize: 22,
              fontWeight: pw.FontWeight.bold,
            ),
          ),
          pw.SizedBox(height: 8),
          pw.Text(
            data.title,
            style: pw.TextStyle(
              fontSize: 18,
              fontWeight: pw.FontWeight.bold,
            ),
          ),
          pw.SizedBox(height: 24),
          _row(data.invoiceLabel, data.invoiceId),
          _row(data.amountLabel, data.amount),
          _row(data.paymentMethodLabel, data.paymentMethod),
          _row(data.paymentReferenceLabel, data.paymentReference),
          _row(data.statusLabel, data.status),
          pw.SizedBox(height: 24),
          pw.Divider(),
          pw.SizedBox(height: 12),
          pw.Text(
            data.disclaimer,
            style: const pw.TextStyle(fontSize: 10),
          ),
        ],
      ),
    );

    return document.save();
  }

  Future<SubscriptionInvoiceReceiptPdfExportResult> export({
    required String invoiceId,
    required SubscriptionInvoiceReceiptPdfData data,
  }) async {
    final bytes = await buildPdfBytes(data);
    final directory = await _directoryProvider();
    final fileName = fileNameForInvoice(invoiceId);
    final file = File('${directory.path}${Platform.pathSeparator}$fileName');

    await file.writeAsBytes(bytes, flush: true);

    return SubscriptionInvoiceReceiptPdfExportResult(
      fileName: fileName,
      path: file.path,
      bytesLength: bytes.length,
    );
  }

  pw.Widget _row(String label, String value) {
    return pw.Padding(
      padding: const pw.EdgeInsets.symmetric(vertical: 6),
      child: pw.Row(
        crossAxisAlignment: pw.CrossAxisAlignment.start,
        children: [
          pw.Expanded(
            flex: 2,
            child: pw.Text(
              label,
              style: pw.TextStyle(fontWeight: pw.FontWeight.bold),
            ),
          ),
          pw.SizedBox(width: 12),
          pw.Expanded(
            flex: 3,
            child: pw.Text(value),
          ),
        ],
      ),
    );
  }
}
