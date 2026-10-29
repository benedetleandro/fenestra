# Fenestra no macOS

Esta pasta é o kit de montagem. Você compila na sua máquina — não existe binário
pronto para baixar, e isso é de propósito: um app compilado localmente não passa
pelo Gatekeeper.

## Requisito: Go

O instalador oficial em <https://go.dev/dl/> é assinado e notarizado pelo Google,
então **não aparece aviso nenhum**. É a única coisa que você instala.

## Caminho A — git clone (zero avisos)

`git clone` não marca os arquivos com quarentena. Então este é o caminho em que o
macOS não pede nada:

```bash
git clone https://github.com/benedetleandro/fenestra.git
cd fenestra/macos
./build.command
```

O script compila `fenestra` e monta `Fenestra.app`.

> Baixar o ZIP do GitHub é diferente: o ZIP e tudo que sai dele ganham
> quarentena, e aí o macOS vai pedir uma liberação. Prefira `git clone`.

## Caminho B — sem Terminal nenhum

Se você já tem Go, ainda dá para usar só o Finder:

1. Duplo clique em `build.command`.
2. Na primeira vez o macOS pode bloquear: **Ajustes → Privacidade e Segurança →
   Abrir assim mesmo**.
3. O script compila e monta `Fenestra.app`.

## Usar

```bash
./fenestra ~/projetos/app/dist/index.html   # abre no navegador padrão
./fenestra --browsers                       # o que está instalado
./fenestra --browser firefox                # troca o navegador
./fenestra --install                        # instruções para o Finder
```

Para o Finder:

1. Arraste `Fenestra.app` para `/Applications`.
2. Botão direito num `index.html` → **Abrir com** → **Fenestra**.
3. Marque **Sempre usar este aplicativo**.

## Como funciona o `.app`

O `Fenestra.app` é um *droplet* em AppleScript (criado com `osacompile`, que já
vem no macOS). Ele recebe os arquivos que o Finder entrega e chama o binário Go
que está dentro do bundle. É isso que permite o "Abrir com" funcionar sem uma
app Cocoa.

## Se alguma coisa falhar

O script imprime `FALHOU na linha N`. Copie essa mensagem.

Enquanto isso, o caminho manual mostra onde está o problema:

```bash
cd fenestra/macos
go version
go build -o fenestra .
./fenestra ~/caminho/dist/index.html
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
