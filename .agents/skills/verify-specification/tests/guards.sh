#!/usr/bin/env bash
# Locks the verify helper's guards. Cleanup, and each command that writes into a folder, refuses a decoy outside the
# scratch root, a path that climbs out with .., a folder inside a scratch item, and a link out of the scratch root,
# while cleanup of an item the helper made still succeeds. The [pr] suite in skillflow.toml runs it.
# Usage: guards.sh [<helper>], where the helper defaults to ../bin/verify; prints one line per case, exits 1 on a failure.
set -uo pipefail

HERE=$(cd "$(dirname "$(realpath "${BASH_SOURCE[0]}")")" && pwd)
V=${1:-$HERE/../bin/verify}
# mktemp names, since $$ is the same in every sandboxed run.
SCRATCH=$(mktemp -d /tmp/verify-specification-guards.XXXXXX)
export VERIFY_SPECIFICATION_SCRATCH=$SCRATCH
ESCAPE=$SCRATCH-escape
DECOYS=()
trap 'rm -rf -- "$SCRATCH" "$ESCAPE" "${DECOYS[@]}"' EXIT

failed=0
pass() { echo "ok: $1"; }
fail() { echo "FAIL: $1"; failed=1; }

# A decoy carries the helper's marker and a project file, so only the path checks stand between it and the helper.
new_decoy() {
    local decoy
    decoy=$(mktemp -d /tmp/verify-specification-decoy.XXXXXX)
    echo keep > "$decoy/keep"
    echo decoy > "$decoy/.verify-made"
    echo "<Project" > "$decoy/Consumer.csproj"
    DECOYS+=("$decoy")
}
snapshot() { (cd "$1" 2>/dev/null && find . -printf '%p %s\n' | sort && cat keep); }

# refuses <case> <helper arguments>: expects the helper to refuse, and the case's own decoy to stay as it was. In the
# arguments, @DECOY@ names that decoy, @DECOYNAME@ its name in /tmp, and @LINK@ a link to it in the scratch root.
refuses() {
    local case=$1 decoy link before out status arg
    local -a args=()
    shift
    new_decoy
    decoy=${DECOYS[-1]}
    link=$SCRATCH/consumers/link-${#DECOYS[@]}
    ln -s "$decoy" "$link"
    before=$(snapshot "$decoy")
    for arg; do
        arg=${arg//@DECOYNAME@/$(basename "$decoy")}
        arg=${arg//@DECOY@/$decoy}
        args+=("${arg//@LINK@/$link}")
    done
    out=$("$V" "${args[@]}" 2>&1) && status=0 || status=$?
    rm -f -- "$link"
    if [[ $status -eq 0 || $out != *refused* ]]; then
        fail "$case: exit $status, output: ${out//$'\n'/ | }"
    elif [[ $(snapshot "$decoy") != "$before" ]]; then
        fail "$case: the decoy changed"
    else
        pass "$case"
    fi
}

TREE=$("$V" tree HEAD)
if [[ -n $TREE && -d $TREE/src ]]; then pass "tree exports HEAD to $TREE"; else echo "FAIL: verify tree HEAD made no tree"; exit 1; fi

refuses "cleanup of a decoy outside the scratch root" cleanup @DECOY@
refuses "cleanup of a path that climbs out with .." cleanup "$SCRATCH/trees/../../@DECOYNAME@"
refuses "cleanup of a link out of the scratch root" cleanup @LINK@
refuses "cleanup of a folder inside a scratch item" cleanup "$TREE/src"
[[ -d $TREE/src ]] || fail "cleanup removed $TREE/src"
refuses "consumer from a decoy tree" consumer @DECOY@ decoy
refuses "consumer from a folder inside a tree" consumer "$TREE/src" inside
refuses "consumer with a name that climbs out" consumer "$TREE" "../../$(basename "$ESCAPE")"
[[ ! -e $ESCAPE ]] || fail "consumer wrote $ESCAPE"
refuses "build of a decoy consumer" build @DECOY@
refuses "build through a link out of the scratch root" build @LINK@
refuses "build of a folder inside a scratch item" build "$TREE/src"
refuses "run of a decoy consumer" run @DECOY@
refuses "warnings of a decoy project" warnings @DECOY@/Consumer.csproj
refuses "warnings of a project that climbs out with .." warnings "$TREE/../../../@DECOYNAME@/Consumer.csproj"
refuses "benchmark of a decoy tree" benchmark @DECOY@ "$SCRATCH" IncludeStep

CONSUMER=$("$V" consumer "$TREE" guards)
if [[ -n $CONSUMER && -f $CONSUMER/Consumer.csproj ]]; then pass "consumer makes $CONSUMER"; else fail "consumer made no project from $TREE"; fi
if [[ -n $CONSUMER ]] && "$V" cleanup "$CONSUMER" >/dev/null && [[ ! -e $CONSUMER ]]; then
    pass "cleanup removes the consumer it made"
else
    fail "cleanup didn't remove the consumer '$CONSUMER'"
fi

new_decoy
ln -s "${DECOYS[-1]}" "$SCRATCH/consumers/link"
before=$(snapshot "${DECOYS[-1]}")
out=$("$V" cleanup 2>&1) && status=0 || status=$?
if [[ $status -ne 0 && $out == *refused* && ! -e $TREE && $(snapshot "${DECOYS[-1]}") == "$before" ]]; then
    pass "cleanup of everything removes the tree and refuses the link"
else
    fail "cleanup of everything: exit $status, tree left: $([[ -e $TREE ]] && echo yes || echo no), output: ${out//$'\n'/ | }"
fi

# A tree older than a day goes when the next folder is made, so a consumer from it is refused rather than left
# pointing at nothing.
TREE=$("$V" tree HEAD)
touch -d '2 days ago' "$TREE/.verify-made"
out=$("$V" consumer "$TREE" aged 2>&1) && status=0 || status=$?
if [[ $status -ne 0 && $out == *refused* && ! -e $TREE && ! -e $SCRATCH/consumers/aged ]]; then
    pass "consumer from a tree the day-old cleanup removed"
else
    fail "consumer from a day-old tree: exit $status, consumer made: $([[ -e $SCRATCH/consumers/aged ]] && echo yes || echo no), output: ${out//$'\n'/ | }"
fi

exit $failed
