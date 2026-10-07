using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Queries.GetSkuByBarcode;

public record GetSkuByBarcodeQuery(string Barcode) : IQuery<SkuBarcodeLookupDto>;
