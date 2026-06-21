import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult } from "@/lib/api/bff";
import { getCart, clearCart } from "@/lib/api/cart";

export async function GET() {
  return jsonResult(() => getCart());
}

export async function DELETE(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  return jsonResult(() => clearCart());
}
