import { type NextRequest } from "next/server";
import { csrfGuard, jsonResult, readJson } from "@/lib/api/bff";
import { changePhoneVerify, type ChangePhoneVerifyBody } from "@/lib/api/auth";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  const body = await readJson<ChangePhoneVerifyBody>(request);
  return jsonResult(() => changePhoneVerify(body));
}
