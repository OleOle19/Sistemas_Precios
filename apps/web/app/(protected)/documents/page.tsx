import Link from "next/link";
import { statusLabel } from "../../../lib/display";
import { DocumentUploadForm } from "../../../components/document-upload-form";
import {
  getDocuments,
  getSuppliers,
  getExtractionSettings,
  requireSession,
} from "../../../lib/api";
import { canEditPrices } from "../../../lib/permissions";

export const dynamic = "force-dynamic";

export default async function DocumentsPage() {
  const [suppliers, documents, settings, session] = await Promise.all([
    getSuppliers(),
    getDocuments(),
    getExtractionSettings(),
    requireSession(),
  ]);

  return (
    <div className="page-stack">
      {canEditPrices(session) && <DocumentUploadForm
        suppliers={suppliers}
        configured={settings.configured}
        provider={settings.provider}
      />}

      <section className="card">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Documentos</p>
            <h2>Documentos recientes</h2>
          </div>
        </div>
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Proveedor</th>
                <th>Archivo</th>
                <th>Estado</th>
                <th>Lineas</th>
                <th>Subido</th>
              </tr>
            </thead>
            <tbody>
              {documents.map((document) => (
                <tr key={document.id}>
                  <td>{document.supplierName}</td>
                  <td>
                    <Link href={`/documents/${document.id}`}>
                      {document.fileName}
                    </Link>
                  </td>
                  <td>{statusLabel(document.status)}</td>
                  <td>{document.extractedLineCount}</td>
                  <td>
                    {new Date(document.uploadedAt).toLocaleString("es-PE")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
