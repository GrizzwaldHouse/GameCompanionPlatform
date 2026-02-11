# Admin Controller Setup Guide

How to set yourself up as the admin/owner of Arcadia Tracker with full control over activation codes, feature management, and the admin panel.

---

## Prerequisites

- Windows machine with the Arcadia Tracker source code
- Visual Studio 2022+ (or Rider) with .NET 8.0 SDK
- The project built in **DEBUG** configuration (admin features are disabled in Release builds)

---

## Step 1: Set Environment Variables

Admin access is gated behind two environment variables that must be set **before** launching the app. This ensures admin capabilities are never accidentally active.

### Option A: Set permanently (recommended for your dev machine)

Open PowerShell as Administrator and run:

```powershell
[System.Environment]::SetEnvironmentVariable("ARCADIA_ADMIN_ENABLED", "true", "User")
[System.Environment]::SetEnvironmentVariable("ARCADIA_ADMIN_SCOPE", "star_rupture", "User")
```

> Use `"*"` for `ARCADIA_ADMIN_SCOPE` to get admin access across all games, or `"star_rupture"` for Star Rupture only.

### Option B: Set per-session (temporary)

In the terminal where you launch the app:

```powershell
$env:ARCADIA_ADMIN_ENABLED = "true"
$env:ARCADIA_ADMIN_SCOPE = "star_rupture"
```

### Option C: Set in Visual Studio launch profile

Edit `Properties/launchSettings.json` in the app project:

```json
{
  "profiles": {
    "ArcadiaTracker.App": {
      "commandName": "Project",
      "environmentVariables": {
        "ARCADIA_ADMIN_ENABLED": "true",
        "ARCADIA_ADMIN_SCOPE": "star_rupture"
      }
    }
  }
}
```

---

## Step 2: Build in DEBUG Mode

Admin capabilities are **only** injected when the app is compiled in DEBUG mode. The production safety check in `App.xaml.cs` passes `isProduction: false` only under `#if DEBUG`:

```csharp
#if DEBUG
    var isProduction = false;
#else
    var isProduction = true;
#endif
```

Build the project in Debug configuration:

```
Build > Configuration Manager > Active solution configuration: Debug
```

Or from command line:

```powershell
dotnet build -c Debug
```

---

## Step 3: Launch the App

When you launch the app with the environment variables set and a DEBUG build:

1. **On startup**, `AdminCapabilityProvider.TryInjectAdminCapabilitiesAsync()` runs automatically
2. It checks for the `ARCADIA_ADMIN_ENABLED` env var
3. If `"true"`, it grants you:
   - `admin.save.override` — full admin save override
   - `admin.capability.issue` — ability to issue capabilities to others
   - All 9 paid capabilities (save.modify, save.inspect, save.backup.manage, analytics.optimizer, analytics.compare, analytics.replay, alerts.milestones, ui.themes, export.pro)
4. All capabilities auto-expire after **8 hours** (re-granted on next launch)
5. The injection is recorded in the audit log

---

## Step 4: Access the Admin Panel

Once admin capabilities are active:

1. Look at the left sidebar navigation
2. You'll see all premium nav items appear (Save Editor, Save Inspector, Backup Manager)
3. At the bottom, the **Admin Panel** nav item (shield icon) appears
4. Click it to access the admin panel

---

## Step 5: Using the Admin Panel

### Generate Activation Codes

1. In the **Generate Activation Code** section, select a bundle from the dropdown:
   - **Pro Bundle** — Save Modifier + Inspector + Backup + Themes
   - **Save Modifier** — Save editing only
   - **Save Inspector** — Read-only save analytics
   - **Backup Manager** — Backup management
   - **Theme Customizer** — UI themes
   - **Efficiency Optimizer** — Analytics optimizer
   - **Milestones & Alerts** — Alert system
   - **Export Pro** — Advanced export
2. Click **Generate Code**
3. An `ARCA-XXXX-XXXX-XXXX-XXXX-XXXX` code appears
4. Click **Copy to Clipboard** to copy it

### Batch Generate Codes

1. In the **Batch Generate** section, enter a count (1-100)
2. Select the bundle type from the dropdown above
3. Click **Generate Batch**
4. All codes appear in the text area
5. Click **Copy All** to copy them

### Self-Activate Features

For your own machine, you don't need to manually enter codes:

1. Click **Activate Pro Bundle** to instantly activate Pro features
2. Click **Activate ALL Features** to instantly activate every paid feature
3. A status message confirms the activation
4. The sidebar updates to show all premium nav items

### View Audit Log

The audit log at the bottom shows the last 50 events:
- Code generations, redemptions, capability grants
- Tamper detection events
- Admin capability injections
- Click **Refresh** to reload the log

---

## Step 6: Distribute Codes to Customers

When a customer pays (via Cash App, Venmo, or Amazon gift card):

1. Open the Admin Panel
2. Generate a code for the appropriate bundle
3. Send the code to the customer
4. They enter it in the **Activate Feature** screen in their copy of the app
5. The code is one-time-use and machine-bound after activation

### Code Properties

- **Format**: `ARCA-XXXX-XXXX-XXXX-XXXX-XXXX`
- **One-time use**: Each code can only be redeemed once
- **Cross-machine generation**: You generate codes on your machine, customers redeem on theirs
- **HMAC-verified**: Codes are cryptographically signed, can't be guessed or forged
- **Bundle-scoped**: Each code unlocks a specific set of features

---

## Security Architecture Summary

| Layer | Mechanism |
|-------|-----------|
| Admin gating | `#if DEBUG` + environment variables |
| Capability signing | HMAC-SHA256 with machine-derived keys |
| Code verification | HMAC-SHA256 with HKDF-derived activation key |
| Local storage | AES-GCM encrypted capability store |
| Tamper detection | Integrity checksums on capability store |
| Audit trail | Append-only local audit log |
| Expiry | Admin caps: 8hr, Paid caps: permanent |

---

## Troubleshooting

### Admin Panel doesn't appear

1. Verify env vars are set: `echo $env:ARCADIA_ADMIN_ENABLED` should print `true`
2. Verify you're running a DEBUG build (check the title bar or output window)
3. Check the audit log file at `%LocalAppData%/ArcadiaTracker/entitlements/audit.log`

### Codes don't work on customer machines

Codes are designed to work on any machine. If a code fails:
1. Check it hasn't already been redeemed (one-time use)
2. Verify the code was copied correctly (case-insensitive, dashes optional)
3. Check the customer's `redeemed.json` file for previous redemptions

### Capabilities expired

Admin capabilities expire after 8 hours. Simply restart the app to re-inject them. Paid capabilities granted via activation codes are permanent.

### Reset everything

To reset the entitlement state on any machine, delete the entitlements directory:

```powershell
Remove-Item -Recurse "$env:LOCALAPPDATA\ArcadiaTracker\entitlements"
```

This clears all capabilities, redemption records, consent records, and audit logs.
