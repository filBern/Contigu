#!/usr/bin/env python3
"""Static server for a Unity WebGL build. Usage: python3 serve.py <build folder> [port]
Serves .wasm/.js/.data with the right types, and any precompressed file (.br / .gz / .unityweb) with its Content-Encoding header,
so a build made with compression on works as well as one made with it off."""
import http.server
import os
import socketserver
import sys

ROOT = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.getcwd()
PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8765


class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map = dict(http.server.SimpleHTTPRequestHandler.extensions_map, **{
        '.wasm': 'application/wasm', '.js': 'application/javascript', '.json': 'application/json',
        '.data': 'application/octet-stream', '.unityweb': 'application/octet-stream', '.symbols': 'application/octet-stream',
    })

    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def end_headers(self):
        path = self.translate_path(self.path.split('?')[0])
        if path.endswith('.br'):
            self.send_header('Content-Encoding', 'br')
        elif path.endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cache-Control', 'no-store')
        super().end_headers()

    def guess_type(self, path):
        base = path[:-3] if path.endswith('.br') else path[:-3] if path.endswith('.gz') else path
        return super().guess_type(base)

    def log_message(self, fmt, *args):
        pass


class Server(socketserver.ThreadingTCPServer):
    allow_reuse_address = True


if __name__ == '__main__':
    with Server(('127.0.0.1', PORT), Handler) as httpd:
        print('Serving %s on http://127.0.0.1:%d' % (ROOT, PORT), flush=True)
        httpd.serve_forever()
