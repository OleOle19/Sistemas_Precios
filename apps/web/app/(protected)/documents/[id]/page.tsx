import { ReviewTable } from "../../../../components/review-table";
import { getDocumentDetail, requireSession } from "../../../../lib/api";
import { canEditPrices } from "../../../../lib/permissions";

export const dynamic = "force-dynamic";

export default async function DocumentReviewPage({
  params
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [document, session] = await Promise.all([getDocumentDetail(id), requireSession()]);

  return <ReviewTable document={document} canWrite={canEditPrices(session)} />;
}
