# Política de segurança

[English](SECURITY.md) · Português

## Versões suportadas

O Fenestra é um projeto alpha. Só o último commit da `main` é suportado.

## Reportar uma vulnerabilidade

Por favor **não abra uma issue pública**. Use o reporte privado de
vulnerabilidades do GitHub (aba "Security" → "Report a vulnerability") neste
repositório.

Você recebe resposta em alguns dias. Se o relato for válido, virão correção e
release, e você será creditado, a menos que prefira o contrário.

## Modelo de ameaça

O Fenestra é uma ferramenta **local**. Ele roda um servidor HTTP preso a
`127.0.0.1` e serve a pasta do arquivo que você abriu. Não foi feito para ser
exposto a uma rede, e não implementa TLS, autenticação nem controle de acesso.

O que é protegido:

- **Binding**: o listener só escuta `127.0.0.1`, nunca `0.0.0.0`.
- **Path traversal**: as requisições são normalizadas e confinadas à raiz servida.
  `..`, `:`, `::$DATA` e caminhos absolutos são rejeitados.
- **Sem código remoto**: o servidor só lê e serve arquivos. Não executa nada da
  pasta servida.

O que **não** é protegido, por design:

- Todo arquivo sob a raiz servida é legível por qualquer coisa que alcance a
  porta, inclusive outros processos locais. Aponte o Fenestra para uma pasta,
  não para o seu diretório home.
- Arquivos e pastas ocultos (`.env`, `.git/`) retornam 403 e ficam fora das
  listagens; `.well-known/` é a exceção.
- Não há autenticação: um processo local pode pedir qualquer arquivo da raiz.
- Os cabeçalhos `Referer`/`Origin` não são validados, então uma página maliciosa
  pode sondar a porta local (DNS rebinding é parcialmente mitigado pelo binding
  em loopback, mas não totalmente).

## Escopo

Fora de escopo: vulnerabilidades que exigem que o atacante já tenha execução de
código local, e problemas causados por abrir uma pasta que contém segredos.
