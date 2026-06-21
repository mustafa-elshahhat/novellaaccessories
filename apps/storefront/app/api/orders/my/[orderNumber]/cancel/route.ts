import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { cancelOrder } from "@/lib/api/orders";

type Context = { params: Promise<{ orderNumber: string }> };

export async function POST(request: NextRequest, { params }: Context) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const { orderNumber } = await params;
  const body = await readJson<{ reason?: string | null }>(request);
  return jsonResult(() => cancelOrder(orderNumber, body.reason ?? null));
}
