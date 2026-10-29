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

## Limitações honestas

- **Não testado num Mac real** por quem escreveu esta pasta. O código é o mesmo
  da build Linux, que roda em CI; o que muda é o `open -a`, os caminhos em
  `~/Library/Application Support` e o droplet.
- O `.app` é gerado pelo `osacompile`; se o seu macOS não tiver a ferramenta, o
  script avisa e gera só o binário.
- Cada pessoa compila o seu. Mandar o `.app` por AirDrop para outra máquina
  adiciona quarentena e o Gatekeeper bloqueia de novo.
- Sem assinatura de desenvolvedor, sem notarização, sem US$ 99/ano.

## Desfazer

Apague `Fenestra.app` e o `fenestra` desta pasta. Para o Finder, escolha outro
app em **Abrir com → Sempre**.
