import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders: Record<string, string> = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

function adminHeaders(serviceRole: string, withJsonBody: boolean): Record<string, string> {
  const h: Record<string, string> = {
    Authorization: `Bearer ${serviceRole}`,
    apikey: serviceRole,
  };
  if (withJsonBody) {
    h["Content-Type"] = "application/json";
  }
  return h;
}

type AdminCaller = { userId: string };

function normalizeUrl(value: string | undefined): string | null {
  if (!value) {
    return null;
  }

  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }

  const withoutTrailingSlash = trimmed.replace(/\/+$/, "");
  try {
    const parsed = new URL(withoutTrailingSlash);
    const lowerPath = parsed.pathname.toLowerCase();
    const functionsIndex = lowerPath.indexOf("/functions/v1");
    const authIndex = lowerPath.indexOf("/auth/v1");
    let keepPath = parsed.pathname.replace(/\/+$/, "");
    if (functionsIndex >= 0) {
      keepPath = parsed.pathname.slice(0, functionsIndex).replace(/\/+$/, "");
    } else if (authIndex >= 0) {
      keepPath = parsed.pathname.slice(0, authIndex).replace(/\/+$/, "");
    }

    return keepPath ? `${parsed.origin}${keepPath}` : parsed.origin;
  } catch {
    const withoutFunctionsSuffix = withoutTrailingSlash.replace(/\/functions\/v1(?:\/.*)?$/i, "");
    const withoutAuthSuffix = withoutFunctionsSuffix.replace(/\/auth\/v1(?:\/.*)?$/i, "");
    return withoutAuthSuffix;
  }
}

function getSupabaseUrlCandidates(requestOrigin?: string): string[] {
  const primary = normalizeUrl(Deno.env.get("SUPABASE_URL"));
  const publicUrl = normalizeUrl(Deno.env.get("SUPABASE_PUBLIC_URL"));
  const requestBase = normalizeUrl(requestOrigin);

  const unique: string[] = [];
  if (requestBase) {
    unique.push(requestBase);
  }
  if (primary) {
    unique.push(primary);
  }
  if (publicUrl && !unique.includes(publicUrl)) {
    unique.push(publicUrl);
  }

  // In local CLI runs the internal host can be "kong". If function runtime is outside
  // docker network, DNS for "kong" fails, so prefer public URL first in that case.
  if (unique.length > 1) {
    try {
      const host = new URL(unique[0]).hostname.toLowerCase();
      if (host === "kong") {
        return [unique[1], unique[0]];
      }
    } catch {
      // keep original order on invalid URL
    }
  }

  return unique;
}

function getGatewayOrigin(req: Request): string {
  const forwardedHost = req.headers.get("x-forwarded-host") ?? req.headers.get("host");
  const forwardedProto = req.headers.get("x-forwarded-proto") ?? "http";
  if (forwardedHost) {
    return `${forwardedProto}://${forwardedHost}`;
  }

  return new URL(req.url).origin;
}

/** Returns caller metadata if admin, otherwise an error Response. */
async function requireAdmin(req: Request): Promise<AdminCaller | Response> {
  const authHeader = req.headers.get("Authorization");
  if (!authHeader?.startsWith("Bearer ")) {
    return json({ error: "Missing bearer token." }, 401);
  }
  const token = authHeader.slice("Bearer ".length).trim();
  if (!token) {
    return json({ error: "Empty bearer token." }, 401);
  }

  const requestOrigin = getGatewayOrigin(req);
  const supabaseUrlCandidates = getSupabaseUrlCandidates(requestOrigin);
  const anonKey = Deno.env.get("SUPABASE_ANON_KEY");
  if (supabaseUrlCandidates.length === 0 || !anonKey) {
    return json({ error: "Server misconfigured." }, 500);
  }

  let user: Awaited<ReturnType<ReturnType<typeof createClient>["auth"]["getUser"]>>["data"]["user"] | null = null;
  let authErrorMessage: string | null = null;

  for (const supabaseUrl of supabaseUrlCandidates) {
    try {
      const supabase = createClient(supabaseUrl, anonKey);
      const { data, error } = await supabase.auth.getUser(token);
      if (!error && data.user) {
        user = data.user;
        break;
      }

      authErrorMessage = error?.message ?? "Invalid session.";
      console.error(`requireAdmin failed via ${supabaseUrl}: ${authErrorMessage}`);
    } catch (e) {
      authErrorMessage = e instanceof Error ? e.message : String(e);
      console.error(`requireAdmin exception via ${supabaseUrl}: ${authErrorMessage}`);
    }
  }

  if (!user) {
    return json({ error: authErrorMessage ?? "Invalid session." }, 401);
  }

  const role = user.app_metadata?.["role"];
  if (typeof role !== "string" || role.toLowerCase() !== "admin") {
    return json({ error: "Forbidden." }, 403);
  }

  if (!user.id) {
    return json({ error: "Invalid session: missing user id." }, 401);
  }

  return { userId: user.id };
}

