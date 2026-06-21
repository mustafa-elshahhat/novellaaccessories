import { NextResponse, type NextRequest } from "next/server";
import { csrfGuard, errorResponse, readJson } from "@/lib/api/bff";
import { login, type LoginBody } from "@/lib/api/auth";
import { setAuthCookie } from "@/lib/api/cookies";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  try {
    const body = await readJson<LoginBody>(request);
    const res = await login(body);
    await setAuthCookie(res.token);
    return NextResponse.json({ customer: res.customer });
  } catch (error) {
    return errorResponse(error);
  }
}
