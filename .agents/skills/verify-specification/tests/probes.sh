#!/usr/bin/env bash
# Drives the consumers' probes through `verify build`, for the [pr] suite and for features/dbset-read-extensions.md's
# recipe. It builds each consumer from a tree with #11's DbSet extensions (HEAD) and from one without them (main at
# 9fb6d84, before #11), first with the probe files as the templates have them, then with CRLF endings, which
# .editorconfig asks for. Every probe must get the codes it names, the DbSet probes must appear only with the extensions,
# and CRLF must change no probe line. Since #47, HEAD's library is OpenSpecification and 9fb6d84's is
# Ardalis.Specification, so the builds also prove that each consumer's copy of the templates names its tree's library.
# It needs a .NET SDK, and no database.
# Usage: probes.sh [<helper>], where the helper defaults to ../bin/verify; prints each build's probe lines and one line
# per check, and exits 1 on a failure.
set -uo pipefail

HERE=$(cd "$(dirname "$(realpath "${BASH_SOURCE[0]}")")" && pwd)
V=${1:-$HERE/../bin/verify}
WITHOUT=9fb6d84
# mktemp names, since $$ is the same in every sandboxed run.
SCRATCH=$(mktemp -d /tmp/verify-specification-probes.XXXXXX)
export VERIFY_SPECIFICATION_SCRATCH=$SCRATCH
trap 'rm -rf -- "$SCRATCH"' EXIT

failed=0
pass() { echo "ok: $1"; }
fail() { echo "FAIL: $1"; failed=1; }

# build <consumer> <case>: builds the consumer and sets lines to its probe lines, or fails the case.
build() {
    local out
    if out=$("$V" build "$1" 2>&1); then
        lines=$(grep '^probe ' <<< "$out" || true)
    else
        fail "$2 didn't build: $(grep -E 'error|build: exit' <<< "$out" | head -3)"
        return 1
    fi
}

for kind in consumer efcore-consumer; do
    for tree in HEAD "$WITHOUT"; do
        if [[ $tree == HEAD ]]; then has=with; else has=without; fi
        case="$kind from $tree, $has the DbSet extensions"
        T=$("$V" tree "$tree") || { fail "$case: no tree"; continue; }
        C=$("$V" "$kind" "$T" "probes-$kind-$has") || { fail "$case: no consumer"; continue; }
        build "$C" "$case" || continue
        lf=$lines
        for file in "$C"/*Probes.cs; do
            [[ -f $file ]] && sed -i 's/$/\r/' "$file"
        done
        build "$C" "$case, CRLF" || continue
        crlf=$lines
        [[ -n $lf ]] && sed "s/^/$kind $has: /" <<< "$lf"

        mismatched=$(grep -vE '^probe [^ ]+ \([^)]+\): got (.*); an annotated provider gives \1$' <<< "$lf")
        if [[ -z $mismatched ]]; then
            pass "$case: every probe got the codes it names"
        else
            fail "$case: probes that got other codes: $mismatched"
        fi

        dbset=$(grep -cE '^probe (efcore-)?dbset-' <<< "$lf")
        if [[ $has == with && $dbset -gt 0 ]] || [[ $has == without && $dbset -eq 0 ]]; then
            pass "$case: $dbset DbSet probes"
        else
            fail "$case: $dbset DbSet probes"
        fi

        if [[ $lf == "$crlf" ]]; then
            pass "$case: CRLF probe files print the same probe lines"
        else
            fail "$case: CRLF probe files print other probe lines: $(diff <(echo "$lf") <(echo "$crlf") | tr '\r' '^')"
        fi
    done
done
exit $failed
