export const statusLabel = (status: string) =>
  ({
    Uploaded: "En cola",
    Processing: "Leyendo",
    NeedsReview: "Por revisar",
    Approved: "Aprobado",
    Failed: "Lectura fallida",
    Processed: "Por revisar",
  })[status] ?? status;
export const money = (amount: number, currency: string, unit?: string) =>
  `${new Intl.NumberFormat("es-PE", { style: "currency", currency, maximumFractionDigits: 4 }).format(amount)}${unit ? ` / ${unit}` : ""}`;
