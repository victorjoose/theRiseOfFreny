#!/usr/bin/env bash
# Configura este clone para trabalhar em equipe (rode uma vez por clone), no macOS/Linux:
#   bash Tools/git/setup.sh
# No Windows use Tools\git\setup.ps1. Detalhes em CONTRIBUTING.md.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

git lfs version >/dev/null || { echo "Git LFS nao encontrado: https://git-lfs.com" >&2; exit 1; }
git lfs install >/dev/null
git config lfs.locksverify true

version=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt | tr -d '\r')
merge=""
for c in \
  "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/Tools/UnityYAMLMerge" \
  "$HOME/Unity/Hub/Editor/$version/Editor/Data/Tools/UnityYAMLMerge"; do
  [ -x "$c" ] && { merge="$c"; break; }
done
if [ -z "$merge" ]; then
  echo "Aviso: UnityYAMLMerge da Unity $version nao encontrado; Smart Merge nao configurado." >&2
else
  git config merge.unityyamlmerge.name 'Unity SmartMerge (UnityYAMLMerge)'
  git config merge.unityyamlmerge.driver "'$merge' merge -h -p --force --fallback none %O %B %A %A"
  git config merge.unityyamlmerge.recursive binary
  echo "Smart Merge: $merge"
fi
echo "Pronto. Unity esperada: $version"
