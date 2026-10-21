"""Synthetic container only: this fixture contains no playable or personal profile."""
import struct
import unittest

from save_codec import game_hash, validate

FIXTURE = bytes.fromhex(
    '00000000540000009b5fd606000000000000000034000000320000000000000000000000'
    'f5a549c420000000ecacdce8250f5a0d315ceba7f2f927d3ecee5c2c50c145f226dca361'
    '4d0bc088b3482f1f986dcc7c81ebedb77f8a3019d2881596')


def repair_outer_checksum(data):
    size = struct.unpack_from('<I', data, 4)[0]
    struct.pack_into('<I', data, 8, game_hash(data[12:12 + size]))
    return data


class SaveValidationTests(unittest.TestCase):
    def test_native_hash_vectors(self):
        # Cross-checked against the game's ARM hashing routine.
        for data, expected in [(b'', 0x10601bec), (b'abc', 0x83c969ca),
                               (b'abcdefghijklmnop', 0x572ca655)]:
            self.assertEqual(game_hash(data), expected)

    def test_complete_synthetic_container(self):
        _, strings = validate(FIXTURE)
        self.assertEqual(strings, ['Save', 'iTimestamp', '1700000000'])

    def test_missing_padding_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'file length'):
            validate(FIXTURE[:-1])

    def test_corrupted_file_is_rejected(self):
        damaged = bytearray(FIXTURE)
        damaged[55] ^= 1
        with self.assertRaisesRegex(ValueError, 'file checksum'):
            validate(damaged)

    def test_payload_checksum_checked_separately(self):
        damaged = bytearray(FIXTURE)
        damaged[36] ^= 1
        repair_outer_checksum(damaged)
        with self.assertRaisesRegex(ValueError, 'payload checksum'):
            validate(damaged)

    def test_expansion_limit_checked_before_decompression(self):
        damaged = bytearray(FIXTURE)
        struct.pack_into('<I', damaged, 20, 64 * 1024 * 1024)
        repair_outer_checksum(damaged)
        with self.assertRaisesRegex(ValueError, 'payload size'):
            validate(damaged)


if __name__ == '__main__':
    unittest.main()
