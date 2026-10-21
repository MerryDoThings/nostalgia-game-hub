"""Shorten the metadata wait in a user-provided Go 1.8.7 ARMv7 APK.

Produces an unsigned local APK. Does not install anything or modify app data.
"""
import argparse
import hashlib
import json
import struct
import zipfile
from pathlib import Path

LIBRARY = 'lib/armeabi-v7a/libABK.so'
FUNCTION_START = 0x22f16c
FUNCTION_SIZE = 1332
FUNCTION_SHA256 = '586a554c19d698511070a6229240c56d0f903b3adba29e37235db78857c21f55'
WAIT_ADDRESS = 0x22f304


def patch(source_path, output_path, seconds=5):
    source_path, output_path = Path(source_path), Path(output_path)
    if not 1 <= seconds <= 60:
        raise ValueError('Timeout must be between 1 and 60 seconds')
    report_path = output_path.with_suffix('.patch.json')
    if output_path.exists() or report_path.exists():
        raise FileExistsError('Output or report already exists; choose a new filename')
    with zipfile.ZipFile(source_path) as source:
        if len(source.namelist()) != len(set(source.namelist())):
            raise ValueError('Duplicate APK entries are unsupported')
        original = source.read(LIBRARY)
        function = original[FUNCTION_START:FUNCTION_START + FUNCTION_SIZE]
        if hashlib.sha256(function).hexdigest() != FUNCTION_SHA256:
            raise ValueError('Unsupported or already modified Go metadata function')
        if original[WAIT_ADDRESS:WAIT_ADDRESS + 4] != bytes.fromhex('594200e3'):
            raise ValueError('Unexpected ARM wait instruction')
        changed = bytearray(original)
        iterations = seconds * 10 + 1
        # MOVW r4, #(seconds * 10 + 1); each completed pause lasts 100 ms.
        instruction = 0xe3004000 | (iterations & 0xfff) | ((iterations & 0xf000) << 4)
        struct.pack_into('<I', changed, WAIT_ADDRESS, instruction)
        with zipfile.ZipFile(output_path, 'x') as output:
            for entry in source.infolist():
                if entry.filename.upper().startswith('META-INF/'):
                    continue
                data = bytes(changed) if entry.filename == LIBRARY else source.read(entry)
                output.writestr(entry, data)
    with zipfile.ZipFile(source_path) as source, zipfile.ZipFile(output_path) as output:
        for name in output.namelist():
            if name != LIBRARY and source.read(name) != output.read(name):
                raise ValueError('An unrelated APK entry changed: ' + name)
        actual = output.read(LIBRARY)
        if actual[:WAIT_ADDRESS] != original[:WAIT_ADDRESS] or actual[WAIT_ADDRESS + 4:] != original[WAIT_ADDRESS + 4:]:
            raise ValueError('Unrelated native code changed')
    report = {
        'source_sha256': hashlib.sha256(source_path.read_bytes()).hexdigest(),
        'output_sha256': hashlib.sha256(output_path.read_bytes()).hexdigest(),
        'timeout_before_seconds': 60,
        'timeout_after_seconds': seconds,
        'native_instruction_offset': hex(WAIT_ADDRESS),
        'unsigned': True,
        'preserved': ['all other native instructions', 'DEX', 'assets and importer payload'],
    }
    report_path.write_text(json.dumps(report, indent=2), encoding='utf-8')
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input_apk', type=Path)
    parser.add_argument('output_apk', type=Path)
    parser.add_argument('--timeout-seconds', type=int, default=5)
    args = parser.parse_args()
    print(json.dumps(patch(args.input_apk, args.output_apk, args.timeout_seconds), indent=2))


if __name__ == '__main__':
    main()
