# Fenestra no Linux

[English](README.md) · Português

O Fenestra para Linux é um único binário Go estático. Sem runtime, sem GUI, sem root.

## Instalar

Baixe o binário da sua arquitetura na
[release mais recente](https://github.com/benedetleandro/fenestra/releases/latest):

- `fenestra-linux-amd64`
- `fenestra-linux-arm64`

```bash
chmod +x fenestra-linux-amd64
mkdir -p ~/.local/bin
mv fenestra-linux-amd64 ~/.local/bin/fenestra
```

Garanta que `~/.local/bin` está no `PATH`.

## Usar

```bash
fenestra ~/projetos/app/dist/index.html   # abre um build
fenestra ~/projetos/app/dist              # abre uma pasta
fenestra --serve ~/index.html             # força o servidor
fenestra --port 8080 ~/index.html         # escolhe a porta
fenestra --browser firefox ~/app/index.html
fenestra --browsers                       # o que está instalado
```

## Endurecimento

- Só headers `Host` de loopback são aceitos (`127.0.0.1`, `localhost`), o que
  bloqueia DNS rebinding.
- Symlinks que resolvem para fora da raiz servida são rejeitados.
- As respostas carregam `ETag` e `Last-Modified`, revalidam com `304` e suportam
  `Range` para mídia grande.

## Entradas de fonte

Se o HTML referencia `/src/` (entrada de dev do Vite ou parecido), o Fenestra não
serve esse arquivo: ele procura `dist/`, `build/` ou `out/` ao lado e serve o
build. Quando não existe build, ele para com uma mensagem pedindo para compilar
primeiro.

## Registrar o handler de `.html`

```bash
fenestra --install
```

Isso escreve `~/.local/share/applications/fenestra.desktop`, atualiza o banco do
desktop e roda:

```bash
xdg-mime default fenestra.desktop text/html
```

Para desfazer:

```bash
fenestra --uninstall
```

Se o `xdg-mime` não existir, instale o `xdg-utils` ou rode o comando acima na mão.

## Navegadores

A detecção percorre o `PATH`, nesta ordem: `microsoft-edge`, `google-chrome`,
`chromium`, `firefox`, `brave-browser`, `vivaldi`, `opera`. A preferência fica em
`~/.config/fenestra/browser`; `--browser default` entrega para o `xdg-open`.

## Onde guarda estado

| Caminho | O quê |
|---|---|
| `~/.config/fenestra/browser` | navegador preferido |
| `~/.local/share/fenestra/fenestra.log` | log do servidor |
| `~/.local/share/fenestra/` | um lock por pasta servida (porta estável) |

`XDG_CONFIG_HOME` e `XDG_DATA_HOME` são respeitados.

## Compilar do código

Go 1.21+, sem cgo:

```bash
cd linux
CGO_ENABLED=0 go build -trimpath -ldflags "-s -w" -o fenestra .
go test ./...
```

Compilação cruzada para ARM:

```bash
GOOS=linux GOARCH=arm64 CGO_ENABLED=0 go build -o fenestra-arm64 .
```

## Diferenças para os outros builds

- Sem registro e sem Finder: o arquivo `.desktop` mais o `xdg-mime` é o caminho
  da associação.
- O estado vive em caminhos XDG, não em `%LOCALAPPDATA%` nem em
  `~/Library/Application Support`.
- Mesmo servidor, mesmas flags, mesmo fallback de SPA do Windows e do macOS.

## Se algo falhar

- **`could not find <browser> installed on this machine`** — rode
  `fenestra --browsers` para ver o que foi detectado, ou use `--browser default`.
- **`xdg-mime failed`** — instale o `xdg-utils`, ou defina o handler na mão.
- **Nada abre** — `--browser default` usa o `xdg-open`; teste se ele funciona
  sozinho.
- **Sem associação depois do `--install`** — alguns desktops guardam cache de
  MIME; saia da sessão, ou rode
  `update-desktop-database ~/.local/share/applications`.

## Testado no CI

`ubuntu-latest`: `go test`, build e um smoke test HTTP de verdade. O ARM64 é
compilado no mesmo pipeline; não é executado.
