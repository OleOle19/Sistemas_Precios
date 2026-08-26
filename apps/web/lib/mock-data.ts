import type {
  ComparisonHistoryItem,
  CurrentComparison,
  DashboardSummary,
  DocumentDetail,
  DocumentSummary,
  Supplier
} from "./types";

export const mockDashboard: DashboardSummary = {
  documentsProcessed: 14,
  suppliersCompared: 6,
  pendingReviews: 3,
  productsTracked: 28,
  bestPrices: [
    {
      productName: "Aceite vegetal 1L",
      supplierName: "Distribuidora Andina",
      bestPrice: 8.9,
      averagePrice: 9.7,
      spreadPercentage: 14.3
    },
    {
      productName: "Arroz extra 5kg",
      supplierName: "Mercado Norte",
      bestPrice: 22.5,
      averagePrice: 24.1,
      spreadPercentage: 18.1
    }
  ],
  biggestMovers: [
    {
      productName: "Azucar rubia 1kg",
      supplierName: "Proveedor Central",
      previousPrice: 4.3,
      currentPrice: 4.9,
      variationPercentage: 13.95
    },
    {
      productName: "Leche evaporada 410g",
      supplierName: "Distribuidora Andina",
      previousPrice: 3.7,
      currentPrice: 3.4,
      variationPercentage: -8.11
    }
  ]
};

export const mockSuppliers: Supplier[] = [
  {
    id: "sup-1",
    name: "Distribuidora Andina",
    contactEmail: "andina@proveedores.local",
    documentCount: 4
  },
  {
    id: "sup-2",
    name: "Mercado Norte",
    contactEmail: "norte@proveedores.local",
    documentCount: 5
  }
];

export const mockDocuments: DocumentSummary[] = [
  {
    id: "doc-1",
    supplierId: "sup-1",
    supplierName: "Distribuidora Andina",
    fileName: "lista-andina-junio.jpg",
    contentType: "image/jpeg",
    status: "NeedsReview",
    uploadedAt: new Date().toISOString(),
    extractedLineCount: 4,
    failureReason: null
  },
  {
    id: "doc-2",
    supplierId: "sup-2",
    supplierName: "Mercado Norte",
    fileName: "lista-norte-junio.pdf",
    contentType: "application/pdf",
    status: "Approved",
    uploadedAt: new Date(Date.now() - 86400000).toISOString(),
    extractedLineCount: 4,
    failureReason: null
  }
];

export const mockDocumentDetail: DocumentDetail = {
  ...mockDocuments[0],
  lines: [
    {
      id: "line-1",
      lineNumber: 1,
      rawText: "ACEITE VEGETAL 1L S/ 8.90",
      suggestedName: "Aceite vegetal",
      suggestedUnit: "L",
      suggestedQuantity: 1,
      suggestedPrice: 8.9,
      confidenceScore: 0.93,
      needsReview: false,
      approvedCanonicalProductId: "prod-1",
      approvedCanonicalProductName: "Aceite vegetal 1L",
      approvedPrice: 8.9,
      approvedQuantity: 1,
      approvedUnit: "L",
      matches: [
        {
          id: "match-1",
          canonicalProductId: "prod-1",
          canonicalProductName: "Aceite vegetal 1L",
          confidenceScore: 0.93,
          status: "Approved"
        }
      ]
    },
    {
      id: "line-2",
      lineNumber: 2,
      rawText: "ARROZ EXTRA 5KG 22.50",
      suggestedName: "Arroz extra",
      suggestedUnit: "KG",
      suggestedQuantity: 5,
      suggestedPrice: 22.5,
      confidenceScore: 0.82,
      needsReview: true,
      approvedCanonicalProductId: null,
      approvedCanonicalProductName: null,
      approvedPrice: null,
      approvedQuantity: null,
      approvedUnit: null,
      matches: [
        {
          id: "match-2",
          canonicalProductId: "prod-2",
          canonicalProductName: "Arroz extra 5kg",
          confidenceScore: 0.82,
          status: "Suggested"
        }
      ]
    }
  ]
};

export const mockComparisons: CurrentComparison[] = [
  {
    productId: "prod-1",
    productName: "Aceite vegetal 1L",
    baseUnit: "L",
    bestSupplier: "Distribuidora Andina",
    bestPrice: 8.9,
    averagePrice: 9.7,
    highestPrice: 10.2,
    spreadPercentage: 14.61,
    calculatedAt: new Date().toISOString()
  },
  {
    productId: "prod-2",
    productName: "Arroz extra 5kg",
    baseUnit: "KG",
    bestSupplier: "Mercado Norte",
    bestPrice: 22.5,
    averagePrice: 24.1,
    highestPrice: 26.1,
    spreadPercentage: 16,
    calculatedAt: new Date().toISOString()
  }
];

export const mockHistory: ComparisonHistoryItem[] = [
  {
    productName: "Azucar rubia 1kg",
    supplierName: "Proveedor Central",
    unit: "KG",
    price: 4.3,
    effectiveAt: new Date(Date.now() - 5 * 86400000).toISOString(),
    variationPercentage: null
  },
  {
    productName: "Azucar rubia 1kg",
    supplierName: "Proveedor Central",
    unit: "KG",
    price: 4.9,
    effectiveAt: new Date().toISOString(),
    variationPercentage: 13.95
  }
];
