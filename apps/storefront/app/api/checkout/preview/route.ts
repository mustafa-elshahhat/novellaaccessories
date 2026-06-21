import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { preview } from "@/lib/api/checkout";
import type { CheckoutPreviewRequest } from "@/lib/api/types";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const body = await readJson<CheckoutPreviewRequest>(request);
  return jsonResult(() => preview(body));
}
