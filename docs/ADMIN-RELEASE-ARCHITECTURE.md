# Release-Safe Admin Control Architecture

## Overview

This document describes the admin control system that works in both DEBUG and RELEASE builds. Admin access is explicitly activated, time-bound, cryptographically signed, scope-limited, and fully audited.

---

## Admin Capability Model

### Capability Namespace

| Action | Description |
|--------|-------------|
| `admin.save.override` | Full admin save modification override |
| `admin.capability.issue` | Ability to issue capabilities to others |

Admin capabilities are **separate** from paid capabilities. They never piggyback on paid entitlements.

### Properties

- **Signed**: HMAC-SHA256 with machine-derived signing key
- **Time-bound**: Always expire (8h for DEBUG, configurable for tokens, 4h for break-glass, max 30d)
- **Scope-limited**: Per game (`star_rupture`) or global (`*`)
- **Revocable**: Token file can be deleted; capabilities can be individually revoked

---

## Activation Paths

### Path 1: Admin Token File (ALL BUILDS)

**Primary mechanism for production admin access.**

```
admin.token  (AES-GCM encrypted file in entitlements directory)
  └── Contains: { Id, Scope, IssuedAt, ExpiresAt, Nonce, Signature, Method }
      └── Signature: HMAC-SHA256(admin_key, canonical_string)
          └── admin_key: HKDF(AdminKeyMaterial, 32, "ArcadiaTracker.Admin.Signing.v1")
```

**Flow:**
1. Admin generates a token via the Admin Panel or a CLI tool
2. Token is signed with the admin key and encrypted with the machine encryption key
3. Token is saved to `%LocalAppData%/ArcadiaTracker/entitlements/admin.token`
4. On startup, `AdminCapabilityProvider.TryInjectAdminCapabilitiesAsync()` loads and validates the token
5. If valid, all admin + paid capabilities are injected with the token's remaining lifetime
6. Tamper detector verifies file integrity before decryption

**Security:**
- Token file is AES-GCM encrypted (confidentiality + integrity)
- HMAC-SHA256 signature prevents forgery
- Tamper detector checksums prevent file substitution
- Max lifetime: 30 days
- Each token has a random 128-bit nonce (prevents replay)

### Path 2: Environment Variables (DEBUG BUILDS ONLY)

**Development convenience, deprecated for production.**

```
ARCADIA_ADMIN_ENABLED=true
ARCADIA_ADMIN_SCOPE=star_rupture
```

- Only works when compiled in DEBUG configuration
- 8-hour capability lifetime
- Method logged as `DebugEnvironment`

### Path 3: Break-Glass Emergency Recovery (ALL BUILDS)

**For emergency access when the admin token has expired.**

**Challenge-Response Protocol:**
```
challenge = HMAC-SHA256(admin_key, machine_seed || date_string)[0..4]  → 8 hex chars
response  = HMAC-SHA256(break_glass_key, challenge || machine_seed)[0..4]  → 8 hex chars
            where break_glass_key = HKDF(admin_key, 32, "ArcadiaTracker.Admin.BreakGlass.v1")
```

**Flow:**
1. Admin Panel shows the daily challenge (changes every UTC day)
2. Admin computes the response using their admin tool or key material
3. If response matches, a 4-hour emergency admin token is issued
4. All attempts (success and failure) are logged to the audit trail

**Security:**
- Challenge changes daily (cannot be pre-computed for future dates)
- Response requires knowledge of the admin key material (embedded in binary)
- Short-lived token (4 hours) limits exposure
- Failed attempts are audited with `AuditOutcome.Denied`

---

## Key Derivation Hierarchy

```
machine_seed = SHA256(MachineName | UserName | "ArcadiaTracker")
    ├── signing_key   = HKDF(machine_seed, 32, "ArcadiaTracker.Capability.Signing.v1")
    ├── encryption_key = HKDF(machine_seed, 32, "ArcadiaTracker.Capability.Encryption.v1")
    └── (capability signing and store encryption)

admin_key_material = "ArcadiaTracker.Admin.Master.v1" (embedded constant)
    ├── admin_key     = HKDF(admin_key_material, 32, "ArcadiaTracker.Admin.Signing.v1")
    │   └── (admin token signing)
    └── break_glass   = HKDF(admin_key, 32, "ArcadiaTracker.Admin.BreakGlass.v1")
        └── (break-glass response computation)

activation_key_material = "ArcadiaTracker.Activation.Key.v1" (embedded constant)
    └── activation_key = HKDF(activation_key_material, 32, "ArcadiaTracker.Activation.HMAC.v1")
        └── (activation code HMAC)
```

**Note:** Admin key is derived from a constant (not machine-bound) so admin tokens can be conceptually generated on any machine. However, the token FILE is encrypted with the machine-bound encryption key, so a token file from one machine won't decrypt on another.

---

## Entitlements Directory Layout

