import { NextResponse, type NextRequest } from "next/server";
import { csrfGuard } from "@/lib/api/bff";
import { logout } from "@/lib/api/auth";
import { clearAuthCookie } from "@/lib/api/cookies";

export async function POST(request: NextRequest) {
  const blocked = csrfGuard(request);
  if (blocked) return blocked;
  // Best-effort backend logout; always clear the local cookie regardless of the result.
  try {
    await logout();
  } catch {
    // ignore — local logout still proceeds
  }
  await clearAuthCookie();
  return NextResponse.json({ success: true });
}
