from test_app import run


def test_live_grammar_bounds_release_and_never_uses_a_stale_correction():
    result = run('--live-grammar-smoke')
    assert result.returncode == 0, result.stderr
    assert result.stdout == 'live-grammar-ok|bounded-release|exact-cache|no-stale-edit|single-request|silence-empty|failure-fallback'
