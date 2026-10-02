"""Local UI preview with fictional data. No database or real authentication.
Run this file, then open http://127.0.0.1:8767/support/index.html.
Use any fictional email/password. Never use real credentials here.
"""
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parent.parent / 'dotNetApiProBoilerplate.Api' / 'wwwroot'
STORE = '11111111-1111-4111-8111-111111111111'
OLD = '22222222-2222-4222-8222-222222222222'
NEW = '33333333-3333-4333-8333-333333333333'
SALE = '44444444-4444-4444-8444-444444444444'
sale = dict(id=SALE, invoiceNumber='DEMO-001', saleDate='2026-09-17T10:00:00Z', totalAmount=100,
            customerId=OLD, customerName='Ahmed — fictif', debtToTransfer=100, modifiedAt='2026-09-17T10:00:00Z')
history = []

class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT), **kwargs)

    def reply(self, value, status=200):
        body = json.dumps(value).encode()
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        path = self.path.split('?')[0]
        if path == '/api/support/stores':
            self.reply([dict(id=STORE, name='DÉMONSTRATION — Magasin fictif')])
        elif path.endswith('/sales'):
            self.reply([sale])
        elif path.endswith('/customers'):
            self.reply([dict(id=NEW, name='Sami — fictif', phone='Démo', currentBalance=0)])
        elif path.endswith('/corrections'):
            self.reply(history)
        else:
            super().do_GET()

    def do_POST(self):
        body = json.loads(self.rfile.read(int(self.headers['Content-Length'])))
        if self.path == '/api/auth/login':
            self.reply(dict(accessToken='fictional-preview-token', role='SuperAdmin'))
        elif self.path.endswith('/customer'):
            sale.update(customerId=NEW, customerName='Sami — fictif')
            history.append(dict(id=body['operationId'], invoiceNumber='DEMO-001', reason=body['reason'],
                                transferredDebt=100, createdAt='2026-09-17T11:00:00Z'))
            self.reply(dict(id=body['operationId'], transferredDebt=100))
        else:
            self.reply(dict(detail='Route de démonstration inconnue'), 404)

if __name__ == '__main__':
    print('Fictional preview only: http://127.0.0.1:8767/support/index.html', flush=True)
    ThreadingHTTPServer(('127.0.0.1', 8767), Handler).serve_forever()
