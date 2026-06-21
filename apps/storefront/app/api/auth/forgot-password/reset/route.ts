import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { forgotPasswordReset, type ResetPasswordBody } from "@/lib/api/auth";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const body = await readJson<ResetPasswordBody>(request);
  return jsonResult(() => forgotPasswordReset(body));
}
