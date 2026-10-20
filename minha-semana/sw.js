// Service worker: guarda os arquivos do app para uso offline. Não há chamadas de rede para dados.
const VERSAO = 'minha-semana-v1';
const ARQUIVOS = [
  './',
  './index.html',
  './manifest.webmanifest',
  './css/styles.css',
  './js/app.js',
  './js/logic.js',
  './js/store.js',
  './js/ui.js',
  './js/lembretes.js',
  './js/views/agenda.js',
  './js/views/ajustes.js',
  './js/views/comum.js',
  './js/views/compromisso.js',
  './js/views/hoje.js',
  './js/views/metas.js',
  './js/views/parceiros.js',
  './js/views/revisao.js',
  './icons/icone.svg',
  './icons/icone-192.png',
  './icons/icone-512.png',
  './icons/icone-maskable-512.png',
];

self.addEventListener('install', (e) => {
  e.waitUntil(caches.open(VERSAO).then((c) => c.addAll(ARQUIVOS)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (e) => {
  e.waitUntil(
    caches.keys()
      .then((chaves) => Promise.all(chaves.filter((k) => k !== VERSAO).map((k) => caches.delete(k))))
      .then(() => self.clients.claim()),
  );
});

// Primeiro o cache; se o arquivo não estiver lá, busca na rede e guarda a cópia.
self.addEventListener('fetch', (e) => {
  if (e.request.method !== 'GET' || new URL(e.request.url).origin !== self.location.origin) return;
  e.respondWith(
    caches.match(e.request, { ignoreSearch: true }).then((salvo) => salvo || fetch(e.request).then((resp) => {
      if (resp.ok) {
        const copia = resp.clone();
        caches.open(VERSAO).then((c) => c.put(e.request, copia));
      }
      return resp;
    })),
  );
});

self.addEventListener('notificationclick', (e) => {
  e.notification.close();
  e.waitUntil(
    self.clients.matchAll({ type: 'window' }).then((janelas) => {
      if (janelas.length) return janelas[0].focus();
      return self.clients.openWindow('./index.html#/hoje');
    }),
  );
});
