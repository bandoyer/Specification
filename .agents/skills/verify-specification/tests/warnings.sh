#!/usr/bin/env bash
# Checks that `verify warnings` counts every warning form a build writes: a compiler warning with its position and
# project, one repeated for another framework, a NuGet warning with neither position nor framework, a target's warning
# with a position and no project, a target's warning with no code, and an MSBuild warning with no file. A stand-in
# dotnet on PATH writes the log, so no real build runs. The [pr] suite in skillflow.toml runs it.
# Usage: warnings.sh [<helper>], where the helper defaults to ../bin/verify; exits 1 when the report differs.
set -uo pipefail

HERE=$(cd "$(dirname "$(realpath "${BASH_SOURCE[0]}")")" && pwd)
V=${1:-$HERE/../bin/verify}
SCRATCH=$(mktemp -d /tmp/verify-specification-warnings.XXXXXX)
export VERIFY_SPECIFICATION_SCRATCH=$SCRATCH
STANDIN=$(mktemp -d /tmp/verify-specification-standin.XXXXXX)
trap 'rm -rf -- "$SCRATCH" "$STANDIN"' EXIT

TREE=$("$V" tree HEAD) || { echo "FAIL: verify tree HEAD"; exit 1; }
PROJECT=$TREE/src/Ardalis.Specification.EntityFramework6/Ardalis.Specification.EntityFramework6.csproj

# `dotnet build <project> ...` prints these lines, as MSBuild would, and succeeds.
cat > "$STANDIN/dotnet" <<'EOF'
#!/usr/bin/env bash
project=$2
dir=$(dirname "$project")
cat <<LOG
  Determining projects to restore...
$project : warning NU1902: Package 'Example' 1.0.0 has a known moderate severity vulnerability. [$project]
$dir/Evaluators/OrderEvaluator.cs(20,13): warning CS8600: Converting null literal or possible null value to non-nullable type. [$project::TargetFramework=net8.0]
$dir/Evaluators/OrderEvaluator.cs(20,13): warning CS8600: Converting null literal or possible null value to non-nullable type. [$project::TargetFramework=net472]
$dir/Evaluators/OrderEvaluator.cs(20,13): warning CS8600: Converting null literal or possible null value to non-nullable type. [$project::TargetFramework=net8.0]
$project(12,5): warning NU9999: Emitted by a target
$project(13,5): warning : Emitted by a target without a code [$project]
MSBUILD : warning MSB4011: Imported twice
LOG
EOF
chmod +x "$STANDIN/dotnet"

SHA=$(basename "$TREE")
EXPECTED="warning trees/$SHA/src/Ardalis.Specification.EntityFramework6/Ardalis.Specification.EntityFramework6.csproj NU1902 Package 'Example' 1.0.0 has a known moderate severity vulnerability.
warning trees/$SHA/src/Ardalis.Specification.EntityFramework6/Evaluators/OrderEvaluator.cs:20 CS8600 Converting null literal or possible null value to non-nullable type. [net8.0,net472]
warning trees/$SHA/src/Ardalis.Specification.EntityFramework6/Ardalis.Specification.EntityFramework6.csproj:12 NU9999 Emitted by a target
warning trees/$SHA/src/Ardalis.Specification.EntityFramework6/Ardalis.Specification.EntityFramework6.csproj:13 (no code) Emitted by a target without a code
warning MSBUILD MSB4011 Imported twice
warnings: 5
build: exit 0"

ACTUAL=$(PATH=$STANDIN:$PATH "$V" warnings "$PROJECT" 2>&1)
if [[ $ACTUAL == "$EXPECTED" ]]; then
    echo "ok: verify warnings reports each of the 5 distinct warnings in every form"
else
    echo "FAIL: verify warnings reported:"
    printf '%s\n' "$ACTUAL"
    echo "expected:"
    printf '%s\n' "$EXPECTED"
    exit 1
fi
