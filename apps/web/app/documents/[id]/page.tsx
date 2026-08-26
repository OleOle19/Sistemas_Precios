import { ReviewTable } from "../../../components/review-table";
import { getDocumentDetail } from "../../../lib/api";

export const dynamic = "force-dynamic";

export default async function DocumentReviewPage({
  params
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const document = await getDocumentDetail(id);

  return <ReviewTable document={document} />;
}
