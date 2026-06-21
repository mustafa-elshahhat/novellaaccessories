import { jsonResult } from "@/lib/api/bff";
import { myOrders } from "@/lib/api/orders";

export async function GET() {
  return jsonResult(() => myOrders());
}
