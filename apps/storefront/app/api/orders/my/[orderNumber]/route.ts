import { jsonResult } from "@/lib/api/bff";
import { myOrder } from "@/lib/api/orders";

type Context = { params: Promise<{ orderNumber: string }> };

export async function GET(_request: Request, { params }: Context) {
  const { orderNumber } = await params;
  return jsonResult(() => myOrder(orderNumber));
}
