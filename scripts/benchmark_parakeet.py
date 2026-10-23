"""Measure an already-downloaded local Parakeet model. Never downloads models."""
import glob
import json
import os
from pathlib import Path
import sys
import time

os.environ['HF_HUB_OFFLINE'] = '1'
bins = [folder for root in sys.path for folder in glob.glob(os.path.join(root, 'nvidia', '*', 'bin'))]
os.environ['PATH'] = os.pathsep.join(bins + [os.environ.get('PATH', '')])

import onnx_asr

model_path = Path.home() / '.cache/huggingface/hub/models--istupakov--parakeet-tdt-0.6b-v2-onnx/snapshots/0bbb45a3365852604aef28b538a8f066f4ccaa85'
started = time.perf_counter()
model = onnx_asr.load_model('nemo-parakeet-tdt-0.6b-v2', str(model_path), providers=['CUDAExecutionProvider', 'CPUExecutionProvider'])
print(json.dumps({'load_ms': round((time.perf_counter()-started)*1000), 'cuda_bin_directories': len(bins)}), flush=True)
for path in (Path(__file__).parents[1] / 'output/streaming-fixtures').glob('user1_*.wav'):
    for attempt in range(2):
        started = time.perf_counter()
        text = model.recognize(str(path))
        print(json.dumps({'fixture': path.name, 'attempt': attempt, 'inference_ms': round((time.perf_counter()-started)*1000), 'text': text}), flush=True)
