// tenant.js
// Utility to detect tenant ID from subdomain or fallback to query param for local dev

export function getTenantFromHost() {
  // Example tenant host: tenant.example.com
  try {
    // Check localStorage override first (for manual tenant entry)
    const stored = localStorage.getItem('tenant_override');
    if (stored) return stored;

    const host = window.location.hostname; // excludes port

    // If running on localhost, allow a query param ?tenant=<id> for convenience
    if (host === 'localhost' || host.endsWith('.localhost')) {
      const params = new URLSearchParams(window.location.search);
      const q = params.get('tenant');
      if (q) return q;
      return null;
    }

    const parts = host.split('.');
    if (parts.length >= 3) {
      // subdomain exists
      return parts[0];
    }
    return null;
  } catch (e) {
    return null;
  }
}

export function setTenantOverride(tenant) {
  if (tenant) {
    localStorage.setItem('tenant_override', tenant);
  } else {
    localStorage.removeItem('tenant_override');
  }
}

export function clearTenantOverride() {
  localStorage.removeItem('tenant_override');
}
