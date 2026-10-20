// Ajustes: nome, lembretes, exportação/importação e privacidade. Inclui a tela de boas-vindas.
import { h, icone, campo, aviso, confirmar, baixarArquivo } from '../ui.js';
import { LIMITE_NOTIFICACOES_DIA, isoData } from '../logic.js';
import * as store from '../store.js';
import { suportaNotificacoes, pedirPermissao, permissaoAtual } from '../lembretes.js';

const TEXTO_PRIVACIDADE = [
  'Todos os dados ficam guardados apenas neste aparelho. O app não tem conta, servidor, anúncios, chat nem rede social.',
  'Não coletamos localização, fotos ou contatos. As mensagens para parceiros são copiadas e enviadas por você, se quiser.',
  'Você pode exportar seus dados (JSON ou PDF) e apagá-los a qualquer momento.',
];

export function renderBoasVindas(raiz, { redesenhar }) {
  let nome = '';
  raiz.append(
    h('section', { class: 'boas-vindas' },
      h('img', { src: 'icons/icone.svg', alt: '', width: '72', height: '72' }),
      h('h1', {}, 'Minha Semana'),
      h('p', { class: 'destaque' }, 'Um lugar para organizar seus compromissos, escolher até três metas por semana e aprender com cada semana.'),
      h('ul', { class: 'lista-simples' }, TEXTO_PRIVACIDADE.map((t) => h('li', {}, t))),
      h('form', {
        onsubmit: (e) => {
          e.preventDefault();
          store.salvarPerfil({ nome: nome.trim(), boasVindasVistas: true });
          location.hash = '#/hoje';
          redesenhar();
        },
      },
        campo('Como você quer ser chamada? (opcional)', h('input', {
          type: 'text', maxlength: '30', autocomplete: 'given-name', oninput: (e) => { nome = e.target.value; },
        })),
        h('button', { class: 'botao primario largo', type: 'submit' }, 'Começar'),
      ),
    ),
  );
}

export function renderAjustes(raiz, { dados, redesenhar }) {
  const cfg = dados.ajustes.notificacoes;
  const nome = h('input', { type: 'text', value: dados.perfil.nome, maxlength: '30', autocomplete: 'given-name' });

  const linhaLembrete = (tipo, rotulo, ajuda) => {
    const item = cfg[tipo];
    return h('div', { class: 'lembrete' },
      h('label', { class: 'alternador' },
        h('input', {
          type: 'checkbox', checked: item.ativo,
          onchange: async (e) => {
            if (e.target.checked && permissaoAtual() !== 'granted') {
              const p = await pedirPermissao();
              if (p !== 'granted') aviso('Sem permissão do navegador, o lembrete aparecerá só como aviso dentro do app.');
            }
            store.salvarNotificacoes({ [tipo]: { ...item, ativo: e.target.checked } });
          },
        }),
        h('span', {}, h('strong', {}, rotulo), h('br'), h('small', {}, ajuda)),
      ),
      h('input', {
        type: 'time', value: item.hora, 'aria-label': `Horário: ${rotulo}`,
        onchange: (e) => store.salvarNotificacoes({ [tipo]: { ...item, hora: e.target.value || item.hora } }),
      }),
    );
  };

  const importar = h('input', {
    type: 'file', accept: 'application/json,.json', class: 'oculto',
    onchange: async (e) => {
      const arq = e.target.files[0];
      if (!arq) return;
      const ok = await confirmar('Importar dados', 'Os dados atuais deste aparelho serão substituídos pelos do arquivo. Continuar?', 'Importar');
      if (!ok) return;
      try {
        store.importarJson(await arq.text());
        aviso('Dados importados.');
      } catch {
        aviso('Não foi possível ler esse arquivo.');
      }
    },
  });

  raiz.append(
    h('header', { class: 'cabecalho' },
      h('div', {}, h('a', { class: 'voltar', href: '#/hoje' }, icone('esquerda', 'icone pequeno'), 'Hoje'), h('h1', {}, 'Ajustes')),
    ),

    h('section', { class: 'secao' },
      h('h2', {}, 'Perfil'),
      h('form', {
        onsubmit: (e) => { e.preventDefault(); store.salvarPerfil({ nome: nome.value.trim() }); aviso('Nome salvo.'); },
      },
        campo('Nome exibido na saudação', nome),
        h('button', { class: 'botao secundario pequeno', type: 'submit' }, 'Salvar nome'),
      ),
    ),

    h('section', { class: 'secao' },
      h('h2', {}, 'Lembretes'),
      h('p', { class: 'apoio' }, `Você escolhe quais lembretes receber. No máximo ${LIMITE_NOTIFICACOES_DIA} por dia.`),
      !suportaNotificacoes() ? h('p', { class: 'nota' }, 'Este navegador não permite notificações. Os lembretes aparecerão como aviso quando o app estiver aberto.') : null,
      h('label', { class: 'alternador destaque-alternador' },
        h('input', { type: 'checkbox', checked: cfg.silenciado, onchange: (e) => store.salvarNotificacoes({ silenciado: e.target.checked }) }),
        h('span', {}, h('strong', {}, 'Silenciar todos os lembretes')),
      ),
      h('div', { class: cfg.silenciado ? 'esmaecido' : '' },
        linhaLembrete('manha', 'Resumo da manhã', 'Compromissos e ações de metas do dia.'),
        linhaLembrete('tarde', 'Lembrete de lição', 'Só aparece se houver lição de casa pendente.'),
        linhaLembrete('domingo', 'Revisão de domingo', 'Convite para revisar a semana e planejar a próxima.'),
      ),
      h('p', { class: 'ajuda' }, 'Aplicativos web só conseguem mostrar lembretes enquanto o app está aberto ou em segundo plano no aparelho. Instale-o na tela inicial para melhor resultado.'),
    ),

    h('section', { class: 'secao' },
      h('h2', {}, 'Meus dados'),
      h('div', { class: 'acoes-coluna' },
        h('button', {
          class: 'botao secundario', type: 'button',
          onclick: () => baixarArquivo(`minha-semana-${isoData(new Date())}.json`, store.exportarJson(), 'application/json'),
        }, 'Exportar cópia (JSON)'),
        h('button', { class: 'botao secundario', type: 'button', onclick: () => { location.hash = '#/revisao'; setTimeout(() => window.print(), 300); } }, 'Salvar revisão da semana em PDF'),
        h('label', { class: 'botao secundario' }, importar, 'Importar cópia (JSON)'),
        h('button', {
          class: 'botao perigo', type: 'button',
          onclick: async () => {
            const ok = await confirmar('Apagar tudo', 'Todos os compromissos, metas, parceiros e revisões deste aparelho serão apagados. Essa ação não pode ser desfeita.', 'Apagar tudo');
            if (ok) { store.apagarTudo(); location.hash = '#/hoje'; redesenhar(); }
          },
        }, 'Apagar todos os dados'),
      ),
    ),

    h('section', { class: 'secao' },
      h('h2', {}, 'Privacidade'),
      h('ul', { class: 'lista-simples' }, TEXTO_PRIVACIDADE.map((t) => h('li', {}, t))),
      h('p', { class: 'apoio' }, 'Uma eventual versão com sincronização na nuvem só poderá ser ativada com o consentimento do responsável legal (LGPD, art. 14).'),
    ),
  );
}
