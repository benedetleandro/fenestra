# Fenestra

**Dois cliques em qualquer `dist/index.html`. Ele abre no seu navegador como um site —
porque o Fenestra serviu o arquivo em localhost.**

Sem terminal. Sem `npx serve`. Sem extensão para ativar. Sem janela de navegador
falsa.

> **Status: alfa.** O servidor e a integração com o navegador funcionam. A associação
> do `.html` exige uma confirmação única do usuário — o Windows não permite que
> um programa se torne o manipulador padrão sozinho.

[**Site →**](https://benedetleandro.github.io/fenestra/) · [English](README.md)

## O problema

Um build moderno (Vite, React, Svelte, qualquer coisa com módulos ES) não roda
em `file://`: o navegador bloqueia módulos por CORS, `/assets/...` aponta para a
raiz do disco e service worker não existe nessa origem. Essas aplicações
precisam de uma origem HTTP.

Normalmente isso significa abrir um terminal e subir um servidor estático. O
Fenestra elimina esse passo: ele **é** o servidor estático, acoplado ao duplo
clique.

## O que ele faz

Ao abrir um `.html`:

1. Lê o arquivo e decide se ele precisa de origem HTTP (módulos ES, caminhos
   absolutos, `fetch`, service worker, manifest).
2. **Se precisa**, serve a pasta que contém o arquivo em `127.0.0.1`, numa porta
   estável, e abre uma aba normal no seu navegador nessa URL.
3. **Se não precisa**, abre o arquivo direto via `file://`. Nenhum servidor é criado.

A porta é a mesma para cada pasta, então `localStorage`, cookies e service
worker sobrevivem entre sessões.

## Instalação

Requisitos: Windows 10/11. O .NET Framework 4.x e o Edge já vêm com o sistema —
e qualquer navegador Chromium ou o Firefox também funciona.

```powershell
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

O binário é `bin\Fenestra.exe`. Sem SDK, sem NuGet, sem internet para compilar.

## Uso

```powershell
Fenestra.exe "C:\projetos\app\dist\index.html"
Fenestra.exe "C:\projetos\app\dist"
Fenestra.exe --serve "C:\caminho\index.html"   # força o servidor
Fenestra.exe --browser chrome "C:\caminho\index.html"
Fenestra.exe --browser firefox                 # define o navegador preferido
Fenestra.exe --browsers                        # lista os navegadores achados
Fenestra.exe --install                          # registra em "Abrir com"
Fenestra.exe --uninstall                        # remove o registro
```

### Tornar padrão para `.html` (uma vez)

1. Rode `Fenestra.exe --install`.
2. Botão direito em qualquer `index.html` → **Abrir com** → **Escolher outro aplicativo**.
3. Selecione **Fenestra** e marque **Sempre usar este aplicativo**.

O Windows permite apenas um programa por extensão, então essa confirmação não
pode ser automatizada. O manipulador é invisível; a janela que abre é o seu
navegador.

### Navegadores

**Edge é o padrão.** Chrome, Firefox, Brave, Vivaldi, Opera e Chromium são
detectados automaticamente.

```powershell
Fenestra.exe --browsers              # o que está instalado
Fenestra.exe --browser chrome        # passa a usar o Chrome
Fenestra.exe --browser default       # deixa o Windows decidir
```

A escolha fica em `%LOCALAPPDATA%\Fenestra\browser.txt` e vale para todos os
duplo cliques seguintes — não precisa passar a flag de novo.

### Sem navegador (testes e CI)

Defina `FENESTRA_NO_OPEN=1` e o servidor sobe sem abrir navegador. A porta fica
em `%LOCALAPPDATA%\Fenestra\sessions\<hash>.txt`.

## Linux

Existe uma build nativa separada em [`linux/`](linux): um binário estático, sem
runtime e sem Wine. Escrito em Go e compilado para `linux/amd64` e `linux/arm64`.

```bash
chmod +x fenestra-linux-amd64
./fenestra-linux-amd64 ~/projetos/app/dist/index.html
```

O comportamento é o mesmo da build Windows: porta estável por pasta, fallback de
SPA, confinamento de caminho e escolha de navegador.

```bash
./fenestra-linux-amd64 --browsers
./fenestra-linux-amd64 --browser firefox
./fenestra-linux-amd64 --install      # .desktop + xdg-mime
```

O `--install` grava `~/.local/share/applications/fenestra.desktop` e chama
`xdg-mime default`. Precisa de `xdg-utils`, que a maioria dos desktops já tem.

Compilar do código:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
```

## macOS

Não existe binário pronto para macOS, e é de propósito. Distribuir um exigiria
notarização da Apple (US$ 99/ano); sem ela, o download é bloqueado pelo Gatekeeper.

Em vez disso, [`macos/`](macos) é um kit de montagem: instale o Go (instalador
oficial assinado, sem aviso), faça `git clone` e rode um comando. O `git clone`
não marca os arquivos com quarentena, então o macOS não pede permissão nenhuma.
Veja [macos/README.md](macos/README.md).

O macOS não é coberto por CI num Mac real; o código é o mesmo da build Linux, que é.

## Quando funciona

| Caso | Funciona? | Por quê |
|---|---|---|
| Build estático (`dist`, `build`, `out`) | **Sim** | HTML/CSS/JS puro |
| SPA com rotas no cliente | **Sim** | fallback para `index.html` em rotas sem extensão |
| HTML simples de um arquivo | **Sim** | abre via `file://`, sem servidor |
| Projeto fonte (`/src/main.tsx`) | **Não** | precisa do dev server do projeto para compilar |
| App com backend/API | **Não** | o Fenestra serve arquivos; não faz proxy nem roda serviços |
| Entrada em subpasta com `../` | **Não** | a raiz é a pasta do arquivo; `..` é bloqueado por segurança |

Se a detecção errar, `--serve` força a origem HTTP.

## Como funciona

Um arquivo C#, sem dependências, compilado com o `csc.exe` que já vem no Windows.

- **Servidor HTTP/1.1** em `TcpListener`, ligado apenas a `127.0.0.1`.
- **Resolução de caminho**: normaliza, rejeita `..`, `:` e `::$DATA`, confina à
  raiz servida.
- **Porta**: hash FNV-1a da pasta → porta em `51000-63000`, guardada num arquivo
  por pasta e reaproveitada na próxima execução.
- **Fallback de SPA**: 404 sem extensão devolve `index.html`.
- **Instância única**: um segundo duplo clique encontra o servidor ativo e só
  abre outra aba.
- **Ciclo de vida**: encerra quando o navegador aberto por ele fecha, ou após 4 horas
  ociosas se o navegador já estava aberto.

## Segurança

Ferramenta local: só loopback, sem TLS, sem autenticação, sem telemetria. Serve
todos os arquivos sob a pasta aberta — não aponte para um diretório com segredos.
O modelo de ameaça completo está em [SECURITY.md](SECURITY.md).

## Roadmap

- [ ] `--root <pasta>` para builds cuja entrada fica em subpasta
- [x] Linux: build nativa em Go em [`linux/`](linux), amd64 e arm64
- [ ] macOS **não** está no plano: exige notarização da Apple
- [ ] Assinatura de código para remover o aviso do SmartScreen
- [ ] Modo portátil (sem `%LOCALAPPDATA%`)
- [ ] Mais testes: HEAD, ETag, range requests, caminhos UTF-8
- [ ] Vídeo de demonstração de 15 segundos

## Contribuindo

Ferramenta pequena, superfície pequena. Leia [CONTRIBUTING.md](CONTRIBUTING.md);
a lista de `good first issues` está lá.

## Licença

[MIT](LICENSE) © Fenestra contributors
