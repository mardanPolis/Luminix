#!/usr/bin/env bash
# ============================================
# Luminix Build Script (Linux/macOS)
# Always builds Release, for all target OSes:
#   win-x64, linux-x64, osx-x64
# ============================================
# Usage:
#   ./build.sh
# ============================================
 
set -e
 
PROJECT="Luminix/Luminix.csproj"
CONFIG="Release"
RIDS=("win-x64" "linux-x64" "osx-x64")
 
echo ""
echo "=============================================="
echo "Building Luminix (Release, all platforms)"
echo "  Project: $PROJECT"
echo "=============================================="
echo ""
 
# --- Restore once (pulls native assets for all listed RIDs) ---
dotnet restore "$PROJECT"
 
for RID in "${RIDS[@]}"; do
    echo ""
    echo "--- Building $RID ---"
    dotnet build "$PROJECT" -c "$CONFIG" -r "$RID" --no-restore
done
 
echo ""
echo "=============================================="
echo "All Release builds succeeded: ${RIDS[*]}"
echo "=============================================="
 
