# Fenestra no macOS

[English](README.md) · Português

Esta pasta é o kit de montagem. Você compila na sua máquina — não existe binário
pronto para baixar, e isso é de propósito: um app compilado localmente não passa
pelo Gatekeeper.

## Requisito: Go 1.21+

O instalador oficial em <https://go.dev/dl/> é assinado e notarizado pelo Google,
então **não aparece aviso nenhum**. É a única coisa que você instala.

## Caminho A — sem git (Mac novo, sem Command Line Tools)

Mac recém-formatado não tem `git`: o comando dispara a instalação das Command
Line Tools (~1,5 GB). Dá para pular isso:

1. Baixe o ZIP: <https://github.com/benedetleandro/fenestra/archive/refs/heads/main.zip>
2. Extraia
3. No Terminal:

```bash
cd ~/Downloads/fenestra-main/macos
bash build.command
```

O `bash` é do sistema, e o script é **lido**, não executado pelo Finder — então o
Gatekeeper não entra no caminho. O `fenestra` e o `Fenestra.app` nascem locais,
sem quarentena.

## Caminho B — git clone (para quem já tem git)

```bash
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra/macos
./build.command
```

## Caminho C — duplo clique

Só se você já tem Go e quer evitar o Terminal. O macOS pode pedir uma liberação
uma vez: **Ajustes → Privacidade e Segurança → Abrir assim mesmo**.

## Usar

```bash
./fenestra ~/projetos/app/dist/index.html   # abre no navegador padrão
./fenestra --browsers                       # o que está instalado
./fenestra --port 8080 ~/index.html         # escolhe a porta
./fenestra --browser firefox                # troca o navegador
./fenestra --browser safari                 # Safari também está na lista
./fenestra --install                        # instruções para o Finder
```

Para o Finder:

1. Arraste `Fenestra.app` para `/Applications`.
2. Botão direito num `index.html` → **Abrir com** → **Fenestra**.
3. Marque **Sempre usar este aplicativo**.

Se o HTML referencia `/src/` (entrada de dev), o Fenestra usa o `dist/`, `build/`
ou `out/` ao lado, e explica o que fazer quando não existe build.

## Homebrew (só o CLI)

```bash
brew tap benedetleandro/fenestra
brew trust benedetleandro/fenestra
brew install fenestra
```

O `brew trust` é exigência do Homebrew para taps de terceiros — uma vez só. Isso
instala apenas a linha de comando; o `Fenestra.app` continua vindo do
`./build.command`.

## Como funciona o `.app`

O `Fenestra.app` é um *droplet* em AppleScript (criado com `osacompile`, que já
vem no macOS). Ele recebe os arquivos que o Finder entrega e chama o binário Go
que está dentro do bundle. É isso que permite o "Abrir com" funcionar sem uma
app Cocoa.

## Se alguma coisa falhar

O script imprime `FAILED at line N`. Copie essa mensagem.

Enquanto isso, o caminho manual mostra onde está o problema:

```bash
cd fenestra/linux
go version
GOOS=darwin CGO_ENABLED=0 go build -o ../macos/fenestra .
../macos/fenestra ~/caminho/dist/index.html
```

Se o `go build` funcionar e o `./fenestra` abrir a página, o problema está só
no empacotamento do `.app` — e o programa já está utilizável.

Se o `osacompile` falhar (ele pode pedir as Command Line Tools), o script avisa
e mantém o binário. Nesse caso o Finder fica sem a associação, mas o Terminal
funciona.

## Limitações honestas

- **Testado no CI, num `macos-latest` real**: build, `codesign --verify`, droplet
  recebendo o arquivo via Finder (`open -a`) e o servidor respondendo HTTP.
- **Não testado numa sessão gráfica**: o clique no Finder, o menu "Abrir com" e o
  diálogo do Gatekeeper só existem numa tela — o runner é headless.
- O `.app` é gerado pelo `osacompile`; se o seu macOS não tiver a ferramenta, o
  script avisa e gera só o binário.
- Cada pessoa compila o seu. Mandar o `.app` por AirDrop para outra máquina
  adiciona quarentena e o Gatekeeper bloqueia de novo.
- Sem assinatura de desenvolvedor, sem notarização, sem US$ 99/ano.

## Desfazer

Apague `Fenestra.app` e o `fenestra` desta pasta. Para o Finder, escolha outro
app em **Abrir com → Sempre**.
