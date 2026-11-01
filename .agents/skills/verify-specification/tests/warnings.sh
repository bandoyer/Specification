#!/usr/bin/env bash
# Checks that `verify warnings` counts every warning form a build writes. First, a real `dotnet build` of the two
# projects in msbuild-warnings/, whose targets write a warning with a code and one without, in MSBuild's own form.
# Then a stand-in dotnet on PATH writes the forms no small project yields: a compiler warning with its position and
# project, one repeated for another framework, a NuGet warning with neither position nor framework, and an MSBuild
# warning with no file. The [pr] suite in skillflow.toml runs it, and so does the recipe for `verify warnings`.
# Usage: warnings.sh [<helper>], where the helper defaults to ../bin/verify; exits 1 when a report differs.
set -uo pipefail

HERE=$(cd "$(dirname "$(realpath "${BASH_SOURCE[0]}")")" && pwd)
V=${1:-$HERE/../bin/verify}
SCRATCH=$(mktemp -d /tmp/verify-specification-warnings.XXXXXX)
export VERIFY_SPECIFICATION_SCRATCH=$SCRATCH
STANDIN=$(mktemp -d /tmp/verify-specification-standin.XXXXXX)
trap 'rm -rf -- "$SCRATCH" "$STANDIN"' EXIT

TREE=$("$V" tree HEAD) || { echo "FAIL: verify tree HEAD"; exit 1; }
SHA=$(basename "$TREE")
L=$("$V" library "$TREE") || { echo "FAIL: verify library $TREE"; exit 1; }
failed=0

# report <case> <expected> <helper arguments>: runs the helper and compares its whole report.
report() {
    local case=$1 expected=$2 actual
    shift 2
    actual=$("$V" warnings "$@" 2>&1)
    if [[ $actual == "$expected" ]]; then
        echo "ok: $case"
    else
        echo "FAIL: $case; verify warnings reported:"
        printf '%s\n' "$actual"
        echo "expected:"
        printf '%s\n' "$expected"
        failed=1
    fi
}

# Each project in a folder of its own inside the tree, so the two builds don't share an obj folder.
for project in WarningForms UncodedWarning; do
    mkdir "$TREE/$project"
    cp "$HERE/msbuild-warnings/$project.csproj" "$TREE/$project/"
done

report "a real build's warnings with and without a code are both counted" \
"warning trees/$SHA/WarningForms/WarningForms.csproj:7 VERIFY1001 Coded warning from an actual MSBuild target
warning trees/$SHA/WarningForms/WarningForms.csproj:8 (no code) Warning without an optional Code from an actual MSBuild target
warnings: 2
build: exit 0" "$TREE/WarningForms/WarningForms.csproj"

report "a real build's only warning, which has no code, is counted" \
"warning trees/$SHA/UncodedWarning/UncodedWarning.csproj:7 (no code) An actual MSBuild warning whose optional Code is absent
warnings: 1
build: exit 0" "$TREE/UncodedWarning/UncodedWarning.csproj"

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
MSBUILD : warning MSB4011: Imported twice
LOG
EOF
chmod +x "$STANDIN/dotnet"

PATH=$STANDIN:$PATH report "the NuGet, repeated compiler and MSBuild forms are each counted once" \
"warning trees/$SHA/src/$L.EntityFramework6/$L.EntityFramework6.csproj NU1902 Package 'Example' 1.0.0 has a known moderate severity vulnerability.
warning trees/$SHA/src/$L.EntityFramework6/Evaluators/OrderEvaluator.cs:20 CS8600 Converting null literal or possible null value to non-nullable type. [net8.0,net472]
warning MSBUILD MSB4011 Imported twice
warnings: 3
build: exit 0" "$TREE/src/$L.EntityFramework6/$L.EntityFramework6.csproj"

exit $failed
