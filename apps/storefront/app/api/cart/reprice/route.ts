import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult } from "@/lib/api/bff";
import { reprice } from "@/lib/api/cart";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  return jsonResult(() => reprice());
}
