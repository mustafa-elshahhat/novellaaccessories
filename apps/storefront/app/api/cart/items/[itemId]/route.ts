import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { updateItem, removeItem } from "@/lib/api/cart";

type Context = { params: Promise<{ itemId: string }> };

export async function PATCH(request: NextRequest, { params }: Context) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const { itemId } = await params;
  const body = await readJson<{ quantity: number }>(request);
  return jsonResult(() => updateItem(itemId, body.quantity));
}

export async function DELETE(request: NextRequest, { params }: Context) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const { itemId } = await params;
  return jsonResult(() => removeItem(itemId));
}
