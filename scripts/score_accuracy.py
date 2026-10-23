"""Score reviewed references against saved --accurate-batch results, locally.

Usage: python scripts/score_accuracy.py reference-review.csv hypotheses.json
Never substitutes another recognizer's output for a verified reference.
"""
import argparse
import csv
import json
from pathlib import Path

from benchmark_speech import words, word_errors


def score(references, hypotheses, minimum_cases=300, minimum_words=3000, maximum_wer=.03):
    predictions = {row['File']: row for row in hypotheses if 'File' in row}
    errors = total = cases = no_speech = false_insertions = 0
    for row in references:
        if row.get('human_verified', '').strip().casefold() != 'true':
            raise ValueError('Every included reference must be human_verified=true. Unreviewed predictions are not ground truth.')
        if row['file'] not in predictions:
            raise ValueError('Missing prediction for ' + row['file'])
        reference = row['verified_spoken_reference']
        hypothesis = predictions[row['file']]['Raw']
        count, n = word_errors(reference, hypothesis)
        errors += count
        total += n
        if n:
            cases += 1
        else:
            no_speech += 1
            false_insertions += bool(words(hypothesis))
    wer = errors / total if total else None
    sufficient = cases >= minimum_cases and total >= minimum_words
    return {'VerifiedSpeechCases': cases, 'ReferenceWords': total, 'WordErrors': errors,
            'WER': wer, 'VerifiedEmptyReferenceCases': no_speech,
            'NonemptyOutputsOnEmptyReferences': false_insertions,
            'MinimumSpeechCases': minimum_cases, 'MinimumReferenceWords': minimum_words,
            'MaximumWER': maximum_wer,
            'PassedAccuracyGate': sufficient and wer is not None and wer <= maximum_wer and false_insertions == 0,
            'Scope': 'Raw recognition with case/punctuation ignored. Intentional editing, critical entities, and insertion latency need separate checks.'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('references', type=Path)
    parser.add_argument('hypotheses', type=Path)
    args = parser.parse_args()
    with args.references.open(encoding='utf-8-sig', newline='') as source:
        references = list(csv.DictReader(source))
    try:
        result = score(references, json.loads(args.hypotheses.read_text(encoding='utf-8-sig')))
    except ValueError as exc:
        parser.exit(2, str(exc) + '\n')
    print(json.dumps(result, indent=2))
    raise SystemExit(0 if result['PassedAccuracyGate'] else 1)
