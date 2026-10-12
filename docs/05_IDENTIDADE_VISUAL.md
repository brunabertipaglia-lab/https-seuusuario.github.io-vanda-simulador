# Identidade visual — paleta, arte e regras de uso

Fontes: `art/reference/capy_tinkering_logo_original.png` e `art/reference/capi_personagem_original.png` (1254×1254 px, RGB, sem transparência), enviados pela equipe em 12/10/2026.

## 1. Paleta extraída do logo

Cada cor é a mediana dos pixels do logo dentro de uma faixa de matiz, saturação e brilho, e não uma amostra de um único ponto. Os valores ficam centralizados em `Scripts/Game/BrandPalette.cs`.

| Token | Hex | Origem | Uso no jogo |
|---|---|---|---|
| NavyDeep | `#000E26` | Fundo do espaço | Fundo da câmera |
| Navy | `#001D45` | Azul profundo | Painéis; texto sobre laranja e turquesa |
| NavyLight | `#0E3B66` | Variação para botões | Botões secundários; painel do Capi em modo informativo |
| Cream | `#FBEED5` | Letras do logo | Título |
| Turquoise | `#00F4FA` | Anel luminoso | Previsões; painel de sucesso |
| Orange | `#F27F28` | Ferramenta e detalhes do traje | Botão principal; painel de atenção |
| Critical | `#C0392B` | **Não está no logo** | Reservado a alertas críticos |
| Muted | `#8FA3BF` | Derivado | Informação secundária |

## 2. Contraste (WCAG 2.x, calculado)

| Texto / fundo | Razão | Situação |
|---|---|---|
| Branco / Navy | 16,6:1 | Aprovado (AAA) |
| Cream / Navy | 14,5:1 | Aprovado (AAA) |
| Turquoise / Navy | 12,1:1 | Aprovado (AAA) |
| Orange / Navy e Navy / Orange | 6,2:1 | Aprovado (AA) |
| Branco / Critical | 5,4:1 | Aprovado (AA) |
| **Branco / Orange** | **2,7:1** | **Reprovado: não usar** |

A primeira versão do protótipo usava texto branco no botão laranja "Avançar turno" e no painel de atenção do Capi, o que reprovava no contraste. Agora o texto sobre laranja e turquesa é azul-marinho (`BrandPalette.TextOnSeverity`). A cor continua sem ser o único sinal: cada alerta mantém um rótulo textual ([i], [!], [!!], [ok]).

## 3. Arte incorporada ao Unity

| Arquivo em `Assets/CapiTinkering/Art/` | Derivado de | Uso |
|---|---|---|
| `Capi_Portrait.png` (384×384) | Recorte da cabeça do personagem | Retrato no painel do Capi |
| `CapyTinkering_Logo.png` (512×512) | Logo redimensionado | Linha de título do painel da missão |
| `Capi_FullBody.png` (768×768) | Personagem redimensionado | Reservado: abertura e relatório final |

O `CapiArtImporter` importa essas imagens como Sprite automaticamente, e o construtor da cena as aplica. Se faltarem, a cena usa o bloco turquesa com o texto "CAPI".

## 4. Limitações da arte atual

| Limitação | Consequência | Recomendação |
|---|---|---|
| Sem canal alfa (fundo cinza-claro no personagem; fundo do espaço no logo) | O retrato aparece dentro de um quadrado claro, e o logo não pode ser sobreposto a outros fundos | Pedir à pessoa responsável pela arte um PNG com transparência. O recorte automático é arriscado porque o traje branco tem cor próxima à do fundo |
| Ilustração 2D estática | Não serve como modelo 3D animável na cena isométrica | No protótipo, o Capi aparece como retrato 2D. Um modelo 3D próprio fica no backlog, fora do MVP |
| Uma única expressão | O Capi não muda de expressão conforme a severidade | Variações opcionais (atento, preocupado, comemorando), sem bloquear o protótipo |
| Imagens aparentemente geradas por IA | Termos de uso e autoria precisam estar claros para a submissão | Registrar a ferramenta, a data e os termos de uso no material da equipe |

## 5. Inconsistência de nome: decisão da equipe

O logo grafa **"CAPY TINKERING"**. O planejamento e os textos do jogo usam **"CAPI TINKERING"**, e o personagem se chama **"Capi"**.

O nome da equipe exibido no jogo está concentrado em uma única constante, `TextTable.TeamName`, e por enquanto mantém "CAPI TINKERING", conforme o planejamento escrito. Quando a grafia oficial for decidida, basta alterar essa linha e a documentação.
