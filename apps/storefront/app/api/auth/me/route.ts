import { jsonResult } from "@/lib/api/bff";
import { me } from "@/lib/api/auth";

export async function GET() {
  // On 401, jsonResult clears the auth cookie and returns a sanitized 401.
  return jsonResult(async () => {
    const res = await me();
    return { customer: res.customer };
  });
}
