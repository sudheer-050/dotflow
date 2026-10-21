# Third-party notices

The Windows app integrates Vosk API 0.3.45 and vosk-model-small-en-us-0.15 by Alpha Cephei directly through the native C API. Both are distributed under Apache License 2.0.

Vosk API and license: https://github.com/alphacep/vosk-api/tree/v0.3.45
English model, source and model license: https://alphacephei.com/vosk/models
Apache License 2.0: https://www.apache.org/licenses/LICENSE-2.0

The Vosk Windows binary distribution includes the GCC runtime libraries libgcc_s_seh-1.dll, libstdc++-6.dll and libwinpthread-1.dll. Their upstream license and runtime library exception apply; these are unmodified runtime components distributed by the Vosk project. Source: https://github.com/alphacep/vosk-api/releases/tag/v0.3.45
GCC Runtime Library Exception: https://www.gnu.org/licenses/gcc-exception-3.1.html
MinGW runtime source and license notices: https://www.mingw-w64.org/

This application does not use or package Holly, Docker, Python, NVIDIA Parakeet, or CUDA. The first setup downloads the native Vosk runtime and small English model. Subsequent transcription runs offline.
