export type DashboardSummary = {
  documentsProcessed: number;
  suppliersCompared: number;
  pendingReviews: number;
  productsTracked: number;
  bestPrices: TopPriceOpportunity[];
  biggestMovers: PriceMover[];
};

export type TopPriceOpportunity = {
  productName: string;
  supplierName: string;
  bestPrice: number;
  averagePrice: number;
  spreadPercentage: number;
};

export type PriceMover = {
  productName: string;
  supplierName: string;
  previousPrice: number;
  currentPrice: number;
  variationPercentage: number;
};

export type Supplier = {
  id: string;
  name: string;
  contactEmail?: string | null;
  documentCount: number;
};

export type ProductMatch = {
  id: string;
  canonicalProductId: string;
  canonicalProductName: string;
  confidenceScore: number;
  status: string;
};

export type ExtractedLine = {
  id: string;
  lineNumber: number;
  rawText: string;
  suggestedName?: string | null;
  suggestedUnit?: string | null;
  suggestedQuantity?: number | null;
  suggestedPrice?: number | null;
  confidenceScore: number;
  needsReview: boolean;
  approvedCanonicalProductId?: string | null;
  approvedCanonicalProductName?: string | null;
  approvedPrice?: number | null;
  approvedQuantity?: number | null;
  approvedUnit?: string | null;
  matches: ProductMatch[];
};

export type DocumentSummary = {
  id: string;
  supplierId: string;
  supplierName: string;
  fileName: string;
  contentType: string;
  status: string;
  uploadedAt: string;
  extractedLineCount: number;
  failureReason?: string | null;
};

export type DocumentDetail = DocumentSummary & {
  lines: ExtractedLine[];
};

export type CurrentComparison = {
  productId: string;
  productName: string;
  baseUnit: string;
  bestSupplier: string;
  bestPrice: number;
  averagePrice: number;
  highestPrice: number;
  spreadPercentage: number;
  calculatedAt: string;
};

export type ComparisonHistoryItem = {
  productName: string;
  supplierName: string;
  unit: string;
  price: number;
  effectiveAt: string;
  variationPercentage?: number | null;
};
