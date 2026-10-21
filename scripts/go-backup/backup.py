"""Copy validated Go saves from a BlueStacks disk, without changing the disk."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import time
import zlib

from save_codec import validate


def copy_saves(disk, destination):
    # Deferred imports keep --help and the validation tests dependency-free.
    from dissect.hypervisor.disk.vhdx import VHDX
    from dissect.volume.disk import Disk
    from dissect.extfs import ExtFS
    destination.mkdir(parents=True, exist_ok=True)
    copied = []
    with disk.open('rb') as stream:
        filesystem = ExtFS(Disk(VHDX(stream)).partitions[0].open())
        for name in ('save_guest.dat', 'save_guest_backup.dat'):
            try:
                node = filesystem.get('/data/com.rovio.angrybirdsgo/' + name)
                if not 44 <= node.size <= 2_000_000:
                    continue
                data = node.open().read()
                _, strings = validate(data)
                timestamp = int(strings[strings.index('iTimestamp') + 1])
                stamp = datetime.fromtimestamp(timestamp, timezone.utc).strftime('%Y%m%d-%H%M%SZ')
            except (ValueError, IndexError, struct.error, zlib.error, FileNotFoundError):
                # Live filesystem reads may catch a save while it is being written.
                # Keep all earlier copies and retry on the next pass.
                continue
            digest = hashlib.sha256(data).hexdigest()
            target = destination / (stamp + '-' + digest[:12] + '.dat')
            if target.exists():
                continue
            with target.open('xb') as output:
                output.write(data)
            target.with_suffix('.json').write_text(json.dumps({
                'game_timestamp': timestamp, 'source': name, 'sha256': digest,
                'bytes': len(data), 'backed_up_at': datetime.now(timezone.utc).isoformat()
            }, indent=2), encoding='utf-8')
            copied.append(target.name)
    return copied


def player_running():
    result = subprocess.run(
        ['tasklist.exe', '/FI', 'IMAGENAME eq HD-Player.exe', '/FO', 'CSV', '/NH'],
        capture_output=True, creationflags=subprocess.CREATE_NO_WINDOW)
    return b'HD-Player.exe' in result.stdout


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--instance', default='Pie64')
    parser.add_argument('--disk', type=Path, help='Optional absolute path to Data.vhdx')
    parser.add_argument('--backup-dir', type=Path, default=Path(os.environ.get('LOCALAPPDATA', str(Path.home()))) / 'NostalgiaGameHub' / 'Backups' / 'Go')
    parser.add_argument('--player', type=Path, default=Path(os.environ.get('ProgramFiles', r'C:\Program Files')) / 'BlueStacks_nxt' / 'HD-Player.exe')
    parser.add_argument('--once', action='store_true', help='Copy the currently valid saves and exit')
    parser.add_argument('--launch', action='store_true', help='Launch Go before watching for new saves')
    args = parser.parse_args(argv)
    if not args.instance or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-' for c in args.instance):
        parser.error('Invalid BlueStacks instance name')
    disk = args.disk or Path(os.environ.get('ProgramData', r'C:\ProgramData')) / 'BlueStacks_nxt' / 'Engine' / args.instance / 'Data.vhdx'
    if not disk.is_file():
        parser.error('Data.vhdx not found; use --disk or --instance')
    args.backup_dir.mkdir(parents=True, exist_ok=True)
    if args.once:
        print(json.dumps(copy_saves(disk, args.backup_dir)))
        return 0
    if os.name != 'nt':
        parser.error('Watching and launching require Windows')
    if args.launch:
        if not args.player.is_file():
            parser.error('BlueStacks player not found; use --player')
        subprocess.Popen([str(args.player), '--instance', args.instance, '--cmd',
                          'launchApp', '--package', 'com.rovio.angrybirdsgo',
                          '--source', 'desktop_shortcut'], creationflags=subprocess.CREATE_NO_WINDOW)
    import msvcrt
    with (args.backup_dir / 'watcher.lock').open('a+b') as lock:
        if lock.tell() == 0:
            lock.write(b'1')
            lock.flush()
        lock.seek(0)
        try:
            msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        except OSError:
            return 0
        absent = 0
        while True:
            status = {'checked_at': datetime.now(timezone.utc).isoformat()}
            try:
                status['new_backups'] = copy_saves(disk, args.backup_dir)
            except Exception as error:
                status['error'] = str(error)
            (args.backup_dir / 'status.json').write_text(json.dumps(status, indent=2), encoding='utf-8')
            absent = 0 if player_running() else absent + 1
            if absent >= 4:
                return 0
            time.sleep(15)


if __name__ == '__main__':
    raise SystemExit(main())