function passthroughResponse(res: Response): Response {
  const headers: Record<string, string> = { ...corsHeaders };
  const ct = res.headers.get("Content-Type");
  if (ct) {
    headers["Content-Type"] = ct;
  } else if (res.status !== 204) {
    headers["Content-Type"] = "application/json";
  }
  return new Response(res.body, { status: res.status, headers });
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    return json({ error: "Method not allowed." }, 405);
  }

  const adminCaller = await requireAdmin(req);
  if (adminCaller instanceof Response) {
    return adminCaller;
  }

  const serviceRole = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  const requestOrigin = getGatewayOrigin(req);
  const supabaseUrlCandidates = getSupabaseUrlCandidates(requestOrigin);
  if (!serviceRole || supabaseUrlCandidates.length === 0) {
    return json({ error: "Server misconfigured." }, 500);
  }

  let body: Record<string, unknown>;
  try {
    body = await req.json();
  } catch {
    return json({ error: "Invalid JSON body." }, 400);
  }

  const action = typeof body.action === "string" ? body.action.toLowerCase() : "";

  const callAuthAdmin = async (
    path: string,
    init: RequestInit,
  ): Promise<Response> => {
    let lastError: unknown = null;
    for (const baseUrl of supabaseUrlCandidates) {
      try {
        const targetUrl = `${baseUrl}/auth/v1${path}`;
        const response = await fetch(targetUrl, init);
        if (response.status >= 400) {
          console.error(`auth admin call status ${response.status} via ${targetUrl}`);
        }
        return response;
      } catch (e) {
        lastError = e;
        const message = e instanceof Error ? e.message : String(e);
        console.error(`auth admin call exception via ${baseUrl}: ${message}`);
      }
    }

    throw lastError ?? new Error("Unable to reach Auth API.");
  };

  try {
    if (action === "list") {
      const res = await callAuthAdmin("/admin/users?page=1&per_page=1000", {
        headers: adminHeaders(serviceRole, false),
      });
      return passthroughResponse(res);
    }

    if (action === "create") {
      const email = typeof body.email === "string" ? body.email.trim() : "";
      const password = typeof body.password === "string" ? body.password : "";
      const fullName = typeof body.fullName === "string" ? body.fullName.trim() : "";
      if (!email || !password) {
        return json({ error: "email and password required." }, 400);
      }
      const res = await callAuthAdmin("/admin/users", {
        method: "POST",
        headers: adminHeaders(serviceRole, true),
        body: JSON.stringify({
          email,
          password,
          email_confirm: true,
          app_metadata: { role: "user" },
          user_metadata: fullName ? { full_name: fullName } : {},
        }),
      });
      return passthroughResponse(res);
    }

    if (action === "update") {
      const userId = typeof body.userId === "string" ? body.userId.trim() : "";
      if (!userId) {
        return json({ error: "userId required." }, 400);
      }
      if (userId !== adminCaller.userId) {
        return json({ error: "Forbidden: admin can update only own profile." }, 403);
      }
      const newEmail = typeof body.email === "string" ? body.email.trim() : "";
      const newPassword = typeof body.password === "string" ? body.password : "";
      const fullName = typeof body.fullName === "string" ? body.fullName.trim() : "";
      if (!newEmail && !newPassword && !fullName) {
        return json({ error: "email or password or fullName required." }, 400);
      }
      const payload: Record<string, unknown> = {};
      if (newEmail) {
        payload.email = newEmail;
        payload.email_confirm = true;
      }
      if (newPassword) {
        payload.password = newPassword;
      }
      if (fullName) {
        payload.user_metadata = { full_name: fullName };
      }
      const res = await callAuthAdmin(`/admin/users/${encodeURIComponent(userId)}`, {
        method: "PUT",
        headers: adminHeaders(serviceRole, true),
        body: JSON.stringify(payload),
      });
      return passthroughResponse(res);
    }

    if (action === "delete") {
      const userId = typeof body.userId === "string" ? body.userId.trim() : "";
      if (!userId) {
        return json({ error: "userId required." }, 400);
      }
      if (userId !== adminCaller.userId) {
        return json({ error: "Forbidden: admin can delete only own profile." }, 403);
      }
      const res = await callAuthAdmin(`/admin/users/${encodeURIComponent(userId)}`, {
        method: "DELETE",
        headers: adminHeaders(serviceRole, false),
      });
      return passthroughResponse(res);
    }

    return json({ error: "Unknown action." }, 400);
  } catch (e) {
    const message = e instanceof Error ? e.message : String(e);
    return json({ error: message }, 500);
  }
});