```
%LocalAppData%/ArcadiaTracker/entitlements/
├── capabilities.dat    AES-GCM encrypted capability store
├── admin.token         AES-GCM encrypted admin token (NEW)
├── audit.log           Append-only NDJSON audit log
├── consent.json        Consent records
├── integrity.dat       SHA256 checksums for tamper detection
└── redeemed.json       Used activation codes
```

---

## Admin Panel Features

### Status Banner
- Shows: Active/Inactive/Expired status
- Displays: Scope, expiry time, activation method
- Revoke button (with confirmation dialog)

### Break-Glass Section
- Only visible when no valid admin token exists
- Shows daily challenge
- Response input with activation button
- Status feedback

### Activation Code Management
- Single code generation with bundle selection
- Batch generation (1-100 codes)
- Copy to clipboard

### Self-Activation
- Pro Bundle (one-click)
- ALL Features (one-click)

### Capability Inspector
- Lists all active capabilities with:
  - Action name
  - Game scope
  - Time remaining
  - Status indicator
- Refresh button

### System Diagnostics
- Machine fingerprint
- Admin token status/scope/expiry/method
- Store integrity check result
- Store size
- Total audit entries
- Last admin action timestamp

### Repair Tools
- **Purge Expired**: Removes expired/revoked capabilities from store
- **Verify Integrity**: Runs tamper detection on capability store
- **Export Audit Log**: Saves all audit entries to JSON file

### Audit Log Viewer
- Last 50 entries
- Color-coded outcomes
- Refresh button

---

## Audit Actions

| Action | When | Outcome |
|--------|------|---------|
| `admin.inject` | Admin capabilities injected | Success |
| `admin.token.save` | Admin token saved to disk | Success |
| `admin.token.revoke` | Admin token deleted | Success |
| `admin.token.tamper` | Token file integrity failure | TamperDetected |
| `admin.breakglass.failed` | Invalid break-glass response | Denied |
| `admin.revoke` | Admin access fully revoked | Success |
| `activation.grant` | Activation code redeemed | Success |
| `tamper_detection` | Store integrity failure | TamperDetected |

---

## DEBUG Admin Sunset Strategy

### Current State (v1.x)

- DEBUG builds: env var injection + admin token (both paths available)
- RELEASE builds: admin token only

### Migration Plan (v2.0)

1. **Phase 1** (Current): Both paths coexist. Admin token takes priority.
2. **Phase 2** (Pre-release): Admin generates a long-lived token (30 days) for their machine.
3. **Phase 3** (Post-release): Environment variable path remains in DEBUG builds for development convenience, but all production admin access uses tokens exclusively.

### When to Disable DEBUG Admin

- DEBUG admin should NEVER appear in distributed binaries
- The `#if DEBUG` guard ensures this automatically
- Production builds (`dotnet publish -c Release`) will never execute the env var path

### Production Checklist

Before distributing a release build:

- [ ] Build configuration is set to `Release`
- [ ] Verify `isProduction = true` is set in Release builds
- [ ] Generate and save a 30-day admin token on your machine
- [ ] Test admin panel access via token authentication
- [ ] Verify break-glass challenge/response works
- [ ] Verify non-admin users cannot see admin panel
- [ ] Verify activation codes work on customer machines
- [ ] Review audit log for any anomalies

---

## Threat Model

| Threat | Mitigation |
|--------|------------|
| Forged admin token | HMAC-SHA256 signature verification |
| Token file theft | AES-GCM encryption (machine-bound key) |
| Token file tampering | Tamper detector checksums + AES-GCM auth tag |
| Brute-force break-glass | Only 2^32 possible responses, but requires binary reverse-engineering to extract key material; daily rotation limits attempts |
| Privilege escalation (paid→admin) | Separate capability namespaces; admin capabilities never issued via activation codes |
| Non-discoverable admin UI | Admin panel nav item only appears after capability check passes |
| Indefinite admin access | Max 30-day token lifetime; break-glass limited to 4 hours |
| Admin action deniability | Append-only audit log records all admin actions |

### Residual Risks

1. **Key material in binary**: Admin key material is embedded in the binary. An attacker with the binary could extract it and forge admin tokens. Mitigation: This is an accepted risk for a local-first desktop app with no server component.
2. **Machine-bound encryption**: Admin token files only work on the machine that created them. If the machine is compromised, the attacker already has full access.
3. **Break-glass daily window**: The break-glass challenge is valid for a full UTC day. Mitigation: Short token lifetime (4 hours) and audit logging.

---

## Files

### New
- `Models/AdminToken.cs` — Admin token model with activation methods
- `Interfaces/IAdminTokenService.cs` — Token management interface + diagnostics
- `Services/AdminTokenService.cs` — Token generation, validation, persistence, break-glass

### Modified
- `Services/AdminCapabilityProvider.cs` — Now supports token path + break-glass + revocation
- `Views/AdminPanelView.xaml/.cs` — Hardened with diagnostics, break-glass UI, capability inspector
- `App.xaml.cs` — Registers AdminTokenService

### Unchanged
- All paid feature enforcement (CapabilityValidator, EntitlementService, PluginLoader)
- All user-facing views (non-admin users see zero changes)
- Activation code system (works independently of admin tokens)
