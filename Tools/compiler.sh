#!/bin/sh
# LE COMPILATEUR DE CONTROLE. Le verificateur (verifier.py) devine ; celui-ci
# COMPILE vraiment les scripts du jeu avec le compilateur C#, contre les
# assemblies de reference d'Unity (telechargees une fois depuis NuGet, jamais
# versionnees). Il donne les memes erreurs qu'Unity (CS0119, CS1061...).
#
#     sh Tools/compiler.sh
#
# Il faut le SDK .NET (sur Ubuntu : apt-get install dotnet-sdk-8.0).
# Limite : ces references sont celles d'Unity 2021.3. Une API ajoutee dans
# Unity 6 (Rigidbody.linearVelocity...) y serait signalee a tort.
set -e
cd "$(dirname "$0")/compilateur"
REF="${FIEF_UNITY_REF:-$HOME/.cache/fief-unityref}"
if [ ! -d "$REF/lib/netstandard2.0" ]; then
  mkdir -p "$REF"
  curl -sS -L -o "$REF/m.nupkg" https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg
  (cd "$REF" && unzip -o -q m.nupkg)
fi
status=0
for defines in "" "ENABLE_INPUT_SYSTEM"; do
  out=$(dotnet build Controle.csproj -nologo -v q -p:RefDir="$REF" -p:ExtraDefines="$defines" 2>&1 || true)
  errs=$(printf "%s\n" "$out" | grep -E ": error " | sed 's#.*/Assets/#Assets/#; s# \[.*##' | sort -u)
  if [ -n "$errs" ]; then
    echo "ERREURS DE COMPILATION ${defines:+($defines)} :"
    echo "$errs"
    status=1
  fi
done
[ $status -eq 0 ] && echo "Compilation OK (Input System et Input classique)."
exit $status
