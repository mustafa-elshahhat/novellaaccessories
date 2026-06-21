import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { createOrder } from "@/lib/api/orders";
import type { CreateOrderRequest } from "@/lib/api/types";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const body = await readJson<CreateOrderRequest>(request);
  return jsonResult(() => createOrder(body));
}
