import { NextRequest, NextResponse } from "next/server";

export function middleware(request: NextRequest) {
  if (!request.cookies.get("sistemas-precios-session")?.value) {
    return NextResponse.redirect(new URL("/login", request.url));
  }
  // Presence is only an early redirect. The API verifies the actual cookie and live user permissions.
  const response = NextResponse.next();
  response.headers.set("Cache-Control", "no-store");
  return response;
}
export const config = { matcher: ["/", "/suppliers/:path*", "/documents/:path*", "/comparisons/:path*", "/history/:path*", "/team/:path*", "/account/:path*"] };
