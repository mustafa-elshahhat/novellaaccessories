import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { addItem } from "@/lib/api/cart";
import type { AddCartItemRequest } from "@/lib/api/types";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const body = await readJson<AddCartItemRequest>(request);
  return jsonResult(() => addItem(body));
}
