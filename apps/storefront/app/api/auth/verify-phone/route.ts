import { NextResponse, type NextRequest } from "next/server";
import { csrfGuard, errorResponse, readJson } from "@/lib/api/bff";
import { verifyPhone, type VerifyPhoneBody } from "@/lib/api/auth";
import { setAuthCookie } from "@/lib/api/cookies";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  try {
    const body = await readJson<VerifyPhoneBody>(request);
    const res = await verifyPhone(body);
    await setAuthCookie(res.token);
    return NextResponse.json({ customer: res.customer });
  } catch (error) {
    return errorResponse(error);
  }
}
