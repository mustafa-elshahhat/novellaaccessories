import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { forgotPasswordRequest, type ForgotPasswordBody } from "@/lib/api/auth";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const body = await readJson<ForgotPasswordBody>(request);
  return jsonResult(() => forgotPasswordRequest(body));
}
