#!/bin/bash
set -euo pipefail

# Only run in remote (Claude Code on the web) environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

# Suppress .NET telemetry and first-run experience
echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1' >> "$CLAUDE_ENV_FILE"
echo 'export DOTNET_NOLOGO=1' >> "$CLAUDE_ENV_FILE"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Install .NET 8.0 SDK if not already present
if ! command -v dotnet &>/dev/null; then
  echo "Installing .NET 8.0 SDK..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
  rm -f /tmp/dotnet-install.sh

  # Persist dotnet on PATH for the session
  echo 'export DOTNET_ROOT="/usr/share/dotnet"' >> "$CLAUDE_ENV_FILE"
  echo 'export PATH="/usr/share/dotnet:$PATH"' >> "$CLAUDE_ENV_FILE"
  export DOTNET_ROOT="/usr/share/dotnet"
  export PATH="/usr/share/dotnet:$PATH"
fi

echo "Restoring NuGet packages..."

# Restore NuGet packages for test projects that build on Linux
# (skip WPF/windows-only projects that cannot restore on Linux)
dotnet restore "$CLAUDE_PROJECT_DIR/tests/GameCompanion.Core.Tests/GameCompanion.Core.Tests.csproj" --verbosity quiet || true
dotnet restore "$CLAUDE_PROJECT_DIR/tests/GameCompanion.Engine.Entitlements.Tests/GameCompanion.Engine.Entitlements.Tests.csproj" --verbosity quiet || true

echo "Session startup complete. .NET 8.0 SDK and test dependencies ready."
